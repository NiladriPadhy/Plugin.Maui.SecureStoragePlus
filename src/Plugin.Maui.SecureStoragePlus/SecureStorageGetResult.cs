namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Result of a non-throwing read.
/// </summary>
/// <typeparam name="T">The decrypted value type.</typeparam>
public sealed class SecureStorageGetResult<T>
{
    /// <summary>
    /// Creates a result describing a successful or unsuccessful read.
    /// </summary>
    public SecureStorageGetResult(bool found, bool expired, T? value)
    {
        Found = found;
        Expired = expired;
        Value = value;
    }

    /// <summary>
    /// <c>true</c> when a live value was decrypted.
    /// </summary>
    public bool Found { get; }

    /// <summary>
    /// <c>true</c> when a value existed but had already expired and was removed.
    /// </summary>
    public bool Expired { get; }

    /// <summary>
    /// The decrypted value when <see cref="Found"/> is <c>true</c>.
    /// </summary>
    public T? Value { get; }

    internal static SecureStorageGetResult<T> NotFound() => new(false, false, default);

    internal static SecureStorageGetResult<T> WasExpired() => new(false, true, default);

    internal static SecureStorageGetResult<T> Success(T value) => new(true, false, value);
}
