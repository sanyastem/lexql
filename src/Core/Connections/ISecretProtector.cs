namespace Lexql.Core.Connections;

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
