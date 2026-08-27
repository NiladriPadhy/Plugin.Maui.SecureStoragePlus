namespace Plugin.Maui.SecureStoragePlus.Encryption;

internal interface IDataEncryptor
{
    byte[] CreateKey();

    EncryptedPayload Encrypt(byte[] key, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData);

    byte[] Decrypt(byte[] key, EncryptedPayload payload, ReadOnlySpan<byte> associatedData);
}
