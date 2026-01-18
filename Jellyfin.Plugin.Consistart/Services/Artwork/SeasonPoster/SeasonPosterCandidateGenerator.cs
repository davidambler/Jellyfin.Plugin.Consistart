using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.Artwork.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;

internal sealed class SeasonPosterCandidateGenerator(
    ILibraryManager library,
    IArtworkImageProvider<SeasonPosterSource> seasonPosterProvider,
    IArtworkImageProvider<PosterSource> parentPosterImageProvider,
    IArtworkImageSelector<SeasonPosterSource> selector,
    IArtworkImageSelector<PosterSource> parentSelector,
    IRenderRequestBuilder<SeasonPosterRenderRequest> renderRequest
) : IArtworkCandidateGenerator
{
    private const string DefaultPreset = "season.default";

    public bool CanHandle(BaseItem item, ImageType imageType) =>
        item is Season && imageType == ImageType.Primary;

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

        var parent = library.GetItemById(item.ParentId);
        if (parent is null)
            return [];

        if (!parent.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        if (item.IndexNumber is null)
            return [];

        var posters = await seasonPosterProvider
            .GetImagesAsync(item, cancellationToken)
            .ConfigureAwait(false);
        var selected = selector.SelectImages(posters);

        var results = new List<ArtworkCandidateDto>();
        AddResults(results, selected, tmdbId, item.IndexNumber.Value, fallback: false);

        if (results.Count == 0) // Fallback to parent posters if no season posters found
        {
            var parentPosters = await parentPosterImageProvider
                .GetImagesAsync(parent, cancellationToken)
                .ConfigureAwait(false);
            var parentSelected = parentSelector.SelectImages(parentPosters);
            if (parentSelected.Count == 0)
                return [];

            AddResults(results, parentSelected, tmdbId, item.IndexNumber.Value, fallback: true);
        }

        return results;
    }

    /// <summary>
    /// Adds artwork candidates to the results list.
    /// </summary>
    /// <typeparam name="T">The type of artwork image source.</typeparam>
    /// <param name="results">The list to add results to.</param>
    /// <param name="posters">The list of posters to process.</param>
    /// <param name="tmdbId">The TMDb ID of the parent show.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="fallback">Indicates if this is a fallback from parent posters.</param>
    private void AddResults<T>(
        List<ArtworkCandidateDto> results,
        IReadOnlyList<T> posters,
        int tmdbId,
        int seasonNumber,
        bool fallback
    )
        where T : IArtworkImageSource
    {
        var suffix = fallback ? ":parent" : string.Empty;

        foreach (var poster in posters)
        {
            var filePath = ((dynamic)poster).FilePath;
            var width = ((dynamic)poster).Width;
            var height = ((dynamic)poster).Height;
            var language = ((dynamic)poster).Language;

            var request = new SeasonPosterRenderRequest(
                TmdbId: tmdbId,
                SeasonNumber: seasonNumber,
                SeasonPosterFilePath: filePath,
                Preset: DefaultPreset
            );

            var url = renderRequest.BuildUrl(request);
            var id = $"{tmdbId}:season:poster:{seasonNumber}:{filePath}:{DefaultPreset}{suffix}";

            results.Add(
                new ArtworkCandidateDto(
                    Id: id,
                    Url: url,
                    Width: width,
                    Height: height,
                    Language: language
                )
            );
        }
    }
}
