using System.Runtime.Versioning;
using System.Security.Cryptography;
using Lexql.Core.Connections;
using Microsoft.Maui.Storage;

namespace Lexql.App.Services;

public static class KeyStore
{
    public static byte[] GetOrCreateKey()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "lexql.key");
        if (File.Exists(path))
        {
            return Unwrap(File.ReadAllBytes(path));
        }

        var key = AesSecretProtector.CreateKey();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Wrap(key));
        return key;
    }

    private static byte[] Wrap(byte[] key) =>
        OperatingSystem.IsWindows() ? ProtectOnWindows(key) : key;

    private static byte[] Unwrap(byte[] stored) =>
        OperatingSystem.IsWindows() ? UnprotectOnWindows(stored) : stored;

    [SupportedOSPlatform("windows")]
    private static byte[] ProtectOnWindows(byte[] key) =>
        ProtectedData.Protect(key, optionalEntropy: null, DataProtectionScope.CurrentUser);

    [SupportedOSPlatform("windows")]
    private static byte[] UnprotectOnWindows(byte[] stored) =>
        ProtectedData.Unprotect(stored, optionalEntropy: null, DataProtectionScope.CurrentUser);
}
