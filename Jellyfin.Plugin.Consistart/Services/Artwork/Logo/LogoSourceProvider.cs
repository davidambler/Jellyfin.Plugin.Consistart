using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

internal sealed class LogoSourceProvider(ITMDbImagesClient tmdbImagesClient)
    : IArtworkImageProvider<LogoSource>
{
    public async Task<IReadOnlyList<LogoSource>> GetImagesAsync(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        if (!item.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        var mediaKind = item switch
        {
            Movie => MediaKind.Movie,
            Series => MediaKind.TvShow,
            _ => throw new NotSupportedException(
                $"Item type '{item.GetType().Name}' is not supported for logo retrieval."
            ),
        };

        var images = await tmdbImagesClient
            .GetImagesAsync(tmdbId, mediaKind, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (images is null || images.Logos.Count == 0)
            return [];

        return
        [
            .. images
                .Logos.Where(l => !string.IsNullOrWhiteSpace(l.FilePath))
                .Select(l => new LogoSource(
                    Kind: LogoSourceKind.TMDb,
                    FilePath: l.FilePath,
                    Width: l.Width,
                    Height: l.Height,
                    Language: l.Iso_639_1
                )),
        ];
    }
}
