using System.Security.Cryptography;
using System.Text;

namespace Lexql.Core.Connections;

public sealed class AesSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesSecretProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException("Key must be 16, 24 or 32 bytes.", nameof(key));
        }

        _key = key;
    }

    public static byte[] CreateKey() => RandomNumberGenerator.GetBytes(32);

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var gcm = new AesGcm(_key, TagSize);
        gcm.Encrypt(nonce, plain, cipher, tag);

        var output = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(output, 0);
        tag.CopyTo(output, NonceSize);
        cipher.CopyTo(output, NonceSize + TagSize);
        return Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);

        var bytes = Convert.FromBase64String(protectedValue);
        if (bytes.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Protected value is too short.");
        }

        var nonce = bytes.AsSpan(0, NonceSize);
        var tag = bytes.AsSpan(NonceSize, TagSize);
        var cipher = bytes.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var gcm = new AesGcm(_key, TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
