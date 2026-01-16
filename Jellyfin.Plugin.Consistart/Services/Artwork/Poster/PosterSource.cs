namespace Jellyfin.Plugin.Consistart.Services.Artwork.Poster;

public sealed record PosterSource(
    string FilePath,
    int Width = 0,
    int Height = 0,
    string? Language = null
) : IArtworkImageSource;
