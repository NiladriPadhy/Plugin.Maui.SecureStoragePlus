using System.Security.Cryptography;
using System.Text;
using Plugin.Maui.SecureStoragePlus.Encryption;

namespace Plugin.Maui.SecureStoragePlus.Tests;

public sealed class AesGcmDataEncryptorTests
{
    readonly AesGcmDataEncryptor _encryptor = new();

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var key = _encryptor.CreateKey();
        var plaintext = Encoding.UTF8.GetBytes("refresh-token-value");
        var aad = Encoding.UTF8.GetBytes("ssp.1|auth");

        var payload = _encryptor.Encrypt(key, plaintext, aad);
        var decrypted = _encryptor.Decrypt(key, payload, aad);

        Assert.Equal(plaintext, decrypted);
        Assert.Equal(12, payload.Nonce.Length);
        Assert.Equal(16, payload.Tag.Length);
    }

    [Fact]
    public void Decrypt_WithTamperedCipherText_Throws()
    {
        var key = _encryptor.CreateKey();
        var plaintext = Encoding.UTF8.GetBytes("secret");
        var aad = Encoding.UTF8.GetBytes("aad");
        var payload = _encryptor.Encrypt(key, plaintext, aad);
        payload.CipherText[0] ^= 0xFF;

        Assert.Throws<SecureStoragePlusException>(() => _encryptor.Decrypt(key, payload, aad));
    }

    [Fact]
    public void Decrypt_WithDifferentAssociatedData_Throws()
    {
        var key = _encryptor.CreateKey();
        var payload = _encryptor.Encrypt(key, "value"u8, "ssp.1|one"u8);

        Assert.Throws<SecureStoragePlusException>(() => _encryptor.Decrypt(key, payload, "ssp.1|two"u8));
    }

    [Fact]
    public void Decrypt_WithDifferentKey_Throws()
    {
        var payload = _encryptor.Encrypt(_encryptor.CreateKey(), "value"u8, "aad"u8);

        Assert.Throws<SecureStoragePlusException>(() => _encryptor.Decrypt(_encryptor.CreateKey(), payload, "aad"u8));
    }

    [Fact]
    public void CreateKey_Is32CryptographicallyRandomBytes()
    {
        var first = _encryptor.CreateKey();
        var second = _encryptor.CreateKey();

        Assert.Equal(32, first.Length);
        Assert.NotEqual(first, second);
        Assert.False(CryptographicOperations.FixedTimeEquals(first, new byte[32]));
    }
}
