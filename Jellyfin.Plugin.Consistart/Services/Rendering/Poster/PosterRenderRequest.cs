using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Poster;

public sealed record PosterRenderRequest(
    MediaKind MediaKind,
    int TmdbId,
    string PosterFilePath,
    LogoSource LogoSource,
    string? Preset
) : IRenderRequest;
