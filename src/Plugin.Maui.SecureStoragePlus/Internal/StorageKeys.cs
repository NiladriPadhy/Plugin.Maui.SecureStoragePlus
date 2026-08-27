using System.Text;

namespace Plugin.Maui.SecureStoragePlus.Internal;

internal static class StorageKeys
{
    public const string DataPrefix = "ssp.d.";
    public const string MetaDek = "ssp.m.dek";
    public const string MetaIndex = "ssp.m.index";
    public const int MaxKeyLength = 128;

    public static string Normalize(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var trimmed = key.Trim();
        if (trimmed.Length > MaxKeyLength)
        {
            throw new ArgumentException($"Key cannot be longer than {MaxKeyLength} characters.", nameof(key));
        }

        if (trimmed.StartsWith("ssp.", StringComparison.Ordinal))
        {
            throw new ArgumentException("Keys reserved by SecureStoragePlus cannot be used.", nameof(key));
        }

        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch))
            {
                throw new ArgumentException("Key cannot contain control characters.", nameof(key));
            }
        }

        return trimmed;
    }

    public static string ToBackendKey(string normalizedKey) => DataPrefix + normalizedKey;

    public static byte[] CreateAssociatedData(int version, string normalizedKey) =>
        Encoding.UTF8.GetBytes($"ssp.{version}|{normalizedKey}");
}
