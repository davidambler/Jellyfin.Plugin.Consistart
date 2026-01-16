using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using TMDbLib.Objects.General;

namespace Jellyfin.Plugin.Consistart.Tests.TestDoubles;

/// <summary>
/// Fake implementation of ITMDbClientAdapter for testing purposes.
/// Allows configuration of responses for different TMDb IDs and tracks method calls.
/// </summary>
internal sealed class FakeTMDbClientAdapter : ITMDbClientAdapter
{
    private readonly Dictionary<int, ImagesWithId> _movieImages = [];
    private readonly Dictionary<int, ImagesWithId> _tvShowImages = [];
    private readonly Dictionary<(int TmdbId, int Season), ImagesWithId> _tvSeasonImages = [];
    private readonly Dictionary<
        (int TmdbId, int Season, int Episode),
        ImagesWithId
    > _tvEpisodeImages = [];
    private readonly Dictionary<string, Uri> _imageUris = [];

    public bool InitialiseAsyncCalled { get; private set; }
    public int InitialiseCallCount { get; private set; }
    public List<int> MovieImageRequests { get; } = [];
    public List<int> TvShowImageRequests { get; } = [];
    public List<(int TmdbId, int Season)> TvSeasonImageRequests { get; } = [];
    public List<(int TmdbId, int Season, int Episode)> TvEpisodeImageRequests { get; } = [];
    public List<(string FilePath, string Size)> ImageUriRequests { get; } = [];

    public bool ThrowOnInitialise { get; set; }
    public bool ThrowOnMovieImages { get; set; }
    public bool ThrowOnTvShowImages { get; set; }
    public bool ThrowOnTvSeasonImages { get; set; }
    public bool ThrowOnTvEpisodeImages { get; set; }

    public Task InitialiseAsync()
    {
        InitialiseAsyncCalled = true;
        InitialiseCallCount++;

        if (ThrowOnInitialise)
        {
            throw new InvalidOperationException("Fake initialization failed");
        }

        return Task.CompletedTask;
    }

    public Task<ImagesWithId> GetMovieImagesAsync(
        int tmdbid,
        CancellationToken cancellationToken = default
    )
    {
        MovieImageRequests.Add(tmdbid);

        if (ThrowOnMovieImages)
        {
            throw new InvalidOperationException($"Fake error getting movie images for {tmdbid}");
        }

        if (_movieImages.TryGetValue(tmdbid, out var images))
        {
            return Task.FromResult(images);
        }

        return Task.FromResult(new ImagesWithId { Id = tmdbid });
    }

    public Task<ImagesWithId> GetTvShowImagesAsnync(
        int tmdbid,
        CancellationToken cancellationToken = default
    )
    {
        TvShowImageRequests.Add(tmdbid);

        if (ThrowOnTvShowImages)
        {
            throw new InvalidOperationException($"Fake error getting TV show images for {tmdbid}");
        }

        if (_tvShowImages.TryGetValue(tmdbid, out var images))
        {
            return Task.FromResult(images);
        }

        return Task.FromResult(new ImagesWithId { Id = tmdbid });
    }

    public Task<ImagesWithId> GetTvSeasonImagesAsync(
        int tmdbid,
        int seasonNumber,
        CancellationToken cancellationToken = default
    )
    {
        var key = (tmdbid, seasonNumber);
        TvSeasonImageRequests.Add(key);

        if (ThrowOnTvSeasonImages)
        {
            throw new InvalidOperationException(
                $"Fake error getting TV season images for {tmdbid} season {seasonNumber}"
            );
        }

        if (_tvSeasonImages.TryGetValue(key, out var images))
        {
            return Task.FromResult(images);
        }

        return Task.FromResult(new ImagesWithId { Id = tmdbid });
    }

    public Task<ImagesWithId> GetTvEpisodeImagesAsync(
        int tmdbid,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default
    )
    {
        var key = (tmdbid, seasonNumber, episodeNumber);
        TvEpisodeImageRequests.Add(key);

        if (ThrowOnTvEpisodeImages)
        {
            throw new InvalidOperationException(
                $"Fake error getting TV episode images for {tmdbid} S{seasonNumber}E{episodeNumber}"
            );
        }

        if (_tvEpisodeImages.TryGetValue(key, out var images))
        {
            return Task.FromResult(images);
        }

        return Task.FromResult(new ImagesWithId { Id = tmdbid });
    }

    public Uri GetImageUri(string filePath, string size = "original")
    {
        ImageUriRequests.Add((filePath, size));

        if (_imageUris.TryGetValue(filePath, out var uri))
        {
            return uri;
        }

        // Return a default URI if not configured
        return new Uri($"https://image.tmdb.org/t/p/{size}{filePath}");
    }

    // Configuration methods for setting up test data

    public void SetMovieImages(int tmdbId, ImagesWithId images)
    {
        _movieImages[tmdbId] = images;
    }

    public void SetTvShowImages(int tmdbId, ImagesWithId images)
    {
        _tvShowImages[tmdbId] = images;
    }

    public void SetTvSeasonImages(int tmdbId, int seasonNumber, ImagesWithId images)
    {
        _tvSeasonImages[(tmdbId, seasonNumber)] = images;
    }

    public void SetTvEpisodeImages(
        int tmdbId,
        int seasonNumber,
        int episodeNumber,
        ImagesWithId images
    )
    {
        _tvEpisodeImages[(tmdbId, seasonNumber, episodeNumber)] = images;
    }

    public void SetImageUri(string filePath, Uri uri)
    {
        _imageUris[filePath] = uri;
    }

    public void Reset()
    {
        InitialiseAsyncCalled = false;
        InitialiseCallCount = 0;
        MovieImageRequests.Clear();
        TvShowImageRequests.Clear();
        TvSeasonImageRequests.Clear();
        ImageUriRequests.Clear();
        _movieImages.Clear();
        _tvShowImages.Clear();
        _tvSeasonImages.Clear();
        _imageUris.Clear();
        ThrowOnInitialise = false;
        ThrowOnMovieImages = false;
        ThrowOnTvShowImages = false;
        ThrowOnTvSeasonImages = false;
    }
}
