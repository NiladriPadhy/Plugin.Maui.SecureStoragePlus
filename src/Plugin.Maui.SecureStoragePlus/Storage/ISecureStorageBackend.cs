namespace Plugin.Maui.SecureStoragePlus.Storage;

internal interface ISecureStorageBackend
{
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    bool Remove(string key);

    void RemoveAll();
}
