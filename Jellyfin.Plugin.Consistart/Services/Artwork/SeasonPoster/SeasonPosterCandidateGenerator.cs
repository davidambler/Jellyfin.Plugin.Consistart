using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;

internal sealed class SeasonPosterCandidateGenerator(
    ILibraryManager library,
    IArtworkImageProvider<SeasonPosterSource> provider,
    IArtworkImageSelector<SeasonPosterSource> selector,
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

        var posters = await provider.GetImagesAsync(item, cancellationToken).ConfigureAwait(false);
        var selected = selector.SelectImages(posters);

        var results = new List<ArtworkCandidateDto>(selected.Count);
        foreach (var poster in selected)
        {
            var request = new SeasonPosterRenderRequest(
                TmdbId: tmdbId,
                SeasonNumber: item.IndexNumber.Value,
                SeasonPosterFilePath: poster.FilePath,
                Preset: DefaultPreset
            );

            var url = renderRequest.BuildUrl(request);
            var id = $"{tmdbId}:season:poster:{item.IndexNumber}:{poster.FilePath}:{DefaultPreset}";

            results.Add(
                new ArtworkCandidateDto(
                    Id: id,
                    Url: url,
                    Width: poster.Width,
                    Height: poster.Height,
                    Language: poster.Language
                )
            );
        }

        return results;
    }
}
