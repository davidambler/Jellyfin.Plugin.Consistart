using Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Thumbnail;

public class ThumbnailSourceProviderTests
{
    private readonly ITMDbImagesClient _tmdbImagesClient;
    private readonly ThumbnailSourceProvider _provider;

    public ThumbnailSourceProviderTests()
    {
        _tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        _provider = new ThumbnailSourceProvider(_tmdbImagesClient);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_id_missing()
    {
        var item = new Movie();

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_throws_for_unsupported_item_type()
    {
        var item = new Season();
        item.SetProviderId(MetadataProvider.Tmdb, "123");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _provider.GetImagesAsync(item, CancellationToken.None)
        );

        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_calls_tmdb_for_movie()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "42");

        _tmdbImagesClient
            .GetImagesAsync(42, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(
                new ImagesWithId
                {
                    Backdrops =
                    [
                        new ImageData
                        {
                            FilePath = "/backdrop.jpg",
                            Width = 1920,
                            Height = 1080,
                            Iso_639_1 = "en",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(42, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>());
        var thumbnail = Assert.Single(result);
        Assert.Equal("/backdrop.jpg", thumbnail.FilePath);
        Assert.Equal(1920, thumbnail.Width);
        Assert.Equal(1080, thumbnail.Height);
        Assert.Equal("en", thumbnail.Language);
    }

    [Fact]
    public async Task GetImagesAsync_calls_tmdb_for_series()
    {
        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "7");

        _tmdbImagesClient
            .GetImagesAsync(7, MediaKind.TvShow, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Backdrops = [] });

        await _provider.GetImagesAsync(item, CancellationToken.None);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(7, MediaKind.TvShow, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImagesAsync_filters_out_entries_without_file_path()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "88");

        _tmdbImagesClient
            .GetImagesAsync(88, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(
                new ImagesWithId
                {
                    Backdrops =
                    [
                        new ImageData
                        {
                            FilePath = null,
                            Width = 1,
                            Height = 1,
                        },
                        new ImageData
                        {
                            FilePath = string.Empty,
                            Width = 2,
                            Height = 2,
                        },
                        new ImageData
                        {
                            FilePath = "   ",
                            Width = 3,
                            Height = 3,
                        },
                        new ImageData
                        {
                            FilePath = "/valid-backdrop.jpg",
                            Width = 1280,
                            Height = 720,
                            Iso_639_1 = "fr",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        var thumbnail = Assert.Single(result);
        Assert.Equal("/valid-backdrop.jpg", thumbnail.FilePath);
        Assert.Equal(1280, thumbnail.Width);
        Assert.Equal(720, thumbnail.Height);
        Assert.Equal("fr", thumbnail.Language);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_returns_no_backdrops()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "99");

        _tmdbImagesClient
            .GetImagesAsync(99, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Backdrops = [] });

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_returns_null()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "100");

        _tmdbImagesClient
            .GetImagesAsync(100, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((ImagesWithId)null!);

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_passes_cancellation_token()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "55");
        var cts = new CancellationTokenSource();

        _tmdbImagesClient
            .GetImagesAsync(55, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Backdrops = [] });

        await _provider.GetImagesAsync(item, cts.Token);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(55, MediaKind.Movie, cancellationToken: cts.Token);
    }

    [Fact]
    public async Task GetImagesAsync_returns_multiple_backdrops()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "200");

        _tmdbImagesClient
            .GetImagesAsync(200, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(
                new ImagesWithId
                {
                    Backdrops =
                    [
                        new ImageData
                        {
                            FilePath = "/backdrop1.jpg",
                            Width = 1920,
                            Height = 1080,
                            Iso_639_1 = "en",
                        },
                        new ImageData
                        {
                            FilePath = "/backdrop2.jpg",
                            Width = 3840,
                            Height = 2160,
                            Iso_639_1 = "de",
                        },
                        new ImageData
                        {
                            FilePath = "/backdrop3.jpg",
                            Width = 1280,
                            Height = 720,
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("/backdrop1.jpg", result[0].FilePath);
        Assert.Equal(1920, result[0].Width);
        Assert.Equal(1080, result[0].Height);
        Assert.Equal("en", result[0].Language);

        Assert.Equal("/backdrop2.jpg", result[1].FilePath);
        Assert.Equal(3840, result[1].Width);
        Assert.Equal(2160, result[1].Height);
        Assert.Equal("de", result[1].Language);

        Assert.Equal("/backdrop3.jpg", result[2].FilePath);
        Assert.Equal(1280, result[2].Width);
        Assert.Equal(720, result[2].Height);
        Assert.Null(result[2].Language);
    }
}
