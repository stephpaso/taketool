using System.Text;

namespace TakeTool.Core.Security;

public interface ISecretProtector
{
    /// <summary>
    /// Protects a secret for at-rest storage. Idempotent for already-protected values.
    /// </summary>
    string Protect(string plaintext);

    /// <summary>
    /// Returns plaintext. Passes through values that are not protected payloads.
    /// </summary>
    string Unprotect(string storedValue);
}

/// <summary>
/// Development/test protector that does not encrypt. Production Windows host must register DPAPI.
/// </summary>
public sealed class PassthroughSecretProtector : ISecretProtector
{
    public string Protect(string plaintext) => plaintext ?? string.Empty;

    public string Unprotect(string storedValue) => storedValue ?? string.Empty;
}
