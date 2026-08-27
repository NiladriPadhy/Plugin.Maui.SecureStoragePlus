namespace Plugin.Maui.SecureStoragePlus.Tests;

sealed class InMemoryLegacySource : ILegacyStorageSource
{
    readonly Dictionary<string, string> _store;

    public InMemoryLegacySource(IDictionary<string, string>? values = null)
    {
        _store = values is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    public bool Contains(string key) => _store.ContainsKey(key);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Remove(key));
}
