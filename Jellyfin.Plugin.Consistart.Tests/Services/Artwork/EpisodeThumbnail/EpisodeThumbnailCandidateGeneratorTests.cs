using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.EpisodeThumbnail;

public class EpisodeThumbnailCandidateGeneratorTests
{
    #region CanHandle Tests

    [Fact]
    public void CanHandle_with_episode_and_primary_image_type_returns_true()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var item = new Episode();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_episode_and_non_primary_image_type_returns_false()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var item = new Episode();
        var result = generator.CanHandle(item, ImageType.Thumb);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_non_episode_and_primary_image_type_returns_false()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_series_and_primary_image_type_returns_false()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    #endregion

    #region GetCandidatesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetCandidatesAsync_with_episode_returns_candidates()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 3,
            Name = "Episode Three",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var stillSource = new EpisodeThumbnailSource("/still.jpg", 1920, 1080, "en");
        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([stillSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>())
            .Returns([stillSource]);
        clientFactory.CreateClient().Returns(client);
        renderRequest
            .BuildUrl(Arg.Any<EpisodeThumbnailRenderRequest>())
            .Returns("/consistart/render?token=test-token");

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("12345:episode:1:3:/still.jpg:episode.default", result[0].Id);
        Assert.Equal("/consistart/render?token=test-token", result[0].Url);
        Assert.Equal(1920, result[0].Width);
        Assert.Equal(1080, result[0].Height);
        Assert.Equal("en", result[0].Language);
        await client.Received(1).InitialiseAsync();
    }

    [Fact]
    public async Task GetCandidatesAsync_with_multiple_stills_returns_multiple_candidates()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "67890");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 2,
            IndexNumber = 5,
            Name = "The Fifth Episode",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var still1 = new EpisodeThumbnailSource("/still1.jpg", 1920, 1080, "en");
        var still2 = new EpisodeThumbnailSource("/still2.jpg", 1280, 720, "fr");
        var still3 = new EpisodeThumbnailSource("/still3.jpg", 1920, 1080, null);
        provider
            .GetImagesAsync(episode, Arg.Any<CancellationToken>())
            .Returns([still1, still2, still3]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>())
            .Returns([still1, still2, still3]);
        clientFactory.CreateClient().Returns(client);
        renderRequest
            .BuildUrl(Arg.Any<EpisodeThumbnailRenderRequest>())
            .Returns(
                "/consistart/render?token=token1",
                "/consistart/render?token=token2",
                "/consistart/render?token=token3"
            );

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Equal(3, result.Count);
        Assert.Equal("67890:episode:2:5:/still1.jpg:episode.default", result[0].Id);
        Assert.Equal("67890:episode:2:5:/still2.jpg:episode.default", result[1].Id);
        Assert.Equal("67890:episode:2:5:/still3.jpg:episode.default", result[2].Id);
        Assert.Equal("/consistart/render?token=token1", result[0].Url);
        Assert.Equal("/consistart/render?token=token2", result[1].Url);
        Assert.Equal("/consistart/render?token=token3", result[2].Url);
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_tmdb_id()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        // No TMDb ID set
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Empty(result);
        await provider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Episode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_season_number()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            // ParentIndexNumber = null, // No season number
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Empty(result);
        await provider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Episode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_episode_number()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            // IndexNumber = null, // No episode number
            Name = "Episode Name",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Empty(result);
        await provider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Episode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_item_type_not_supported()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(item, ImageType.Primary)
        );
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_image_type_not_supported()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(episode, ImageType.Thumb)
        );
    }

    #endregion

    #region GetCandidatesAsync - Render Request Tests

    [Fact]
    public async Task GetCandidatesAsync_builds_correct_render_request()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "99999");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 3,
            IndexNumber = 7,
            Name = "The Seventh",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var stillSource = new EpisodeThumbnailSource("/ep-still.jpg", 1920, 1080, "de");
        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([stillSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>())
            .Returns([stillSource]);
        clientFactory.CreateClient().Returns(client);
        renderRequest
            .BuildUrl(Arg.Any<EpisodeThumbnailRenderRequest>())
            .Returns("/consistart/render?token=test");

        await generator.GetCandidatesAsync(episode, ImageType.Primary);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<EpisodeThumbnailRenderRequest>(req =>
                    req.TmdbId == 99999
                    && req.ThumbnailFilePath == "/ep-still.jpg"
                    && req.EpisodeNumber == 7
                    && req.EpisodeName == "The Seventh"
                    && req.Preset == "episode.default"
                )
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_uses_default_preset()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "11111");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Pilot",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var stillSource = new EpisodeThumbnailSource("/pilot.jpg", 1920, 1080, "en");
        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([stillSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>())
            .Returns([stillSource]);
        clientFactory.CreateClient().Returns(client);
        renderRequest
            .BuildUrl(Arg.Any<EpisodeThumbnailRenderRequest>())
            .Returns("/consistart/render?token=test");

        await generator.GetCandidatesAsync(episode, ImageType.Primary);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<EpisodeThumbnailRenderRequest>(req => req.Preset == "episode.default")
            );
    }

    #endregion

    #region GetCandidatesAsync - Provider/Selector Interaction Tests

    [Fact]
    public async Task GetCandidatesAsync_calls_provider_with_correct_item()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([]);
        selector.SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>()).Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(episode, ImageType.Primary);

        await provider.Received(1).GetImagesAsync(episode, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_selector_with_provider_results()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var still1 = new EpisodeThumbnailSource("/still1.jpg", 1920, 1080, "en");
        var still2 = new EpisodeThumbnailSource("/still2.jpg", 1280, 720, "fr");
        var stills = new List<EpisodeThumbnailSource> { still1, still2 };
        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns(stills);
        selector.SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>()).Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(episode, ImageType.Primary);

        selector
            .Received(1)
            .SelectImages(
                Arg.Is<IReadOnlyList<EpisodeThumbnailSource>>(list =>
                    list.Count == 2 && list[0] == still1 && list[1] == still2
                )
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_when_selector_returns_empty()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        var still = new EpisodeThumbnailSource("/still.jpg", 1920, 1080, "en");
        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([still]);
        selector.SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>()).Returns([]);
        clientFactory.CreateClient().Returns(client);

        var result = await generator.GetCandidatesAsync(episode, ImageType.Primary);

        Assert.Empty(result);
        await client.Received(1).InitialiseAsync();
    }

    [Fact]
    public async Task GetCandidatesAsync_initializes_tmdb_client()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<EpisodeThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<EpisodeThumbnailSource>>();
        var clientFactory = Substitute.For<ITMDbClientFactory>();
        var client = Substitute.For<ITMDbClientAdapter>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<EpisodeThumbnailRenderRequest>>();
        var generator = new EpisodeThumbnailCandidateGenerator(
            libraryManager,
            provider,
            selector,
            clientFactory,
            renderRequest
        );

        var seriesId = Guid.NewGuid();
        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");
        var episode = new Episode
        {
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1,
            Name = "Episode One",
        };
        libraryManager.GetItemById(seriesId).Returns(series);

        provider.GetImagesAsync(episode, Arg.Any<CancellationToken>()).Returns([]);
        selector.SelectImages(Arg.Any<IReadOnlyList<EpisodeThumbnailSource>>()).Returns([]);
        clientFactory.CreateClient().Returns(client);

        await generator.GetCandidatesAsync(episode, ImageType.Primary);

        clientFactory.Received(1).CreateClient();
        await client.Received(1).InitialiseAsync();
    }

    #endregion
}
