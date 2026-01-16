namespace Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;

public sealed record EpisodeThumbnailRenderRequest(
    int TmdbId,
    string ThumbnailFilePath,
    int EpisodeNumber,
    string EpisodeName,
    string? Preset
) : IRenderRequest;
