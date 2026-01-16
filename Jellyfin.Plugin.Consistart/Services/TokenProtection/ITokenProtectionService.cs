namespace Jellyfin.Plugin.Consistart.Services.TokenProtection;

public interface ITokenProtectionService
{
    /// <summary>
    /// Encrypts and encodes data into a protected token.
    /// </summary>
    /// <param name="data">The data to protect.</param>
    /// <returns>A protected token string.</returns>
    string Protect(byte[] data);

    /// <summary>
    /// Decrypts and decodes a protected token.
    /// </summary>
    /// <param name="protectedToken">The protected token to unprotect.</param>
    /// <returns>The original data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">If the token is invalid or tampered with.</exception>
    byte[] Unprotect(string protectedToken);
}
