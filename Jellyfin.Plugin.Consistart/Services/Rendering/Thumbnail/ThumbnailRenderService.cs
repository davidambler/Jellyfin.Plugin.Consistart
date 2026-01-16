using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;

internal class ThumbnailRenderService(
    ITMDbImagesClient tMDbImagesClient,
    IThumbnailRenderer renderer,
    ILocalFileReader localFileReader
) : IRenderService<ThumbnailRenderRequest>
{
    public async Task<RenderedImage?> RenderAsync(
        ThumbnailRenderRequest request,
        CancellationToken cancellationToken
    )
    {
        var thumbnailBytes = await tMDbImagesClient
            .GetImageBytesAsync(request.ThumbnailFilePath, ImageSize.Original, cancellationToken)
            .ConfigureAwait(false);

        if (thumbnailBytes is null || thumbnailBytes.Length == 0)
            return null;

        var logoBytes = request.LogoSource.Kind switch
        {
            LogoSourceKind.Local => await localFileReader
                .TryReadAllBytesAsync(request.LogoSource.FilePath, cancellationToken)
                .ConfigureAwait(false)
                ?? null,
            LogoSourceKind.TMDb => await tMDbImagesClient
                .GetImageBytesAsync(
                    request.LogoSource.FilePath,
                    ImageSize.Original,
                    cancellationToken
                )
                .ConfigureAwait(false)
                ?? null,
            _ => null,
        };

        if (logoBytes is null || logoBytes.Length == 0)
            return null;

        var jpeg = await renderer
            .RenderAsync(thumbnailBytes, logoBytes, cancellationToken)
            .ConfigureAwait(false);

        return jpeg is null ? null : new RenderedImage(jpeg, "image/jpeg");
    }
}
