using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.Consistart.Services.TokenProtection;

internal sealed class TokenProtectionService(IDataProtectionProvider dataProtectionProvider)
    : ITokenProtectionService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "Jellyfin.Plugin.Consistart.Token"
    );

    public string Protect(byte[] data)
    {
        var protectedBytes = _protector.Protect(data);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    public byte[] Unprotect(string protectedToken)
    {
        var protectedBytes = WebEncoders.Base64UrlDecode(protectedToken);
        return _protector.Unprotect(protectedBytes);
    }
}
