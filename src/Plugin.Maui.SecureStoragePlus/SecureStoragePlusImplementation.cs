using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Plugin.Maui.SecureStoragePlus.Encryption;
using Plugin.Maui.SecureStoragePlus.Internal;
using Plugin.Maui.SecureStoragePlus.Storage;

namespace Plugin.Maui.SecureStoragePlus;

internal sealed class SecureStoragePlusImplementation : ISecureStoragePlus
{
    readonly ISecureStorageBackend _backend;
    readonly IDataEncryptor _encryptor;
    readonly TimeProvider _time;
    readonly SemaphoreSlim _gate = new(1, 1);

    byte[]? _dek;

    public SecureStoragePlusImplementation()
        : this(new MauiSecureStorageBackend(), new AesGcmDataEncryptor(), TimeProvider.System)
    {
    }

    internal SecureStoragePlusImplementation(
        ISecureStorageBackend backend,
        IDataEncryptor encryptor,
        TimeProvider timeProvider)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        _time = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task SetAsync(string key, string value, SecureStorageOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        return WithLock(() => SetCoreAsync(StorageKeys.Normalize(key), value, options, cancellationToken), cancellationToken);
    }

    public Task SetAsync<T>(string key, T value, SecureStorageOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (value is string text)
        {
            return SetAsync(key, text, options, cancellationToken);
        }

        var json = JsonSerializer.Serialize(value, JsonDefaults.Options);
        return SetAsync(key, json, options, cancellationToken);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    public Task<SecureStorageGetResult<string>> TryGetAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = StorageKeys.Normalize(key);
        return WithLock(() => TryGetCoreAsync(normalized, cancellationToken), cancellationToken);
    }

    public async Task<SecureStorageGetResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
        if (!result.Found)
        {
            return new SecureStorageGetResult<T>(false, result.Expired, default);
        }

