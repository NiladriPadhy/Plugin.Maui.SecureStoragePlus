using System.Security.Cryptography;

namespace Plugin.Maui.SecureStoragePlus.Encryption;

internal sealed class AesGcmDataEncryptor : IDataEncryptor
{
    public const int KeySizeBytes = 32;
    public const int NonceSizeBytes = 12;
    public const int TagSizeBytes = 16;

    public byte[] CreateKey()
    {
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public EncryptedPayload Encrypt(byte[] key, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateKey(key);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, cipherText, tag, associatedData);

        return new EncryptedPayload
        {
            Nonce = nonce,
            CipherText = cipherText,
            Tag = tag
        };
    }

    public byte[] Decrypt(byte[] key, EncryptedPayload payload, ReadOnlySpan<byte> associatedData)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(payload);
        ValidateKey(key);

        if (payload.Nonce is not { Length: NonceSizeBytes })
        {
            throw new SecureStoragePlusException("Encrypted payload nonce is invalid.");
        }

        if (payload.Tag is not { Length: TagSizeBytes })
        {
            throw new SecureStoragePlusException("Encrypted payload authentication tag is invalid.");
        }

        if (payload.CipherText is null)
        {
            throw new SecureStoragePlusException("Encrypted payload ciphertext is missing.");
        }

        var plaintext = new byte[payload.CipherText.Length];

        try
        {
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(payload.Nonce, payload.CipherText, payload.Tag, plaintext, associatedData);
        }
        catch (CryptographicException ex)
        {
            throw new SecureStoragePlusException("The stored value could not be decrypted. It may be corrupt or was written with a different key.", ex);
        }

        return plaintext;
    }

    static void ValidateKey(byte[] key)
    {
        if (key.Length != KeySizeBytes)
        {
            throw new SecureStoragePlusException($"Encryption key must be {KeySizeBytes} bytes.");
        }
    }
}
