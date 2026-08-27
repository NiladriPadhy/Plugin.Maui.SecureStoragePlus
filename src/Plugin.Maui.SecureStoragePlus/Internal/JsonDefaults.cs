using System.Text.Json;

namespace Plugin.Maui.SecureStoragePlus.Internal;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
