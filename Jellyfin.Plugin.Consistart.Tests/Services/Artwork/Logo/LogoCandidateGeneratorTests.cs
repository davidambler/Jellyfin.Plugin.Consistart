using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Logo;

public class LogoCandidateGeneratorTests
{
    #region CanHandle Tests

    [Fact]
    public void CanHandle_with_movie_and_logo_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Logo);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_series_and_logo_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Logo);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_movie_and_non_logo_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_series_and_non_logo_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Backdrop);

        Assert.False(result);
    }

    #endregion

    #region GetCandidatesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetCandidatesAsync_with_movie_returns_candidates_with_movie_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/logo.png",
            Width: 500,
            Height: 250,
            Language: "en"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Contains("12345:Movie:logo:/logo.png", result[0].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_series_returns_candidates_with_tv_show_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "67890");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/series-logo.png",
            Width: 600,
            Height: 300,
            Language: "en"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/series-logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/series-logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Contains("67890:TvShow:logo:/series-logo.png", result[0].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_tmdb_id()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        // No TMDb ID set

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_item_type_not_supported()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(item, ImageType.Primary)
        );
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_image_type_not_supported()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(item, ImageType.Backdrop)
        );
    }

    #endregion

    #region GetCandidatesAsync - Multiple Logo Tests

    [Fact]
    public async Task GetCandidatesAsync_returns_multiple_candidates_when_selector_returns_multiple()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "11111");

        var logo1 = new LogoSource(LogoSourceKind.TMDb, "/logo1.png", 500, 250, "en");
        var logo2 = new LogoSource(LogoSourceKind.TMDb, "/logo2.png", 600, 300, "fr");
        var logo3 = new LogoSource(LogoSourceKind.TMDb, "/logo3.png", 700, 350, null);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logo1, logo2, logo3]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logo1, logo2, logo3]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo1.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo1.png"));
        client
            .GetImageUri("/logo2.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo2.png"));
        client
            .GetImageUri("/logo3.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo3.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Url == "https://image.tmdb.org/t/p/original/logo1.png");
        Assert.Contains(result, c => c.Url == "https://image.tmdb.org/t/p/original/logo2.png");
        Assert.Contains(result, c => c.Url == "https://image.tmdb.org/t/p/original/logo3.png");
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_selector_returns_empty()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png", 500, 250, "en");
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([]);
        clientFactory.CreateClient().Returns(client);

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Empty(result);
    }

    #endregion

    #region GetCandidatesAsync - URL and ID Generation Tests

    [Fact]
    public async Task GetCandidatesAsync_builds_correct_id_with_tmdb_id_and_file_path()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "99999");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/custom/path/logo.png",
            500,
            250,
            "en"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/custom/path/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/custom/path/logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Equal("99999:Movie:logo:/custom/path/logo.png", result[0].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_uses_tmdb_client_to_build_image_uri()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/test-logo.png", 500, 250, "en");
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        var expectedUri = new Uri("https://image.tmdb.org/t/p/original/test-logo.png");
        client.GetImageUri("/test-logo.png", Arg.Any<string>()).Returns(expectedUri);

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Equal(expectedUri.ToString(), result[0].Url);
        client.Received(1).GetImageUri("/test-logo.png", Arg.Any<string>());
    }

    #endregion

    #region GetCandidatesAsync - Property Mapping Tests

    [Fact]
    public async Task GetCandidatesAsync_maps_width_height_and_language_correctly()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/logo.png",
            Width: 1920,
            Height: 1080,
            Language: "fr"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Equal(1920, result[0].Width);
        Assert.Equal(1080, result[0].Height);
        Assert.Equal("fr", result[0].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_handles_null_language_in_logo_source()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/logo.png",
            Width: 500,
            Height: 250,
            Language: null
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Null(result[0].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_handles_zero_dimensions()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/logo.png",
            Width: 0,
            Height: 0,
            Language: "en"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Equal(0, result[0].Width);
        Assert.Equal(0, result[0].Height);
    }

    #endregion

    #region GetCandidatesAsync - Dependency Interaction Tests

    [Fact]
    public async Task GetCandidatesAsync_calls_provider_with_correct_item()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        await provider.Received(1).GetImagesAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_selector_with_provider_results_and_language()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logo1 = new LogoSource(LogoSourceKind.TMDb, "/logo1.png", 500, 250, "de");
        var logo2 = new LogoSource(LogoSourceKind.TMDb, "/logo2.png", 600, 300, "en");
        var logos = new List<LogoSource> { logo1, logo2 };

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns(logos);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        selector
            .Received(1)
            .SelectImages(
                Arg.Is<IReadOnlyList<LogoSource>>(list =>
                    list.Count == 2 && list.Contains(logo1) && list.Contains(logo2)
                ),
                Arg.Any<int>(),
                Arg.Any<string?>()
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_initializes_tmdb_client()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png", 500, 250, "en");
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        await client.Received(1).InitialiseAsync();
    }

    [Fact]
    public async Task GetCandidatesAsync_creates_client_from_factory()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png", 500, 250, "en");
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/logo.png", Arg.Any<string>())
            .Returns(new Uri("https://image.tmdb.org/t/p/original/logo.png"));

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        clientFactory.Received(1).CreateClient();
    }

    #endregion

    #region GetCandidatesAsync - Language Handling Tests

    [Fact]
    public async Task GetCandidatesAsync_extracts_language_subtag_from_preferred_metadata_language()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        selector
            .Received(1)
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task GetCandidatesAsync_with_simple_language_code_passes_to_selector()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(item, ImageType.Logo);

        selector
            .Received(1)
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region GetCandidatesAsync - Edge Cases

    [Fact]
    public async Task GetCandidatesAsync_with_invalid_tmdb_id_string_returns_empty()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "not-a-number");

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_respects_cancellation_token()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        provider
            .GetImagesAsync(item, Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<LogoSource>>(_ =>
                throw new OperationCanceledException(cts.Token)
            );

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            generator.GetCandidatesAsync(item, ImageType.Logo, cts.Token)
        );
    }

    [Fact]
    public async Task GetCandidatesAsync_with_special_characters_in_file_path_handles_correctly()
    {
        var provider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var selector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var generator = new LogoCandidateGenerator(provider, selector, clientFactory);

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var logoSource = new LogoSource(
            LogoSourceKind.TMDb,
            "/path/with spaces & symbols/logo.png",
            500,
            250,
            "en"
        );
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([logoSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([logoSource]);
        clientFactory.CreateClient().Returns(client);
        client
            .GetImageUri("/path/with spaces & symbols/logo.png", Arg.Any<string>())
            .Returns(
                new Uri(
                    "https://image.tmdb.org/t/p/original/path/with%20spaces%20&%20symbols/logo.png"
                )
            );

        var result = await generator.GetCandidatesAsync(item, ImageType.Logo);

        Assert.Single(result);
        Assert.Equal("12345:Movie:logo:/path/with spaces & symbols/logo.png", result[0].Id);
    }

    #endregion
}
