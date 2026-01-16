namespace Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;

public sealed record SeasonPosterSource(
    string FilePath,
    int Width = 0,
    int Height = 0,
    string? Language = null
) : IArtworkImageSource;
