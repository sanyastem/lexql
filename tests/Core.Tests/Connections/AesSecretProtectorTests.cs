using System.Security.Cryptography;
using Lexql.Core.Connections;

namespace Lexql.Core.Tests.Connections;

public class AesSecretProtectorTests
{
    private static AesSecretProtector Protector() => new(AesSecretProtector.CreateKey());

    [Fact]
    public void ProtectThenUnprotect_RoundTrips()
    {
        var protector = Protector();

        var cipher = protector.Protect("hunter2");

        Assert.Equal("hunter2", protector.Unprotect(cipher));
    }

    [Fact]
    public void Protect_ProducesDifferentCiphertextEachTime()
    {
        var protector = Protector();

        Assert.NotEqual(protector.Protect("same"), protector.Protect("same"));
    }

    [Fact]
    public void Unprotect_WithWrongKey_Throws()
    {
        var cipher = Protector().Protect("secret");
        var other = Protector();

        Assert.Throws<AuthenticationTagMismatchException>(() => other.Unprotect(cipher));
    }

    [Fact]
    public void Unprotect_TamperedCiphertext_Throws()
    {
        var protector = Protector();
        var cipher = protector.Protect("secret");
        var bytes = Convert.FromBase64String(cipher);
        bytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        Assert.Throws<AuthenticationTagMismatchException>(() => protector.Unprotect(tampered));
    }

    [Fact]
    public void Constructor_RejectsBadKeyLength()
    {
        Assert.Throws<ArgumentException>(() => new AesSecretProtector(new byte[10]));
    }
}
