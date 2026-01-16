using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Poster;

internal sealed class PosterSourceProvider(ITMDbImagesClient tmdbImagesClient)
    : IArtworkImageProvider<PosterSource>
{
    public async Task<IReadOnlyList<PosterSource>> GetImagesAsync(
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

        var images = await tmdbImagesClient
            .GetImagesAsync(tmdbId, mediaKind, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (images is null || images.Posters.Count == 0)
            return [];

        return
        [
            .. images
                .Posters.Where(p => !string.IsNullOrWhiteSpace(p.FilePath))
                .Select(p => new PosterSource(
                    FilePath: p.FilePath,
                    Width: p.Width,
                    Height: p.Height,
                    Language: p.Iso_639_1
                )),
        ];
    }
}
