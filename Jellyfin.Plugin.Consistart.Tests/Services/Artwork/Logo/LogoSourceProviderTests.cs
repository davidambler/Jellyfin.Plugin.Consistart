using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Logo;

public class LogoSourceProviderTests
{
    #region Basic Functionality Tests

    [Fact]
    public async Task GetImagesAsync_with_movie_and_valid_tmdb_id_returns_logos()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/logo1.png",
                    Width = 500,
                    Height = 200,
                    Iso_639_1 = "en",
                },
                new ImageData
                {
                    FilePath = "/logo2.png",
                    Width = 600,
                    Height = 250,
                    Iso_639_1 = "fr",
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(LogoSourceKind.TMDb, result[0].Kind);
        Assert.Equal("/logo1.png", result[0].FilePath);
        Assert.Equal(500, result[0].Width);
        Assert.Equal(200, result[0].Height);
        Assert.Equal("en", result[0].Language);
        Assert.Equal("/logo2.png", result[1].FilePath);
    }

    [Fact]
    public async Task GetImagesAsync_with_series_and_valid_tmdb_id_returns_logos()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "54321");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/series-logo.png",
                    Width = 800,
                    Height = 300,
                    Iso_639_1 = "de",
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(
                54321,
                MediaKind.TvShow,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(images);

        var result = await provider.GetImagesAsync(series, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(LogoSourceKind.TMDb, result[0].Kind);
        Assert.Equal("/series-logo.png", result[0].FilePath);
        Assert.Equal(800, result[0].Width);
        Assert.Equal(300, result[0].Height);
        Assert.Equal("de", result[0].Language);
    }

    [Fact]
    public async Task GetImagesAsync_without_tmdb_id_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
        await tmdbImagesClient
            .DidNotReceive()
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetImagesAsync_with_invalid_tmdb_id_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "not-a-number");

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
        await tmdbImagesClient
            .DidNotReceive()
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetImagesAsync_with_null_images_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((ImagesWithId)null!);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImagesAsync_with_empty_logos_list_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId { Logos = [] };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region Media Kind Tests

    [Fact]
    public async Task GetImagesAsync_with_movie_calls_tmdb_with_movie_kind()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "999");

        var images = new ImagesWithId { Logos = [] };
        tmdbImagesClient
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(images);

        await provider.GetImagesAsync(movie, CancellationToken.None);

        await tmdbImagesClient
            .Received(1)
            .GetImagesAsync(999, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImagesAsync_with_series_calls_tmdb_with_tv_show_kind()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "888");

        var images = new ImagesWithId { Logos = [] };
        tmdbImagesClient
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(images);

        await provider.GetImagesAsync(series, CancellationToken.None);

        await tmdbImagesClient
            .Received(1)
            .GetImagesAsync(888, MediaKind.TvShow, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImagesAsync_with_unsupported_item_type_throws()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var folder = new Folder();
        folder.SetProviderId(MetadataProvider.Tmdb, "12345");

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await provider.GetImagesAsync(folder, CancellationToken.None)
        );
    }

    #endregion

    #region Logo Filtering Tests

    [Fact]
    public async Task GetImagesAsync_filters_out_logos_with_null_file_path()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/valid-logo.png",
                    Width = 500,
                    Height = 200,
                },
                new ImageData
                {
                    FilePath = null!,
                    Width = 600,
                    Height = 250,
                },
                new ImageData
                {
                    FilePath = "/another-valid.png",
                    Width = 700,
                    Height = 300,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("/valid-logo.png", result[0].FilePath);
        Assert.Equal("/another-valid.png", result[1].FilePath);
    }

    [Fact]
    public async Task GetImagesAsync_filters_out_logos_with_empty_file_path()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/valid-logo.png",
                    Width = 500,
                    Height = 200,
                },
                new ImageData
                {
                    FilePath = "",
                    Width = 600,
                    Height = 250,
                },
                new ImageData
                {
                    FilePath = "   ",
                    Width = 700,
                    Height = 300,
                },
                new ImageData
                {
                    FilePath = "/another-valid.png",
                    Width = 800,
                    Height = 400,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("/valid-logo.png", result[0].FilePath);
        Assert.Equal("/another-valid.png", result[1].FilePath);
    }

    [Fact]
    public async Task GetImagesAsync_with_all_invalid_file_paths_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = null!,
                    Width = 500,
                    Height = 200,
                },
                new ImageData
                {
                    FilePath = "",
                    Width = 600,
                    Height = 250,
                },
                new ImageData
                {
                    FilePath = "   ",
                    Width = 700,
                    Height = 300,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region Logo Properties Tests

    [Fact]
    public async Task GetImagesAsync_preserves_all_logo_properties()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/detailed-logo.png",
                    Width = 1920,
                    Height = 1080,
                    Iso_639_1 = "ja",
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(LogoSourceKind.TMDb, result[0].Kind);
        Assert.Equal("/detailed-logo.png", result[0].FilePath);
        Assert.Equal(1920, result[0].Width);
        Assert.Equal(1080, result[0].Height);
        Assert.Equal("ja", result[0].Language);
    }

    [Fact]
    public async Task GetImagesAsync_handles_logos_without_language()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/no-language-logo.png",
                    Width = 800,
                    Height = 400,
                    Iso_639_1 = null,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("/no-language-logo.png", result[0].FilePath);
        Assert.Null(result[0].Language);
    }

    [Fact]
    public async Task GetImagesAsync_handles_logos_with_zero_dimensions()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/unknown-size-logo.png",
                    Width = 0,
                    Height = 0,
                    Iso_639_1 = "en",
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(0, result[0].Width);
        Assert.Equal(0, result[0].Height);
    }

    #endregion

    #region Multiple Logos Tests

    [Fact]
    public async Task GetImagesAsync_with_multiple_logos_returns_all_valid_ones()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/logo1.png",
                    Width = 500,
                    Height = 200,
                    Iso_639_1 = "en",
                },
                new ImageData
                {
                    FilePath = "/logo2.png",
                    Width = 600,
                    Height = 250,
                    Iso_639_1 = "fr",
                },
                new ImageData
                {
                    FilePath = "/logo3.png",
                    Width = 700,
                    Height = 300,
                    Iso_639_1 = "de",
                },
                new ImageData
                {
                    FilePath = "/logo4.png",
                    Width = 800,
                    Height = 350,
                    Iso_639_1 = "es",
                },
                new ImageData
                {
                    FilePath = "/logo5.png",
                    Width = 900,
                    Height = 400,
                    Iso_639_1 = "it",
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Equal(5, result.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal($"/logo{i + 1}.png", result[i].FilePath);
        }
    }

    [Fact]
    public async Task GetImagesAsync_preserves_logo_order()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/zebra.png",
                    Width = 100,
                    Height = 50,
                },
                new ImageData
                {
                    FilePath = "/alpha.png",
                    Width = 200,
                    Height = 100,
                },
                new ImageData
                {
                    FilePath = "/mike.png",
                    Width = 300,
                    Height = 150,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("/zebra.png", result[0].FilePath);
        Assert.Equal("/alpha.png", result[1].FilePath);
        Assert.Equal("/mike.png", result[2].FilePath);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetImagesAsync_respects_cancellation_token()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        tmdbImagesClient
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns<ImagesWithId>(callInfo => throw new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await provider.GetImagesAsync(movie, cts.Token)
        );
    }

    [Fact]
    public async Task GetImagesAsync_passes_cancellation_token_to_client()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var cts = new CancellationTokenSource();
        var images = new ImagesWithId { Logos = [] };

        tmdbImagesClient
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(images);

        await provider.GetImagesAsync(movie, cts.Token);

        await tmdbImagesClient
            .Received(1)
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: cts.Token);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetImagesAsync_with_very_large_tmdb_id_works()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, int.MaxValue.ToString());

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/logo.png",
                    Width = 500,
                    Height = 200,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(
                int.MaxValue,
                MediaKind.Movie,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Single(result);
        await tmdbImagesClient
            .Received(1)
            .GetImagesAsync(
                int.MaxValue,
                MediaKind.Movie,
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetImagesAsync_with_negative_tmdb_id_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "-123");

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
        await tmdbImagesClient
            .DidNotReceive()
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetImagesAsync_with_zero_tmdb_id_returns_empty()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "0");

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Empty(result);
        await tmdbImagesClient
            .DidNotReceive()
            .GetImagesAsync(
                Arg.Any<int>(),
                Arg.Any<MediaKind>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetImagesAsync_with_special_characters_in_file_path_preserves_them()
    {
        var tmdbImagesClient = Substitute.For<ITMDbImagesClient>();
        var provider = new LogoSourceProvider(tmdbImagesClient);

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var images = new ImagesWithId
        {
            Logos =
            [
                new ImageData
                {
                    FilePath = "/path with spaces/logo (1).png",
                    Width = 500,
                    Height = 200,
                },
            ],
        };

        tmdbImagesClient
            .GetImagesAsync(12345, MediaKind.Movie, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(images);

        var result = await provider.GetImagesAsync(movie, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("/path with spaces/logo (1).png", result[0].FilePath);
    }

    #endregion
}
