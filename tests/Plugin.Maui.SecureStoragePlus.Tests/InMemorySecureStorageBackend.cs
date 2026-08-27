using Plugin.Maui.SecureStoragePlus.Storage;

namespace Plugin.Maui.SecureStoragePlus.Tests;

sealed class InMemorySecureStorageBackend : ISecureStorageBackend
{
    readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Snapshot =>
        new Dictionary<string, string>(_store, StringComparer.Ordinal);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

    public bool Remove(string key) => _store.Remove(key);

    public void RemoveAll() => _store.Clear();
}
