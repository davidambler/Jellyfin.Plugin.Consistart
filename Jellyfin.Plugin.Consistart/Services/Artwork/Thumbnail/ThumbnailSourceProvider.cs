using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;

internal sealed class ThumbnailSourceProvider(ITMDbImagesClient tMDbImagesClient)
    : IArtworkImageProvider<ThumbnailSource>
{
    public async Task<IReadOnlyList<ThumbnailSource>> GetImagesAsync(
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
                $"Item type '{item.GetType().Name}' is not supported for poster retrieval."
            ),
        };

        var images = await tMDbImagesClient
            .GetImagesAsync(tmdbId, mediaKind, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (images is null || images.Backdrops.Count == 0)
            return [];

        return
        [
            .. images
                .Backdrops.Where(t => !string.IsNullOrWhiteSpace(t.FilePath))
                .Select(t => new ThumbnailSource(
                    FilePath: t.FilePath,
                    Width: t.Width,
                    Height: t.Height,
                    Language: t.Iso_639_1
                )),
        ];
    }
}
