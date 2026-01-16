namespace Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;

public sealed record ThumbnailSource(
    string FilePath,
    int Width = 0,
    int Height = 0,
    string? Language = null
) : IArtworkImageSource;
