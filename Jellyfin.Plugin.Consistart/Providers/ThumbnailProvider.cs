using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Providers;

public sealed class ThumbnailProvider(
    IConfigurationProvider configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ThumbnailProvider> logger,
    IArtworkCandidateService candidateService
)
    : ConsistartProvider<ThumbnailProvider>(
        configuration,
        httpClientFactory,
        logger,
        candidateService
    )
{
    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Thumb];

    public override bool Supports(BaseItem item) => item is Movie or Series;

    public override async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        var candidates = await CandidateService
            .GetCandidatesAsync(item, ImageType.Thumb, cancellationToken)
            .ConfigureAwait(false);

        return candidates.Select(c => CreateRemoteImageInfo(c, ImageType.Thumb, useBaseUrl: true));
    }
}
