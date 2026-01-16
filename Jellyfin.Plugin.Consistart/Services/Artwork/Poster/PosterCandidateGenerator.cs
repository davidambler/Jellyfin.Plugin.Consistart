using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Poster;

internal sealed class PosterCandidateGenerator(
    IArtworkImageProvider<PosterSource> provider,
    IArtworkImageSelector<PosterSource> selector,
    ILocalLogoProvider localLogoProvider,
    IArtworkImageProvider<LogoSource> remoteLogoProvider,
    IArtworkImageSelector<LogoSource> remoteLogoSelector,
    IRenderRequestBuilder<PosterRenderRequest> renderRequest
) : IArtworkCandidateGenerator
{
    private const string DefaultPreset = "poster.default";

    public bool CanHandle(BaseItem item, ImageType imageType) =>
        item is Movie or Series && imageType == ImageType.Primary;

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

        if (!item.TryGetProviderIdAsInt(MetadataProvider.Tmdb, out var tmdbId))
            return [];

        var kind = item is Movie ? MediaKind.Movie : MediaKind.TvShow;

        var posters = await provider.GetImagesAsync(item, cancellationToken).ConfigureAwait(false);
        var selected = selector.SelectImages(posters);

        var logo = await localLogoProvider
            .TryGetLocalLogoAsync(item, cancellationToken)
            .ConfigureAwait(false);

        if (logo is null)
        {
            var logos = await remoteLogoProvider
                .GetImagesAsync(item, cancellationToken)
                .ConfigureAwait(false);
            var language = item.GetPreferredMetadataLanguageSubtag();
            var remoteSelected = remoteLogoSelector.SelectImages(logos, language: language);
            if (remoteSelected.Count == 0)
                return [];

            logo = remoteSelected[0];
        }

        var results = new List<ArtworkCandidateDto>(selected.Count);
        foreach (var poster in selected)
        {
            var request = new PosterRenderRequest(
                MediaKind: kind,
                TmdbId: tmdbId,
                PosterFilePath: poster.FilePath,
                LogoSource: logo,
                Preset: DefaultPreset
            );

            var url = renderRequest.BuildUrl(request);
            var id = $"{tmdbId}:{kind}:poster:{poster.FilePath}:{DefaultPreset}";

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
