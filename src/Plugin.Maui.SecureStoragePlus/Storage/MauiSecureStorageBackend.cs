using Microsoft.Maui.Storage;

namespace Plugin.Maui.SecureStoragePlus.Storage;

internal sealed class MauiSecureStorageBackend : ISecureStorageBackend
{
    readonly ISecureStorage _secureStorage;

    public MauiSecureStorageBackend()
        : this(SecureStorage.Default)
    {
    }

    public MauiSecureStorageBackend(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _secureStorage.SetAsync(key, value);
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _secureStorage.GetAsync(key);
    }

    public bool Remove(string key) => _secureStorage.Remove(key);

    public void RemoveAll() => _secureStorage.RemoveAll();
}
