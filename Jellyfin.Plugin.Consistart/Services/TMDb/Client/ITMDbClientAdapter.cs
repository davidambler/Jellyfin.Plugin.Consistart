using TMDbLib.Objects.General;

namespace Jellyfin.Plugin.Consistart.Services.TMDb.Client;

internal interface ITMDbClientAdapter
{
    Task InitialiseAsync();

    Task<ImagesWithId> GetMovieImagesAsync(
        int tmdbid,
        CancellationToken cancellationToken = default
    );

    Task<ImagesWithId> GetTvShowImagesAsnync(
        int tmdbid,
        CancellationToken cancellationToken = default
    );

    Task<ImagesWithId> GetTvSeasonImagesAsync(
        int tmdbid,
        int seasonNumber,
        CancellationToken cancellationToken = default
    );

    Task<ImagesWithId> GetTvEpisodeImagesAsync(
        int tmdbid,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default
    );

    Uri GetImageUri(string filePath, string size = "original");
}
