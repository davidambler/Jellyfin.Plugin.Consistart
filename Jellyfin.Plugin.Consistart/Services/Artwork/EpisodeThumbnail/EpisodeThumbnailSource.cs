namespace Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;

public sealed record EpisodeThumbnailSource(
    string FilePath,
    int Width = 0,
    int Height = 0,
    string? Language = null
) : IArtworkImageSource;
