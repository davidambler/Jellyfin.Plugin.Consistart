using Jellyfin.Plugin.Consistart.Extensions;
using Jellyfin.Plugin.Consistart.Infrastructure.Concurrency;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TMDbLib.Objects.General;

namespace Jellyfin.Plugin.Consistart.Services.TMDb.Images;

internal sealed class TMDbImagesClient(
    IHostApplicationLifetime lifetime,
    IHttpClientFactory httpClientFactory,
    ITMDbClientFactory tmdbClientFactory,
    ILogger<TMDbImagesClient> logger,
    IMemoryCache cache
) : ITMDbImagesClient
{
    private readonly SingleFlight<string, ImagesWithId> _singleFlight = new(StringComparer.Ordinal);

    public async Task<ImagesWithId> GetImagesAsync(
        int tmdbId,
        MediaKind mediaKind,
        int? seasonNumber = null,
        int? episodeNumber = null,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = $"tmdb:images:{seasonNumber ?? 0}:{episodeNumber ?? 0}:{mediaKind}:{tmdbId}";

        if (
            cache.TryGetWithLogging<ImagesWithId>(cacheKey, logger, out var cached)
            && cached != null
        )
            return cached;

        return await _singleFlight.RunAsync(
            cacheKey,
            operationCancellation: lifetime.ApplicationStopping,
            waiterCancellation: cancellationToken,
            operation: async ct =>
            {
                if (
                    cache.TryGetWithLogging<ImagesWithId>(cacheKey, logger, out var cached)
                    && cached != null
                )
                    return cached;

                var client = tmdbClientFactory.CreateClient();
                await client.InitialiseAsync().ConfigureAwait(false);

                var images = mediaKind switch
                {
                    MediaKind.Movie => await client
                        .GetMovieImagesAsync(tmdbId, ct)
                        .ConfigureAwait(false),
                    MediaKind.TvShow => await client
                        .GetTvShowImagesAsnync(tmdbId, ct)
                        .ConfigureAwait(false),
                    MediaKind.TvSeason when seasonNumber.HasValue => await client
                        .GetTvSeasonImagesAsync(tmdbId, seasonNumber.Value, ct)
                        .ConfigureAwait(false),
                    MediaKind.TvEpisode when seasonNumber.HasValue && episodeNumber.HasValue =>
                        await client
                            .GetTvEpisodeImagesAsync(
                                tmdbId,
                                seasonNumber.Value,
                                episodeNumber.Value,
                                ct
                            )
                            .ConfigureAwait(false),
                    _ => new ImagesWithId(),
                };

                if (images is null)
                {
                    logger.LogWarning(
                        "No images found for {MediaKind} with TMDb ID {TmdbId}",
                        mediaKind,
                        tmdbId
                    );
                }

                cache.SetWithLogging(
                    cacheKey,
                    images,
                    logger,
                    images is null ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(6)
                );

                return images ?? new ImagesWithId();
            }
        );
    }

    public async Task<byte[]> GetImageBytesAsync(
        string filePath,
        string size = ImageSize.Original,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var tmdbClient = tmdbClientFactory.CreateClient();
        await tmdbClient.InitialiseAsync().ConfigureAwait(false);

        var imageUri = tmdbClient.GetImageUri(filePath, size);
        var httpClient = httpClientFactory.CreateClient();

        try
        {
            return await httpClient
                .GetByteArrayAsync(imageUri, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download image from {ImageUri}", imageUri);
            throw;
        }
    }
}
