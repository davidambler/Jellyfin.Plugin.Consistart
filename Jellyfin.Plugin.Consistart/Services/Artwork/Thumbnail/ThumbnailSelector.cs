namespace Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;

internal sealed class ThumbnailSelector : IArtworkImageSelector<ThumbnailSource>
{
    public IReadOnlyList<ThumbnailSource> SelectImages(
        IReadOnlyList<ThumbnailSource> images,
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
                .Take(maxCount),
        ];
    }
}
