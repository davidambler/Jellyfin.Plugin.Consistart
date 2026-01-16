namespace Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;

internal sealed class SeasonPosterSelector : IArtworkImageSelector<SeasonPosterSource>
{
    public IReadOnlyList<SeasonPosterSource> SelectImages(
        IReadOnlyList<SeasonPosterSource> images,
        int maxCount = 10,
        string? language = null
    )
    {
        if (images.Count == 0)
            return [];

        return
        [
            .. images
                .Where(p => string.IsNullOrWhiteSpace(p.Language))
                .OrderByDescending(p => p.Width * p.Height)
                .Take(maxCount),
        ];
    }
}
