namespace Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;

internal sealed class EpisodeThumbnailSelector : IArtworkImageSelector<EpisodeThumbnailSource>
{
    public IReadOnlyList<EpisodeThumbnailSource> SelectImages(
        IReadOnlyList<EpisodeThumbnailSource> images,
        int maxCount = 10,
        string? language = null
    )
    {
        if (images.Count == 0)
            return [];

        return
        [
            .. images
                .Where(t => string.IsNullOrWhiteSpace(t.Language))
                .OrderByDescending(t => t.Width * t.Height)
                .Take(maxCount)
                .ToList(),
        ];
    }
}
