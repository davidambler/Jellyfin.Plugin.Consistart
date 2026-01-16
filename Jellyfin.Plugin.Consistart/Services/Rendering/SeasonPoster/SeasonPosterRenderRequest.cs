namespace Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;

public sealed record SeasonPosterRenderRequest(
    int TmdbId,
    int SeasonNumber,
    string SeasonPosterFilePath,
    string? Preset
) : IRenderRequest;
