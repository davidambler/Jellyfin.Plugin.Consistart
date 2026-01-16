namespace Jellyfin.Plugin.Consistart.Services.Rendering;

internal interface IRenderRequestBuilder<T>
    where T : IRenderRequest
{
    string BuildUrl(T request);
}
