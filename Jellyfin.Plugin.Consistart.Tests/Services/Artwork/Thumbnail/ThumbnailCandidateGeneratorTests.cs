using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Thumbnail;

public class ThumbnailCandidateGeneratorTests
{
    #region CanHandle Tests

    [Fact]
    public void CanHandle_with_movie_and_thumb_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Thumb);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_series_and_thumb_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Thumb);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_movie_and_non_thumb_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_series_and_non_thumb_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Logo);

        Assert.False(result);
    }

    #endregion

    #region GetCandidatesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetCandidatesAsync_with_movie_and_local_logo_returns_candidates_with_movie_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var thumbSource = new ThumbnailSource("/thumb.jpg", 1280, 720, "en");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns("/consistart/render?token=test-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Single(result);
        Assert.Contains("12345:Movie:thumbnail:/thumb.jpg:thumbnail.default", result[0].Id);
        Assert.Equal("/consistart/render?token=test-token", result[0].Url);
        Assert.Equal(1280, result[0].Width);
        Assert.Equal(720, result[0].Height);
        Assert.Equal("en", result[0].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_series_and_local_logo_returns_candidates_with_tv_show_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "67890");

        var thumbSource = new ThumbnailSource("/series-thumb.jpg", 1280, 720, "fr");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/series-logo.png", 600, 300);
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns("/consistart/render?token=series-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Single(result);
        Assert.Contains("67890:TvShow:thumbnail:/series-thumb.jpg:thumbnail.default", result[0].Id);
        Assert.Equal("/consistart/render?token=series-token", result[0].Url);
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_tmdb_id()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        // No TMDb ID set

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Empty(result);
        await provider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_item_type_not_supported()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
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
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(item, ImageType.Backdrop)
        );
    }

    #endregion

    #region GetCandidatesAsync - Remote Logo Tests

    [Fact]
    public async Task GetCandidatesAsync_uses_remote_logo_when_local_logo_not_found()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");
        item.PreferredMetadataLanguage = "en";

        var thumbSource = new ThumbnailSource("/thumb.jpg", 1280, 720);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250, "en");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([remoteLogo]);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns("/consistart/render?token=remote-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Single(result);
        await remoteLogoProvider.Received(1).GetImagesAsync(item, Arg.Any<CancellationToken>());
        remoteLogoSelector
            .Received(1)
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), "en");
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_remote_logos_found()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var thumbSource = new ThumbnailSource("/thumb.jpg", 1280, 720);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Array.Empty<LogoSource>());

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Empty(result);
        renderRequest.DidNotReceive().BuildUrl(Arg.Any<ThumbnailRenderRequest>());
    }

    [Fact]
    public async Task GetCandidatesAsync_prefers_local_logo_over_remote()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var thumbSource = new ThumbnailSource("/thumb.jpg", 1280, 720);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/local-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns("/consistart/render?token=local-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Single(result);
        await remoteLogoProvider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        remoteLogoSelector
            .DidNotReceive()
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region GetCandidatesAsync - Multiple Thumbnail Tests

    [Fact]
    public async Task GetCandidatesAsync_returns_multiple_candidates_when_selector_returns_multiple_thumbnails()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "11111");

        var thumb1 = new ThumbnailSource("/thumb1.jpg", 1280, 720, "en");
        var thumb2 = new ThumbnailSource("/thumb2.jpg", 1024, 576, "fr");
        var thumb3 = new ThumbnailSource("/thumb3.jpg", 800, 450, null);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider
            .GetImagesAsync(item, Arg.Any<CancellationToken>())
            .Returns([thumb1, thumb2, thumb3]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>())
            .Returns([thumb1, thumb2, thumb3]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns("/consistart/render?token=test-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Equal(3, result.Count);
        Assert.Contains("11111:Movie:thumbnail:/thumb1.jpg:thumbnail.default", result[0].Id);
        Assert.Contains("11111:Movie:thumbnail:/thumb2.jpg:thumbnail.default", result[1].Id);
        Assert.Contains("11111:Movie:thumbnail:/thumb3.jpg:thumbnail.default", result[2].Id);
        Assert.Equal(1280, result[0].Width);
        Assert.Equal(1024, result[1].Width);
        Assert.Equal(800, result[2].Width);
        Assert.Equal(720, result[0].Height);
        Assert.Equal(576, result[1].Height);
        Assert.Equal(450, result[2].Height);
        Assert.Equal("en", result[0].Language);
        Assert.Equal("fr", result[1].Language);
        Assert.Null(result[2].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_builds_url_for_each_thumbnail()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "22222");

        var thumb1 = new ThumbnailSource("/thumb1.jpg", 1280, 720);
        var thumb2 = new ThumbnailSource("/thumb2.jpg", 1024, 576);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumb1, thumb2]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumb1, thumb2]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<ThumbnailRenderRequest>())
            .Returns(c =>
            {
                var request = (ThumbnailRenderRequest)c[0];
                return $"/consistart/render?token=thumb-{request.ThumbnailFilePath}";
            });

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Equal(2, result.Count);
        Assert.Equal("/consistart/render?token=thumb-/thumb1.jpg", result[0].Url);
        Assert.Equal("/consistart/render?token=thumb-/thumb2.jpg", result[1].Url);
        renderRequest.Received(2).BuildUrl(Arg.Any<ThumbnailRenderRequest>());
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_thumbnails_selected()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "33333");

        var thumbSource = new ThumbnailSource("/thumb.jpg", 1280, 720);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>())
            .Returns(Array.Empty<ThumbnailSource>());

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.Empty(result);
        renderRequest.DidNotReceive().BuildUrl(Arg.Any<ThumbnailRenderRequest>());
    }

    #endregion

    #region GetCandidatesAsync - Render Request Creation Tests

    [Fact]
    public async Task GetCandidatesAsync_creates_render_request_with_correct_parameters()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "55555");

        var thumbSource = new ThumbnailSource("/test-thumb.jpg", 1920, 1080, "de");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/test-logo.png", 600, 300);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<ThumbnailRenderRequest>()).Returns("/url");

        await generator.GetCandidatesAsync(item, ImageType.Thumb);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<ThumbnailRenderRequest>(r =>
                    r.MediaKind == MediaKind.Movie
                    && r.TmdbId == 55555
                    && r.ThumbnailFilePath == "/test-thumb.jpg"
                    && r.LogoSource == localLogo
                    && r.Preset == "thumbnail.default"
                )
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_creates_render_request_with_tv_show_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "66666");

        var thumbSource = new ThumbnailSource("/series-thumb.jpg", 1280, 720);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/series-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumbSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumbSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<ThumbnailRenderRequest>()).Returns("/url");

        await generator.GetCandidatesAsync(item, ImageType.Thumb);

        renderRequest
            .Received(1)
            .BuildUrl(Arg.Is<ThumbnailRenderRequest>(r => r.MediaKind == MediaKind.TvShow));
    }

    #endregion

    #region GetCandidatesAsync - Candidate ID Tests

    [Fact]
    public async Task GetCandidatesAsync_generates_unique_id_for_each_candidate()
    {
        var provider = Substitute.For<IArtworkImageProvider<ThumbnailSource>>();
        var selector = Substitute.For<IArtworkImageSelector<ThumbnailSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<ThumbnailRenderRequest>>();
        var generator = new ThumbnailCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "77777");

        var thumb1 = new ThumbnailSource("/thumb1.jpg", 1280, 720);
        var thumb2 = new ThumbnailSource("/thumb2.jpg", 1024, 576);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([thumb1, thumb2]);
        selector.SelectImages(Arg.Any<IReadOnlyList<ThumbnailSource>>()).Returns([thumb1, thumb2]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<ThumbnailRenderRequest>()).Returns("/url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Thumb);

        Assert.NotEqual(result[0].Id, result[1].Id);
        Assert.Contains("/thumb1.jpg", result[0].Id);
        Assert.Contains("/thumb2.jpg", result[1].Id);
    }

    #endregion
}
