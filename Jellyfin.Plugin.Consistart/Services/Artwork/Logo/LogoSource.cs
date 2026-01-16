namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

public enum LogoSourceKind
{
    Local,
    TMDb,
}

public sealed record LogoSource(
    LogoSourceKind Kind,
    string FilePath,
    int Width = 0,
    int Height = 0,
    string? Language = null
) : IArtworkImageSource;
