namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Adapts arbitrary get/remove callbacks, for example Xamarin.Essentials or <c>LegacySecureStorage</c>.
/// </summary>
public sealed class DelegateLegacyStorageSource : ILegacyStorageSource
{
    readonly Func<string, CancellationToken, Task<string?>> _getAsync;
    readonly Func<string, CancellationToken, Task<bool>>? _removeAsync;

    /// <summary>
    /// Creates a source from get and optional remove callbacks.
    /// </summary>
    public DelegateLegacyStorageSource(
        Func<string, CancellationToken, Task<string?>> getAsync,
        Func<string, CancellationToken, Task<bool>>? removeAsync = null)
    {
        _getAsync = getAsync ?? throw new ArgumentNullException(nameof(getAsync));
        _removeAsync = removeAsync;
    }

    /// <summary>
    /// Creates a source from get and optional remove callbacks without a cancellation token.
    /// </summary>
    public DelegateLegacyStorageSource(
        Func<string, Task<string?>> getAsync,
        Func<string, Task<bool>>? removeAsync = null)
    {
        ArgumentNullException.ThrowIfNull(getAsync);
        _getAsync = (key, _) => getAsync(key);
        _removeAsync = removeAsync is null
            ? null
            : (key, _) => removeAsync(key);
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        _getAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _removeAsync is null
            ? Task.FromResult(false)
            : _removeAsync(key, cancellationToken);
}
