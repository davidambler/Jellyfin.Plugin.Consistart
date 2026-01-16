using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;

public sealed record ThumbnailRenderRequest(
    MediaKind MediaKind,
    int TmdbId,
    string ThumbnailFilePath,
    LogoSource LogoSource,
    string? Preset
) : IRenderRequest;
