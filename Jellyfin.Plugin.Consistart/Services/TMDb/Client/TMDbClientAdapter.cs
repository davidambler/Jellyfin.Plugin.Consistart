using System.Diagnostics.CodeAnalysis;
using TMDbLib.Client;
using TMDbLib.Objects.General;

namespace Jellyfin.Plugin.Consistart.Services.TMDb.Client;

[ExcludeFromCodeCoverage]
internal sealed class TMDbClientAdapter(TMDbClient client) : ITMDbClientAdapter
{
    public async Task InitialiseAsync() => await client.GetConfigAsync().ConfigureAwait(false);

    public async Task<ImagesWithId> GetMovieImagesAsync(
        int tmdbid,
        CancellationToken cancellationToken = default
    ) =>
        await client
            .GetMovieImagesAsync(tmdbid, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task<ImagesWithId> GetTvShowImagesAsnync(
        int tmdbid,
        CancellationToken cancellationToken = default
    ) =>
        await client
            .GetTvShowImagesAsync(tmdbid, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task<ImagesWithId> GetTvSeasonImagesAsync(
        int tmdbid,
        int seasonNumber,
        CancellationToken cancellationToken = default
    ) =>
        MapPosterImagesToImagesWithId(
            await client.GetTvSeasonImagesAsync(
                tmdbid,
                seasonNumber,
                cancellationToken: cancellationToken
            )
        );

    public async Task<ImagesWithId> GetTvEpisodeImagesAsync(
        int tmdbid,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default
    )
    {
        var stillImages = await client
            .GetTvEpisodeImagesAsync(
                tmdbid,
                seasonNumber,
                episodeNumber,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (stillImages is null || stillImages.Stills is null)
            return new ImagesWithId();

        // Map stills to backdrops since ImagesWithId doesn't have a Stills property
        return new ImagesWithId { Id = stillImages.Id, Backdrops = stillImages.Stills };
    }

    public Uri GetImageUri(string filePath, string size = "original") =>
        client.GetImageUrl(size, filePath);

    /// <summary>
    /// TV Season images are returned as PosterImages, so we need to map them to ImagesWithId.
    /// </summary>
    /// <param name="images">The PosterImages object to map.</param>
    /// <returns>The mapped ImagesWithId object.</returns>
    private static ImagesWithId MapPosterImagesToImagesWithId(PosterImages images)
    {
        if (images is null)
            return new ImagesWithId();

        return new ImagesWithId { Id = images.Id, Posters = images.Posters };
    }
}
