namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Controls how values are copied from a legacy secure store.
/// </summary>
public sealed class MigrationOptions
{
    /// <summary>
    /// When <c>true</c>, each successfully migrated key is removed from the source store. Default is <c>true</c>.
    /// </summary>
    public bool RemoveSource { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, an existing SecureStoragePlus value is replaced. Default is <c>false</c>.
    /// </summary>
    public bool OverwriteExisting { get; init; }

    /// <summary>
    /// Optional lifetime applied to every migrated value.
    /// </summary>
    public SecureStorageOptions? StorageOptions { get; init; }
}
