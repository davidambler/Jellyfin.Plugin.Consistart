using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;

internal sealed class SeasonPosterSourceProvider(
    ILibraryManager library,
    ITMDbImagesClient tmdbImagesClient
) : IArtworkImageProvider<SeasonPosterSource>
{
    public async Task<IReadOnlyList<SeasonPosterSource>> GetImagesAsync(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        var parent = library.GetItemById(item.ParentId);
        if (parent is null)
            return [];

        if (!parent.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        if (item is not Season season)
            throw new NotSupportedException(
                $"Item type '{item.GetType().Name}' is not supported for season poster retrieval."
            );

        if (item.IndexNumber == null)
            return [];

        var images = await tmdbImagesClient
            .GetImagesAsync(
                tmdbId,
                MediaKind.TvSeason,
                season.IndexNumber,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (images is null || images.Posters.Count == 0)
            return [];

        return
        [
            .. images
                .Posters.Where(p => !string.IsNullOrWhiteSpace(p.FilePath))
                .Select(p => new SeasonPosterSource(
                    FilePath: p.FilePath,
                    Width: p.Width,
                    Height: p.Height,
                    Language: p.Iso_639_1
                )),
        ];
    }
}
