using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Artwork.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Poster;

public class PosterCandidateGeneratorTests
{
    #region CanHandle Tests

    [Fact]
    public void CanHandle_with_movie_and_primary_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_series_and_primary_image_type_returns_true()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_movie_and_non_primary_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Logo);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_series_and_non_primary_image_type_returns_false()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Backdrop);

        Assert.False(result);
    }

    #endregion

    #region GetCandidatesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetCandidatesAsync_with_movie_and_local_logo_returns_candidates_with_movie_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500, "en");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<PosterRenderRequest>())
            .Returns("/consistart/render?token=test-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Contains("12345:Movie:poster:/poster.jpg:poster.default", result[0].Id);
        Assert.Equal("/consistart/render?token=test-token", result[0].Url);
        Assert.Equal(1000, result[0].Width);
        Assert.Equal(1500, result[0].Height);
        Assert.Equal("en", result[0].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_series_and_local_logo_returns_candidates_with_tv_show_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "67890");

        var posterSource = new PosterSource("/series-poster.jpg", 800, 1200, "fr");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/series-logo.png", 600, 300);
        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<PosterRenderRequest>())
            .Returns("/consistart/render?token=series-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Contains("67890:TvShow:poster:/series-poster.jpg:poster.default", result[0].Id);
        Assert.Equal("/consistart/render?token=series-token", result[0].Url);
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_tmdb_id()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        // No TMDb ID set

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
        await provider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_item_type_not_supported()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
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
            generator.GetCandidatesAsync(item, ImageType.Logo)
        );
    }

    [Fact]
    public async Task GetCandidatesAsync_throws_when_image_type_not_supported()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
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
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
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

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250, "en");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([remoteLogo]);
        renderRequest
            .BuildUrl(Arg.Any<PosterRenderRequest>())
            .Returns("/consistart/render?token=remote-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        await remoteLogoProvider.Received(1).GetImagesAsync(item, Arg.Any<CancellationToken>());
        remoteLogoSelector
            .Received(1)
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), "en");
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_no_remote_logos_found()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Array.Empty<LogoSource>());

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
        renderRequest.DidNotReceive().BuildUrl(Arg.Any<PosterRenderRequest>());
    }

    [Fact]
    public async Task GetCandidatesAsync_prefers_local_logo_over_remote()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/local-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<PosterRenderRequest>())
            .Returns("/consistart/render?token=local-token");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        await remoteLogoProvider
            .DidNotReceive()
            .GetImagesAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        remoteLogoSelector
            .DidNotReceive()
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region GetCandidatesAsync - Multiple Poster Tests

    [Fact]
    public async Task GetCandidatesAsync_returns_multiple_candidates_when_selector_returns_multiple_posters()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "11111");

        var poster1 = new PosterSource("/poster1.jpg", 1000, 1500, "en");
        var poster2 = new PosterSource("/poster2.jpg", 1200, 1800, "fr");
        var poster3 = new PosterSource("/poster3.jpg", 800, 1200, null);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider
            .GetImagesAsync(item, Arg.Any<CancellationToken>())
            .Returns([poster1, poster2, poster3]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<PosterSource>>())
            .Returns([poster1, poster2, poster3]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest
            .BuildUrl(Arg.Any<PosterRenderRequest>())
            .Returns(x =>
                $"/consistart/render?token={((PosterRenderRequest)x[0]).PosterFilePath}-token"
            );

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Id.Contains("/poster1.jpg"));
        Assert.Contains(result, c => c.Id.Contains("/poster2.jpg"));
        Assert.Contains(result, c => c.Id.Contains("/poster3.jpg"));
        Assert.Contains(result, c => c.Url == "/consistart/render?token=/poster1.jpg-token");
        Assert.Contains(result, c => c.Url == "/consistart/render?token=/poster2.jpg-token");
        Assert.Contains(result, c => c.Url == "/consistart/render?token=/poster3.jpg-token");
    }

    [Fact]
    public async Task GetCandidatesAsync_returns_empty_list_when_selector_returns_empty()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector
            .SelectImages(Arg.Any<IReadOnlyList<PosterSource>>())
            .Returns(Array.Empty<PosterSource>());
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
        renderRequest.DidNotReceive().BuildUrl(Arg.Any<PosterRenderRequest>());
    }

    #endregion

    #region GetCandidatesAsync - Render Request Tests

    [Fact]
    public async Task GetCandidatesAsync_builds_render_request_with_correct_parameters()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "98765");

        var posterSource = new PosterSource("/test-poster.jpg", 1000, 1500, "de");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/test-logo.png", 600, 300);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        await generator.GetCandidatesAsync(item, ImageType.Primary);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<PosterRenderRequest>(r =>
                    r.MediaKind == MediaKind.Movie
                    && r.TmdbId == 98765
                    && r.PosterFilePath == "/test-poster.jpg"
                    && r.LogoSource == localLogo
                    && r.Preset == "poster.default"
                )
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_builds_render_request_for_series_with_tv_show_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Series();
        item.SetProviderId(MetadataProvider.Tmdb, "55555");

        var posterSource = new PosterSource("/series.jpg", 800, 1200);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/series-logo.png", 400, 200);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        await generator.GetCandidatesAsync(item, ImageType.Primary);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<PosterRenderRequest>(r =>
                    r.MediaKind == MediaKind.TvShow && r.TmdbId == 55555
                )
            );
    }

    #endregion

    #region GetCandidatesAsync - Candidate ID Tests

    [Fact]
    public async Task GetCandidatesAsync_generates_unique_candidate_ids_for_different_posters()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "99999");

        var poster1 = new PosterSource("/poster-a.jpg", 1000, 1500);
        var poster2 = new PosterSource("/poster-b.jpg", 1200, 1800);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([poster1, poster2]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([poster1, poster2]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Equal(2, result.Count);
        Assert.Equal("99999:Movie:poster:/poster-a.jpg:poster.default", result[0].Id);
        Assert.Equal("99999:Movie:poster:/poster-b.jpg:poster.default", result[1].Id);
        Assert.NotEqual(result[0].Id, result[1].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_candidate_id_includes_media_kind()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var movie = new Movie();
        movie.SetProviderId(MetadataProvider.Tmdb, "12345");

        var series = new Series();
        series.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider
            .GetImagesAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>())
            .Returns([posterSource]);
        provider
            .GetImagesAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>())
            .Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>())
            .Returns(localLogo);
        localLogoProvider
            .TryGetLocalLogoAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var movieResult = await generator.GetCandidatesAsync(movie, ImageType.Primary);
        var seriesResult = await generator.GetCandidatesAsync(series, ImageType.Primary);

        Assert.Contains("Movie", movieResult[0].Id);
        Assert.Contains("TvShow", seriesResult[0].Id);
        Assert.NotEqual(movieResult[0].Id, seriesResult[0].Id);
    }

    #endregion

    #region GetCandidatesAsync - Language Handling Tests

    [Fact]
    public async Task GetCandidatesAsync_passes_language_to_remote_logo_selector()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");
        item.PreferredMetadataLanguage = "de-DE";

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250, "de");

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([remoteLogo]);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        await generator.GetCandidatesAsync(item, ImageType.Primary);

        remoteLogoSelector
            .Received(1)
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), "de");
    }

    [Fact]
    public async Task GetCandidatesAsync_preserves_poster_language_in_candidate()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500, "ja");
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("ja", result[0].Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_handles_null_language_in_poster()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500, null);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Null(result[0].Language);
    }

    #endregion

    #region GetCandidatesAsync - Dimension Tests

    [Fact]
    public async Task GetCandidatesAsync_preserves_poster_dimensions_in_candidate()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 2000, 3000);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal(2000, result[0].Width);
        Assert.Equal(3000, result[0].Height);
    }

    [Fact]
    public async Task GetCandidatesAsync_handles_zero_dimensions()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");

        var posterSource = new PosterSource("/poster.jpg", 0, 0);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        var result = await generator.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal(0, result[0].Width);
        Assert.Equal(0, result[0].Height);
    }

    #endregion

    #region GetCandidatesAsync - Cancellation Tests

    [Fact]
    public async Task GetCandidatesAsync_propagates_cancellation_token_to_provider()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");
        var cts = new CancellationTokenSource();

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var localLogo = new LogoSource(LogoSourceKind.Local, "/logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns(localLogo);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        await generator.GetCandidatesAsync(item, ImageType.Primary, cts.Token);

        await provider.Received(1).GetImagesAsync(item, cts.Token);
        await localLogoProvider.Received(1).TryGetLocalLogoAsync(item, cts.Token);
    }

    [Fact]
    public async Task GetCandidatesAsync_propagates_cancellation_token_to_remote_logo_provider()
    {
        var provider = Substitute.For<IArtworkImageProvider<PosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<PosterSource>>();
        var localLogoProvider = Substitute.For<ILocalLogoProvider>();
        var remoteLogoProvider = Substitute.For<IArtworkImageProvider<LogoSource>>();
        var remoteLogoSelector = Substitute.For<IArtworkImageSelector<LogoSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<PosterRenderRequest>>();
        var generator = new PosterCandidateGenerator(
            provider,
            selector,
            localLogoProvider,
            remoteLogoProvider,
            remoteLogoSelector,
            renderRequest
        );

        var item = new Movie();
        item.SetProviderId(MetadataProvider.Tmdb, "12345");
        var cts = new CancellationTokenSource();

        var posterSource = new PosterSource("/poster.jpg", 1000, 1500);
        var remoteLogo = new LogoSource(LogoSourceKind.TMDb, "/remote-logo.png", 500, 250);

        provider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([posterSource]);
        selector.SelectImages(Arg.Any<IReadOnlyList<PosterSource>>()).Returns([posterSource]);
        localLogoProvider
            .TryGetLocalLogoAsync(item, Arg.Any<CancellationToken>())
            .Returns((LogoSource?)null);
        remoteLogoProvider.GetImagesAsync(item, Arg.Any<CancellationToken>()).Returns([remoteLogo]);
        remoteLogoSelector
            .SelectImages(Arg.Any<IReadOnlyList<LogoSource>>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns([remoteLogo]);
        renderRequest.BuildUrl(Arg.Any<PosterRenderRequest>()).Returns("/render-url");

        await generator.GetCandidatesAsync(item, ImageType.Primary, cts.Token);

        await remoteLogoProvider.Received(1).GetImagesAsync(item, cts.Token);
    }

    #endregion
}
