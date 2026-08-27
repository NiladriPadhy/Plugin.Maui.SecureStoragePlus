namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Outcome of a migration batch.
/// </summary>
public sealed class MigrationResult
{
    /// <summary>
    /// Keys that were copied into SecureStoragePlus.
    /// </summary>
    public IReadOnlyList<string> MigratedKeys { get; init; } = [];

    /// <summary>
    /// Keys that were left untouched (missing in source, or already present when overwrite is off).
    /// </summary>
    public IReadOnlyList<string> SkippedKeys { get; init; } = [];

    /// <summary>
    /// Keys that failed, mapped to an error message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Failures { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Number of keys that were migrated.
    /// </summary>
    public int Migrated => MigratedKeys.Count;

    /// <summary>
    /// Number of keys that were skipped.
    /// </summary>
    public int Skipped => SkippedKeys.Count;

    /// <summary>
    /// Number of keys that failed.
    /// </summary>
    public int Failed => Failures.Count;
}
