using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;

internal sealed class SeasonPosterRenderService(
    ITMDbImagesClient tMDbImagesClient,
    ISeasonPosterRenderer renderer
) : IRenderService<SeasonPosterRenderRequest>
{
    public async Task<RenderedImage?> RenderAsync(
        SeasonPosterRenderRequest request,
        CancellationToken cancellationToken
    )
    {
        var posterBytes = await tMDbImagesClient
            .GetImageBytesAsync(request.SeasonPosterFilePath, ImageSize.Original, cancellationToken)
            .ConfigureAwait(false);

        if (posterBytes is null || posterBytes.Length == 0)
            return null;

        var jpeg = await renderer
            .RenderAsync(posterBytes, request.SeasonNumber, cancellationToken)
            .ConfigureAwait(false);

        return jpeg is null ? null : new RenderedImage(jpeg, "image/jpeg");
    }
}
