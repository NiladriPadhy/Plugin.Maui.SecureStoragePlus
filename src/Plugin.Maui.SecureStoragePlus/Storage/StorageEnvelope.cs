using System.Text.Json.Serialization;

namespace Plugin.Maui.SecureStoragePlus.Storage;

internal sealed class StorageEnvelope
{
    public const int CurrentVersion = 1;

    [JsonPropertyName("v")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("n")]
    public string Nonce { get; set; } = string.Empty;

    [JsonPropertyName("c")]
    public string CipherText { get; set; } = string.Empty;

    [JsonPropertyName("t")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("iat")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("exp")]
    public DateTimeOffset? ExpiresAt { get; set; }
}
