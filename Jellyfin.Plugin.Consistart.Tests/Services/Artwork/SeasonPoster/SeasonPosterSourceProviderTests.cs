using Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.SeasonPoster;

public class SeasonPosterSourceProviderTests
{
    private readonly ILibraryManager _library;
    private readonly ITMDbImagesClient _tmdbImagesClient;
    private readonly SeasonPosterSourceProvider _provider;

    public SeasonPosterSourceProviderTests()
    {
        _library = Substitute.For<ILibraryManager>();
        _tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        _provider = new SeasonPosterSourceProvider(_library, _tmdbImagesClient);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_parent_missing()
    {
        var season = new Season { ParentId = Guid.NewGuid(), IndexNumber = 1 };

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_parent_has_no_tmdb_id()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        var season = new Season { ParentId = parentId, IndexNumber = 1 };
        _library.GetItemById(parentId).Returns(parent);

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_throws_for_non_season_item()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "42");
        var item = new Episode { ParentId = parentId };
        _library.GetItemById(parentId).Returns(parent);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _provider.GetImagesAsync(item, CancellationToken.None)
        );

        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_index_number_missing()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "7");
        var season = new Season { ParentId = parentId };
        _library.GetItemById(parentId).Returns(parent);

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        Assert.Empty(result);
        await _tmdbImagesClient
            .DidNotReceiveWithAnyArgs()
            .GetImagesAsync(default, default, default);
    }

    [Fact]
    public async Task GetImagesAsync_calls_tmdb_with_tmdb_id_and_season_number()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "99");
        var season = new Season { ParentId = parentId, IndexNumber = 3 };
        _library.GetItemById(parentId).Returns(parent);

        _tmdbImagesClient
            .GetImagesAsync(
                99,
                MediaKind.TvSeason,
                3,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(
                new ImagesWithId
                {
                    Posters =
                    [
                        new ImageData
                        {
                            FilePath = "/poster.jpg",
                            Width = 500,
                            Height = 750,
                            Iso_639_1 = "en",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(
                99,
                MediaKind.TvSeason,
                3,
                cancellationToken: Arg.Any<CancellationToken>()
            );
        var poster = Assert.Single(result);
        Assert.Equal("/poster.jpg", poster.FilePath);
        Assert.Equal(500, poster.Width);
        Assert.Equal(750, poster.Height);
        Assert.Equal("en", poster.Language);
    }

    [Fact]
    public async Task GetImagesAsync_filters_out_entries_without_file_path()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "123");
        var season = new Season { ParentId = parentId, IndexNumber = 2 };
        _library.GetItemById(parentId).Returns(parent);

        _tmdbImagesClient
            .GetImagesAsync(
                123,
                MediaKind.TvSeason,
                2,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(
                new ImagesWithId
                {
                    Posters =
                    [
                        new ImageData
                        {
                            FilePath = null!,
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
                            FilePath = "/ok.jpg",
                            Width = 4,
                            Height = 5,
                            Iso_639_1 = "fr",
                        },
                    ],
                }
            );

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        var poster = Assert.Single(result);
        Assert.Equal("/ok.jpg", poster.FilePath);
        Assert.Equal(4, poster.Width);
        Assert.Equal(5, poster.Height);
        Assert.Equal("fr", poster.Language);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_returns_no_posters()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "88");
        var season = new Season { ParentId = parentId, IndexNumber = 4 };
        _library.GetItemById(parentId).Returns(parent);

        _tmdbImagesClient
            .GetImagesAsync(
                88,
                MediaKind.TvSeason,
                4,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(new ImagesWithId { Posters = [] });

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_returns_empty_when_tmdb_returns_null()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "66");
        var season = new Season { ParentId = parentId, IndexNumber = 5 };
        _library.GetItemById(parentId).Returns(parent);

        _tmdbImagesClient
            .GetImagesAsync(
                66,
                MediaKind.TvSeason,
                5,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((ImagesWithId)null!);

        var result = await _provider.GetImagesAsync(season, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_passes_cancellation_token()
    {
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "11");
        var season = new Season { ParentId = parentId, IndexNumber = 6 };
        _library.GetItemById(parentId).Returns(parent);
        var cts = new CancellationTokenSource();

        _tmdbImagesClient
            .GetImagesAsync(
                11,
                MediaKind.TvSeason,
                6,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(new ImagesWithId { Posters = [] });

        await _provider.GetImagesAsync(season, cts.Token);

        await _tmdbImagesClient
            .Received(1)
            .GetImagesAsync(11, MediaKind.TvSeason, 6, cancellationToken: cts.Token);
    }
}
