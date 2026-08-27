namespace Plugin.Maui.SecureStoragePlus.Encryption;

internal sealed class EncryptedPayload
{
    public required byte[] Nonce { get; init; }

    public required byte[] CipherText { get; init; }

    public required byte[] Tag { get; init; }
}
