namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Optional lifetime settings applied when a value is stored.
/// </summary>
public sealed class SecureStorageOptions
{
    /// <summary>
    /// Absolute UTC expiry. Takes precedence over <see cref="ExpiresIn"/> when both are set.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Relative lifetime from the moment the value is written.
    /// </summary>
    public TimeSpan? ExpiresIn { get; init; }

    /// <summary>
    /// Creates options that expire after <paramref name="lifetime"/>.
    /// </summary>
    public static SecureStorageOptions ExpireIn(TimeSpan lifetime) => new() { ExpiresIn = lifetime };

    /// <summary>
    /// Creates options that expire at <paramref name="expiresAt"/>.
    /// </summary>
    public static SecureStorageOptions ExpireAt(DateTimeOffset expiresAt) => new() { ExpiresAt = expiresAt };
}
