namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Reads (and optionally deletes) values from a previous secure storage implementation.
/// </summary>
public interface ILegacyStorageSource
{
    /// <summary>
    /// Returns the plaintext value for <paramref name="key"/>, or <c>null</c> when it does not exist.
    /// </summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes <paramref name="key"/> from the source store. Returns <c>true</c> when a value was removed.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}