        if (typeof(T) == typeof(string))
        {
            return new SecureStorageGetResult<T>(true, false, (T)(object)result.Value!);
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(result.Value!, JsonDefaults.Options);
            return SecureStorageGetResult<T>.Success(value!);
        }
        catch (JsonException ex)
        {
            throw new SecureStoragePlusException($"The stored value for '{key}' could not be deserialized as {typeof(T).Name}.", ex);
        }
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = StorageKeys.Normalize(key);
        return WithLock(() => RemoveCoreAsync(normalized, cancellationToken), cancellationToken);
    }

    public Task RemoveAllAsync(bool resetEncryptionKey = false, CancellationToken cancellationToken = default) =>
        WithLock(() => RemoveAllCoreAsync(resetEncryptionKey, cancellationToken), cancellationToken);

    public Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default) =>
        WithLock(() => RemoveExpiredCoreAsync(cancellationToken), cancellationToken);

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
        return result.Found;
    }

    public Task<IReadOnlyList<string>> GetKeysAsync(bool includeExpired = false, CancellationToken cancellationToken = default) =>
        WithLock(() => GetKeysCoreAsync(includeExpired, cancellationToken), cancellationToken);

    public Task<SecureStorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = StorageKeys.Normalize(key);
        return WithLock(() => GetMetadataCoreAsync(normalized, cancellationToken), cancellationToken);
    }

    public Task<MigrationResult> MigrateFromMauiSecureStorageAsync(
        IEnumerable<string> keys,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        MigrateAsync(new MauiSecureStorageSource(), keys, options, cancellationToken);

    public Task<MigrationResult> MigrateAsync(
        ILegacyStorageSource source,
        IEnumerable<string> keys,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keys);
        return WithLock(() => MigrateCoreAsync(source, keys, options ?? new MigrationOptions(), cancellationToken), cancellationToken);
    }

    async Task SetCoreAsync(string key, string value, SecureStorageOptions? options, CancellationToken cancellationToken)
    {
        var expiresAt = ResolveExpiry(options);
        var dek = await GetOrCreateDekAsync(cancellationToken).ConfigureAwait(false);
        var now = _time.GetUtcNow();
        var payload = _encryptor.Encrypt(
            dek,
            Encoding.UTF8.GetBytes(value),
            StorageKeys.CreateAssociatedData(StorageEnvelope.CurrentVersion, key));

        var envelope = new StorageEnvelope
        {
            Version = StorageEnvelope.CurrentVersion,
            Nonce = Convert.ToBase64String(payload.Nonce),
            CipherText = Convert.ToBase64String(payload.CipherText),
            Tag = Convert.ToBase64String(payload.Tag),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _backend.SetAsync(StorageKeys.ToBackendKey(key), JsonSerializer.Serialize(envelope, JsonDefaults.Options), cancellationToken)
            .ConfigureAwait(false);

        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (!index.Contains(key, StringComparer.Ordinal))
        {
            index.Add(key);
            await SaveIndexAsync(index, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task<SecureStorageGetResult<string>> TryGetCoreAsync(string key, CancellationToken cancellationToken)
    {
        var envelope = await ReadEnvelopeAsync(key, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
        {
            await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
            return SecureStorageGetResult<string>.NotFound();
        }

        if (IsExpired(envelope))
        {
            await DeleteStoredValueAsync(key, cancellationToken).ConfigureAwait(false);
            return SecureStorageGetResult<string>.WasExpired();
        }

        var plaintext = DecryptEnvelope(key, envelope);
        return SecureStorageGetResult<string>.Success(Encoding.UTF8.GetString(plaintext));
    }

    async Task<bool> RemoveCoreAsync(string key, CancellationToken cancellationToken)
    {
        var removed = _backend.Remove(StorageKeys.ToBackendKey(key));
        await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
        return removed;
    }

    async Task RemoveAllCoreAsync(bool resetEncryptionKey, CancellationToken cancellationToken)
    {
        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        foreach (var key in index)
        {
            _backend.Remove(StorageKeys.ToBackendKey(key));
        }

        _backend.Remove(StorageKeys.MetaIndex);
        if (resetEncryptionKey)
        {
            _backend.Remove(StorageKeys.MetaDek);
            if (_dek is not null)
            {
                CryptographicOperations.ZeroMemory(_dek);
                _dek = null;
            }
        }
    }

    async Task<int> RemoveExpiredCoreAsync(CancellationToken cancellationToken)
    {
        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        var removed = 0;
        var remaining = new List<string>(index.Count);

        foreach (var key in index)
        {
            var envelope = await ReadEnvelopeAsync(key, cancellationToken).ConfigureAwait(false);
            if (envelope is null)
            {
                continue;
            }

            if (IsExpired(envelope))
            {
                _backend.Remove(StorageKeys.ToBackendKey(key));
                removed++;
                continue;
            }

            remaining.Add(key);
        }

        await SaveIndexAsync(remaining, cancellationToken).ConfigureAwait(false);
        return removed;
    }

    async Task<IReadOnlyList<string>> GetKeysCoreAsync(bool includeExpired, CancellationToken cancellationToken)
    {
        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (includeExpired)
        {
            return index;
        }

        var live = new List<string>(index.Count);
        var remaining = new List<string>(index.Count);

        foreach (var key in index)
        {
            var envelope = await ReadEnvelopeAsync(key, cancellationToken).ConfigureAwait(false);
            if (envelope is null)
            {
                continue;
            }

            if (IsExpired(envelope))
            {
                _backend.Remove(StorageKeys.ToBackendKey(key));
                continue;
            }

            live.Add(key);
            remaining.Add(key);
        }

        if (remaining.Count != index.Count)
        {
            await SaveIndexAsync(remaining, cancellationToken).ConfigureAwait(false);
        }

        return live;
    }

    async Task<SecureStorageMetadata?> GetMetadataCoreAsync(string key, CancellationToken cancellationToken)
    {
        var envelope = await ReadEnvelopeAsync(key, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
        {
            await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return new SecureStorageMetadata
        {
            Key = key,
            Version = envelope.Version,
            CreatedAt = envelope.CreatedAt,
            ExpiresAt = envelope.ExpiresAt,
            IsExpired = IsExpired(envelope)
        };
    }

    async Task<MigrationResult> MigrateCoreAsync(
        ILegacyStorageSource source,
        IEnumerable<string> keys,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        var migrated = new List<string>();
        var skipped = new List<string>();
        var failures = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rawKey in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string key;
            try
            {
                key = StorageKeys.Normalize(rawKey);
            }
            catch (Exception ex)
            {
                failures[rawKey] = ex.Message;
                continue;
            }

            try
            {
                var existing = await ReadEnvelopeAsync(key, cancellationToken).ConfigureAwait(false);
                if (existing is not null && !IsExpired(existing) && !options.OverwriteExisting)
                {
                    skipped.Add(key);
                    continue;
                }

                var value = await source.GetAsync(key, cancellationToken).ConfigureAwait(false);
                if (value is null)
                {
                    skipped.Add(key);
                    continue;
                }

                await SetCoreAsync(key, value, options.StorageOptions, cancellationToken).ConfigureAwait(false);

                if (options.RemoveSource)
                {
                    await source.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                }

                migrated.Add(key);
            }
            catch (Exception ex)
            {
                failures[key] = ex.Message;
            }
        }

        return new MigrationResult
        {
            MigratedKeys = migrated,
            SkippedKeys = skipped,
            Failures = failures
        };
    }

    async Task<StorageEnvelope?> ReadEnvelopeAsync(string key, CancellationToken cancellationToken)
    {
        var raw = await _backend.GetAsync(StorageKeys.ToBackendKey(key), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<StorageEnvelope>(raw, JsonDefaults.Options);
            if (envelope is null || envelope.Version < 1)
            {
                throw new SecureStoragePlusException($"The stored envelope for '{key}' is invalid.");
            }

            return envelope;
        }
        catch (JsonException ex)
        {
            throw new SecureStoragePlusException($"The stored envelope for '{key}' is corrupt.", ex);
        }
    }

    byte[] DecryptEnvelope(string key, StorageEnvelope envelope)
    {
        if (envelope.Version != StorageEnvelope.CurrentVersion)
        {
            throw new SecureStoragePlusException($"Unsupported secure storage envelope version '{envelope.Version}' for '{key}'.");
        }

        EncryptedPayload payload;
        try
        {
            payload = new EncryptedPayload
            {
                Nonce = Convert.FromBase64String(envelope.Nonce),
                CipherText = Convert.FromBase64String(envelope.CipherText),
                Tag = Convert.FromBase64String(envelope.Tag)
            };
        }
        catch (FormatException ex)
        {
            throw new SecureStoragePlusException($"The stored envelope for '{key}' contains invalid Base64 data.", ex);
        }

        var dek = _dek ?? throw new SecureStoragePlusException("No encryption key is available for the stored value. It may belong to a reset store.");
        return _encryptor.Decrypt(dek, payload, StorageKeys.CreateAssociatedData(envelope.Version, key));
    }

    async Task<byte[]> GetOrCreateDekAsync(CancellationToken cancellationToken)
    {
        if (_dek is not null)
        {
            return _dek;
        }

        var stored = await _backend.GetAsync(StorageKeys.MetaDek, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            try
            {
                _dek = Convert.FromBase64String(stored);
            }
            catch (FormatException ex)
            {
                throw new SecureStoragePlusException("The stored encryption key is corrupt.", ex);
            }

            return _dek;
        }

        _dek = _encryptor.CreateKey();
        await _backend.SetAsync(StorageKeys.MetaDek, Convert.ToBase64String(_dek), cancellationToken).ConfigureAwait(false);
        return _dek;
    }

    async Task<List<string>> LoadIndexAsync(CancellationToken cancellationToken)
    {
        var json = await _backend.GetAsync(StorageKeys.MetaIndex, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonDefaults.Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    Task SaveIndexAsync(List<string> keys, CancellationToken cancellationToken) =>
        _backend.SetAsync(StorageKeys.MetaIndex, JsonSerializer.Serialize(keys, JsonDefaults.Options), cancellationToken);

    async Task RemoveFromIndexAsync(string key, CancellationToken cancellationToken)
    {
        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.RemoveAll(item => string.Equals(item, key, StringComparison.Ordinal)) > 0)
        {
            await SaveIndexAsync(index, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task DeleteStoredValueAsync(string key, CancellationToken cancellationToken)
    {
        _backend.Remove(StorageKeys.ToBackendKey(key));
        await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
    }

    DateTimeOffset? ResolveExpiry(SecureStorageOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        DateTimeOffset? expiresAt = options.ExpiresAt ?? (options.ExpiresIn is { } ttl ? _time.GetUtcNow().Add(ttl) : null);
        if (expiresAt is { } at && at <= _time.GetUtcNow())
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Expiry must be in the future.");
        }

        return expiresAt;
    }

    bool IsExpired(StorageEnvelope envelope) =>
        envelope.ExpiresAt is { } expiresAt && expiresAt <= _time.GetUtcNow();

    async Task<T> WithLock<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureDekReadyForReadAsync(cancellationToken).ConfigureAwait(false);
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    Task WithLock(Func<Task> action, CancellationToken cancellationToken) =>
        WithLock(async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, cancellationToken);

    Task EnsureDekReadyForReadAsync(CancellationToken cancellationToken)
    {
        // Reads need the DEK only when a value exists. Creation happens lazily in Set/Migrate.
        if (_dek is not null)
        {
            return Task.CompletedTask;
        }

        return LoadDekIfPresentAsync(cancellationToken);
    }

    async Task LoadDekIfPresentAsync(CancellationToken cancellationToken)
    {
        var stored = await _backend.GetAsync(StorageKeys.MetaDek, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        try
        {
            _dek = Convert.FromBase64String(stored);
        }
        catch (FormatException ex)
        {
            throw new SecureStoragePlusException("The stored encryption key is corrupt.", ex);
        }
    }
}
