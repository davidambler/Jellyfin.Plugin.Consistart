using Jellyfin.Plugin.Consistart.Services.Artwork.Poster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Poster;

public class PosterSourceProviderTests
{
    private readonly ITMDbImagesClient _tmdbImagesClient;
    private readonly PosterSourceProvider _provider;

    public PosterSourceProviderTests()
    {
        _tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        _provider = new PosterSourceProvider(_tmdbImagesClient);
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
                    Posters =
                    [
                        new ImageData
                        {
                            FilePath = "/path.jpg",
                            Width = 100,
                            Height = 200,
                            Iso_639_1 = "en",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(42, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>());
        var poster = Assert.Single(result);
        Assert.Equal("/path.jpg", poster.FilePath);
        Assert.Equal(100, poster.Width);
        Assert.Equal(200, poster.Height);
        Assert.Equal("en", poster.Language);
    }

    [Fact]
    public async Task GetImagesAsync_calls_tmdb_for_series()
    {
        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "7");

        _tmdbImagesClient
            .GetImagesAsync(7, MediaKind.TvShow, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Posters = [] });

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
                    Posters =
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
                            FilePath = "/valid.jpg",
                            Width = 4,
                            Height = 5,
                            Iso_639_1 = "fr",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        var poster = Assert.Single(result);
        Assert.Equal("/valid.jpg", poster.FilePath);
        Assert.Equal(4, poster.Width);
        Assert.Equal(5, poster.Height);
        Assert.Equal("fr", poster.Language);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_returns_no_posters()
    {
        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "99");

        _tmdbImagesClient
            .GetImagesAsync(99, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Posters = [] });

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
            .Returns(new ImagesWithId { Posters = [] });

        await _provider.GetImagesAsync(item, cts.Token);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(55, MediaKind.Movie, cancellationToken: cts.Token);
    }
}
