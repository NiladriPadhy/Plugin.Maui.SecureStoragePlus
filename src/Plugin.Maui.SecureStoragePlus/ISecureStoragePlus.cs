namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Encrypted, expiring key/value storage for .NET MAUI, built on top of platform secure storage.
/// </summary>
public interface ISecureStoragePlus
{
    /// <summary>
    /// Encrypts and stores <paramref name="value"/> under <paramref name="key"/>.
    /// </summary>
    Task SetAsync(string key, string value, SecureStorageOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes, encrypts, and stores <paramref name="value"/> under <paramref name="key"/>.
    /// Strings are stored as-is; other types are JSON-serialized.
    /// </summary>
    Task SetAsync<T>(string key, T value, SecureStorageOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the decrypted value for <paramref name="key"/>, or <c>null</c> when missing or expired.
    /// Expired values are removed automatically.
    /// </summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the decrypted and deserialized value for <paramref name="key"/>, or <c>default</c> when missing or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to read <paramref name="key"/> without throwing for missing or expired values.
    /// </summary>
    Task<SecureStorageGetResult<string>> TryGetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to read and deserialize <paramref name="key"/> without throwing for missing or expired values.
    /// </summary>
    Task<SecureStorageGetResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stored value. Returns <c>true</c> when a value was removed.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every value managed by this plugin. The encryption key is kept unless <paramref name="resetEncryptionKey"/> is <c>true</c>.
    /// </summary>
    Task RemoveAllAsync(bool resetEncryptionKey = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every expired value and returns how many keys were deleted.
    /// </summary>
    Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="key"/> exists and is not expired.
    /// </summary>
    Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the keys currently managed by this plugin.
    /// </summary>
    Task<IReadOnlyList<string>> GetKeysAsync(bool includeExpired = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns metadata for <paramref name="key"/> without exposing the stored secret.
    /// </summary>
    Task<SecureStorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies values from MAUI <see cref="Microsoft.Maui.Storage.SecureStorage"/> into this plugin.
    /// </summary>
    Task<MigrationResult> MigrateFromMauiSecureStorageAsync(IEnumerable<string> keys, MigrationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies values from a custom legacy source (for example Xamarin.Essentials SecureStorage) into this plugin.
    /// </summary>
    Task<MigrationResult> MigrateAsync(ILegacyStorageSource source, IEnumerable<string> keys, MigrationOptions? options = null, CancellationToken cancellationToken = default);
}
