namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Non-secret information about a stored value.
/// </summary>
public sealed class SecureStorageMetadata
{
    /// <summary>
    /// Application key used to store the value.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Envelope format version.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// When the value was written.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the value expires, if a lifetime was configured.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Whether <see cref="ExpiresAt"/> is in the past.
    /// </summary>
    public bool IsExpired { get; init; }
}
