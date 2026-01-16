using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;

internal sealed class EpisodeThumbnailRenderService(
    ITMDbImagesClient tMDbImagesClient,
    IEpisodeThumbnailRenderer renderer
) : IRenderService<EpisodeThumbnailRenderRequest>
{
    public async Task<RenderedImage?> RenderAsync(
        EpisodeThumbnailRenderRequest request,
        CancellationToken cancellationToken
    )
    {
        var thumbnailBytes = await tMDbImagesClient
            .GetImageBytesAsync(request.ThumbnailFilePath, ImageSize.Original, cancellationToken)
            .ConfigureAwait(false);

        if (thumbnailBytes is null || thumbnailBytes.Length == 0)
            return null;

        var jpeg = await renderer
            .RenderAsync(
                thumbnailBytes,
                request.EpisodeNumber,
                request.EpisodeName,
                cancellationToken
            )
            .ConfigureAwait(false);

        return jpeg is null ? null : new RenderedImage(jpeg, "image/jpeg");
    }
}
