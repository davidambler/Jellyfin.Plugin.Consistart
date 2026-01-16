using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;

namespace Jellyfin.Plugin.Consistart.Services.Rendering;

internal sealed class RenderRequestBuilder<T>(ITokenProtectionService tokenProtection)
    : IRenderRequestBuilder<T>
    where T : IRenderRequest
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public string BuildUrl(T request)
    {
        var json = JsonSerializer.Serialize<IRenderRequest>(request, _jsonSerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var token = tokenProtection.Protect(bytes);
        return $"/consistart/render?token={token}";
    }
}
