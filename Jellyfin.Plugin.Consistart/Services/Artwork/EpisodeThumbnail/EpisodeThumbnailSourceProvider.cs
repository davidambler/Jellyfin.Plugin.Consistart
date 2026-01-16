using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;

internal sealed class EpisodeThumbnailSourceProvider(
    ILibraryManager libraryManager,
    ITMDbImagesClient tMDbImagesClient
) : IArtworkImageProvider<EpisodeThumbnailSource>
{
    public async Task<IReadOnlyList<EpisodeThumbnailSource>> GetImagesAsync(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        if (item is not Episode episode)
            return [];

        var series = libraryManager.GetItemById(episode.SeriesId) as Series;
        if (series is null || !series.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        if (!episode.ParentIndexNumber.HasValue || !episode.IndexNumber.HasValue)
            return [];

        var images = await tMDbImagesClient
            .GetImagesAsync(
                tmdbId,
                MediaKind.TvEpisode,
                seasonNumber: episode.ParentIndexNumber.Value,
                episodeNumber: episode.IndexNumber.Value,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (images is null || images.Backdrops.Count == 0)
            return [];

        return
        [
            .. images
                .Backdrops.Where(s => !string.IsNullOrWhiteSpace(s.FilePath))
                .Select(s => new EpisodeThumbnailSource(
                    FilePath: s.FilePath,
                    Width: s.Width,
                    Height: s.Height,
                    Language: s.Iso_639_1
                )),
        ];
    }
}
