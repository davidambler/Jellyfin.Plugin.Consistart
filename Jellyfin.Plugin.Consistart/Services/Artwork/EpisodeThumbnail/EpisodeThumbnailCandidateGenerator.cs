using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;

internal sealed class EpisodeThumbnailCandidateGenerator(
    ILibraryManager libraryManager,
    IArtworkImageProvider<EpisodeThumbnailSource> provider,
    IArtworkImageSelector<EpisodeThumbnailSource> selector,
    ITMDbClientFactory tMDbClientFactory,
    IRenderRequestBuilder<EpisodeThumbnailRenderRequest> renderRequest
) : IArtworkCandidateGenerator
{
    private const string DefaultPreset = "episode.default";

    public bool CanHandle(BaseItem item, ImageType imageType) =>
        item is Episode && imageType == ImageType.Primary;

    public async Task<IReadOnlyList<ArtworkCandidateDto>> GetCandidatesAsync(
        BaseItem item,
        ImageType imageType,
        CancellationToken cancellationToken = default
    )
    {
        if (!CanHandle(item, imageType))
            throw new NotSupportedException(
                $"Item type '{item.GetType().Name}' with image type '{imageType}' is not supported."
            );

        if (item is not Episode episode)
            return [];

        var series = libraryManager.GetItemById(episode.SeriesId) as Series;
        if (series is null || !series.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        if (!episode.ParentIndexNumber.HasValue || !episode.IndexNumber.HasValue)
            return [];

        var stills = await provider.GetImagesAsync(item, cancellationToken).ConfigureAwait(false);
        var selected = selector.SelectImages(stills);

        var results = new List<ArtworkCandidateDto>(selected.Count);

        var client = tMDbClientFactory.CreateClient();
        await client.InitialiseAsync().ConfigureAwait(false);

        foreach (var still in selected)
        {
            var request = new EpisodeThumbnailRenderRequest(
                TmdbId: tmdbId,
                ThumbnailFilePath: still.FilePath,
                EpisodeNumber: episode.IndexNumber.Value,
                EpisodeName: episode.Name,
                Preset: DefaultPreset
            );

            var url = renderRequest.BuildUrl(request);
            var id =
                $"{tmdbId}:episode:{episode.ParentIndexNumber}"
                + $":{episode.IndexNumber}:{still.FilePath}:{DefaultPreset}";

            results.Add(
                new ArtworkCandidateDto(
                    Id: id,
                    Url: url,
                    Width: still.Width,
                    Height: still.Height,
                    Language: still.Language
                )
            );
        }

        return results;
    }
}
