using Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.EpisodeThumbnail;

public class EpisodeThumbnailSourceProviderTests
{
    private readonly ILibraryManager _libraryManager;
    private readonly ITMDbImagesClient _tmdbImagesClient;
    private readonly EpisodeThumbnailSourceProvider _provider;

    public EpisodeThumbnailSourceProviderTests()
    {
        _libraryManager = Substitute.For<ILibraryManager>();
        _tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        _provider = new EpisodeThumbnailSourceProvider(_libraryManager, _tmdbImagesClient);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_item_is_not_episode()
    {
        var item = new Series();

        var result = await _provider.GetImagesAsync(item, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient.DidNotReceiveWithAnyArgs().GetImagesAsync(default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_series_not_found()
    {
        var episodeId = Guid.NewGuid();
        var episode = new Episode
        {
            SeriesId = episodeId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
        };
        _libraryManager.GetItemById(episodeId).Returns((Series?)null);

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient.DidNotReceiveWithAnyArgs().GetImagesAsync(default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_series_tmdb_id_missing()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
        };
        _libraryManager.GetItemById(seriesId).Returns(series);

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient.DidNotReceiveWithAnyArgs().GetImagesAsync(default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_season_number_missing()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "123");
        var episode = new Episode { SeriesId = seriesId, IndexNumber = 1 };
        _libraryManager.GetItemById(seriesId).Returns(series);

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient.DidNotReceiveWithAnyArgs().GetImagesAsync(default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_episode_number_missing()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "123");
        var episode = new Episode { SeriesId = seriesId, ParentIndexNumber = 1 };
        _libraryManager.GetItemById(seriesId).Returns(series);

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient.DidNotReceiveWithAnyArgs().GetImagesAsync(default, default);
    }

    [Fact]
    public async Task GetImagesAsync_calls_tmdb_for_episode()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "42");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 2,
            IndexNumber = 5,
        };
        _libraryManager.GetItemById(seriesId).Returns(series);

        _tmdbImagesClient
            .GetImagesAsync(42, MediaKind.TvEpisode, 2, 5, Arg.Any<CancellationToken>())
            .Returns(
                new ImagesWithId
                {
                    Backdrops =
                    [
                        new ImageData
                        {
                            FilePath = "/still.jpg",
                            Width = 1920,
                            Height = 1080,
                            Iso_639_1 = null,
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(42, MediaKind.TvEpisode, 2, 5, Arg.Any<CancellationToken>());
        var still = Assert.Single(result);
        Assert.Equal("/still.jpg", still.FilePath);
        Assert.Equal(1920, still.Width);
        Assert.Equal(1080, still.Height);
        Assert.Null(still.Language);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_no_stills()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "99");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
        };
        _libraryManager.GetItemById(seriesId).Returns(series);

        _tmdbImagesClient
            .GetImagesAsync(99, MediaKind.TvEpisode, 1, 1, Arg.Any<CancellationToken>())
            .Returns(new ImagesWithId { Backdrops = [] });

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_filters_out_entries_without_file_path()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "88");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 3,
            IndexNumber = 7,
        };
        _libraryManager.GetItemById(seriesId).Returns(series);

        _tmdbImagesClient
            .GetImagesAsync(88, MediaKind.TvEpisode, 3, 7, Arg.Any<CancellationToken>())
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
                            FilePath = "/valid-still.jpg",
                            Width = 1280,
                            Height = 720,
                            Iso_639_1 = "en",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        var still = Assert.Single(result);
        Assert.Equal("/valid-still.jpg", still.FilePath);
        Assert.Equal(1280, still.Width);
        Assert.Equal(720, still.Height);
        Assert.Equal("en", still.Language);
    }

    [Fact]
    public async Task GetImagesAsync_maps_all_valid_stills()
    {
        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "11");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 2,
        };
        _libraryManager.GetItemById(seriesId).Returns(series);

        _tmdbImagesClient
            .GetImagesAsync(11, MediaKind.TvEpisode, 1, 2, Arg.Any<CancellationToken>())
            .Returns(
                new ImagesWithId
                {
                    Backdrops =
                    [
                        new ImageData
                        {
                            FilePath = "/still1.jpg",
                            Width = 1920,
                            Height = 1080,
                            Iso_639_1 = null,
                        },
                        new ImageData
                        {
                            FilePath = "/still2.jpg",
                            Width = 1280,
                            Height = 720,
                            Iso_639_1 = "en",
                        },
                        new ImageData
                        {
                            FilePath = "/still3.jpg",
                            Width = 1600,
                            Height = 900,
                            Iso_639_1 = "fr",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(episode, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.FilePath == "/still1.jpg");
        Assert.Contains(result, s => s.FilePath == "/still2.jpg");
        Assert.Contains(result, s => s.FilePath == "/still3.jpg");
    }
}
