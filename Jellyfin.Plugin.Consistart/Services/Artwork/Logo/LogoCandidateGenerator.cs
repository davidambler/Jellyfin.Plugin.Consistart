using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

internal sealed class LogoCandidateGenerator(
    IArtworkImageProvider<LogoSource> provider,
    IArtworkImageSelector<LogoSource> selector,
    ITMDbClientFactory tMDbClientFactory
) : IArtworkCandidateGenerator
{
    public bool CanHandle(BaseItem item, ImageType imageType) =>
        item is Movie or Series && imageType == ImageType.Logo;

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

        var logos = await provider.GetImagesAsync(item, cancellationToken).ConfigureAwait(false);
        var language = item.GetPreferredMetadataLanguageSubtag();
        var selected = selector.SelectImages(logos, language: language);

        var results = new List<ArtworkCandidateDto>(selected.Count);

        var client = tMDbClientFactory.CreateClient();
        await client.InitialiseAsync().ConfigureAwait(false);

        foreach (var logo in selected)
        {
            var id = $"{tmdbId}:{kind}:logo:{logo.FilePath}";
            var url = client.GetImageUri(logo.FilePath);

            results.Add(
                new ArtworkCandidateDto(
                    Id: id,
                    Url: url.ToString(),
                    Width: logo.Width,
                    Height: logo.Height,
                    Language: logo.Language
                )
            );
        }
        return results;
    }
}
