namespace Jellyfin.Plugin.Consistart.Services.Artwork.Poster;

internal sealed class PosterSelector : IArtworkImageSelector<PosterSource>
{
    public IReadOnlyList<PosterSource> SelectImages(
        IReadOnlyList<PosterSource> images,
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
