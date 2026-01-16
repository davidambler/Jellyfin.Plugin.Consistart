using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Providers;

public sealed class LogoProvider(
    IConfigurationProvider configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<LogoProvider> logger,
    IArtworkCandidateService candidateService
) : ConsistartProvider<LogoProvider>(configuration, httpClientFactory, logger, candidateService)
{
    public override bool Supports(BaseItem item) => item is Movie or Series;

    public override async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        var candidates = await CandidateService
            .GetCandidatesAsync(item, ImageType.Logo, cancellationToken)
            .ConfigureAwait(false);

        return candidates.Select(c => CreateRemoteImageInfo(c, ImageType.Logo));
    }

    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Logo];
}
