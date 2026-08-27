using Microsoft.Maui.Storage;

namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Reads values from MAUI <see cref="SecureStorage"/>.
/// </summary>
public sealed class MauiSecureStorageSource : ILegacyStorageSource
{
    readonly ISecureStorage _secureStorage;

    /// <summary>
    /// Uses <see cref="SecureStorage.Default"/>.
    /// </summary>
    public MauiSecureStorageSource()
        : this(SecureStorage.Default)
    {
    }

    /// <summary>
    /// Uses a specific <see cref="ISecureStorage"/> instance.
    /// </summary>
    public MauiSecureStorageSource(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _secureStorage.GetAsync(key);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_secureStorage.Remove(key));
    }
}
