using System.Security.Cryptography;
using System.Text;
using TakeTool.Core.Security;

namespace TakeTool.UI.Windows.Services;

/// <summary>
/// Protects secrets at rest with Windows DPAPI (CurrentUser scope).
/// </summary>
public sealed class DpapiSecretProtector : ISecretProtector
{
    public const string Prefix = "enc.v1:";

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TakeTool.ImgBB.v1");

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return plaintext;
        }

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return string.Empty;
        }

        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            // Legacy plaintext values from earlier builds — return as-is (will be re-protected on next save).
            return storedValue;
        }

        var payload = storedValue[Prefix.Length..];
        var protectedBytes = Convert.FromBase64String(payload);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
