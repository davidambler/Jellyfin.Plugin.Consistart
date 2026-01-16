namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

internal sealed class LogoSelector : IArtworkImageSelector<LogoSource>
{
    public IReadOnlyList<LogoSource> SelectImages(
        IReadOnlyList<LogoSource> images,
        int maxCount = 10,
        string? language = null
    )
    {
        if (images.Count == 0)
            return [];

        return
        [
            .. images
                .Where(p => !p.FilePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                .Where(p => string.Equals(p.Language, language, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Width > l.Height)
                .ThenByDescending(l => (double)l.Width / Math.Max(1, l.Height))
                .ThenByDescending(l => l.Width)
                .Take(maxCount),
        ];
    }
}
