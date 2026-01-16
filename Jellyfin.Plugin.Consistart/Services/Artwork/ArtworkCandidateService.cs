using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Services.Artwork;

internal sealed class ArtworkCandidateService(
    ILogger<ArtworkCandidateService> logger,
    IEnumerable<IArtworkCandidateGenerator> generators
) : IArtworkCandidateService
{
    public async Task<IReadOnlyList<ArtworkCandidateDto>> GetCandidatesAsync(
        BaseItem item,
        ImageType imageType,
        CancellationToken cancellationToken = default
    )
    {
        var generator = generators.FirstOrDefault(g => g.CanHandle(item, imageType));

        if (generator is null)
        {
            logger.LogWarning(
                "No artwork candidate generator found for item {ItemId} and image type {ImageType}",
                item.Id,
                imageType
            );

            return [];
        }

        return await generator
            .GetCandidatesAsync(item, imageType, cancellationToken)
            .ConfigureAwait(false);
    }
}
