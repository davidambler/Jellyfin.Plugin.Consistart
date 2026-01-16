using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Poster;

internal sealed class PosterRenderService(
    ITMDbImagesClient tMDbImagesClient,
    IPosterRenderer renderer,
    ILocalFileReader localFileReader
) : IRenderService<PosterRenderRequest>
{
    public async Task<RenderedImage?> RenderAsync(
        PosterRenderRequest request,
        CancellationToken cancellationToken
    )
    {
        var posterBytes = await tMDbImagesClient
            .GetImageBytesAsync(request.PosterFilePath, ImageSize.Original, cancellationToken)
            .ConfigureAwait(false);

        if (posterBytes is null || posterBytes.Length == 0)
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
            .RenderAsync(posterBytes, logoBytes, cancellationToken)
            .ConfigureAwait(false);

        return jpeg is null ? null : new RenderedImage(jpeg, "image/jpeg");
    }
}
