namespace Jellyfin.Plugin.Consistart.Services.Artwork;

public sealed record ArtworkCandidateDto(
    string Id,
    string Url,
    int? Width = 0,
    int? Height = 0,
    string? Language = null
);
