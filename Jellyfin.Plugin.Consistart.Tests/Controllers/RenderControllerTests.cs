using System.Text.Json;
using Jellyfin.Plugin.Consistart.Controllers;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Controllers;

public class RenderControllerTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private static RenderController CreateController(
        out ITokenProtectionService tokenProtection,
        out IRenderService<PosterRenderRequest> posterRenderer,
        out IRenderService<SeasonPosterRenderRequest> seasonPosterRenderer,
        out IRenderService<ThumbnailRenderRequest> thumbnailRenderer,
        out IRenderService<EpisodeThumbnailRenderRequest> episodeThumbnailRenderer
    )
    {
        tokenProtection = Substitute.For<ITokenProtectionService>();
        posterRenderer = Substitute.For<IRenderService<PosterRenderRequest>>();
        seasonPosterRenderer = Substitute.For<IRenderService<SeasonPosterRenderRequest>>();
        thumbnailRenderer = Substitute.For<IRenderService<ThumbnailRenderRequest>>();
        episodeThumbnailRenderer = Substitute.For<IRenderService<EpisodeThumbnailRenderRequest>>();

        return new RenderController(
            NullLogger<RenderController>.Instance,
            tokenProtection,
            posterRenderer,
            seasonPosterRenderer,
            thumbnailRenderer,
            episodeThumbnailRenderer
        );
    }

    private static byte[] SerializeRequest(IRenderRequest request) =>
        JsonSerializer.SerializeToUtf8Bytes(request, _jsonOptions);

    [Fact]
    public async Task Get_with_missing_token_returns_bad_request()
    {
        var controller = CreateController(out var tokenProtection, out _, out _, out _, out _);

        var result = await controller.Get("   ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing token.", badRequest.Value);
        tokenProtection.DidNotReceiveWithAnyArgs().Unprotect(default!);
    }

    [Fact]
    public async Task Get_with_invalid_token_returns_bad_request()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out var seasonPosterRenderer,
            out var thumbnailRenderer,
            out var episodeThumbnailRenderer
        );
        tokenProtection
            .Unprotect("invalid-token")
            .Returns(_ => throw new InvalidOperationException("bad token"));

        var result = await controller.Get("invalid-token", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid token.", badRequest.Value);
        await posterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await seasonPosterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await thumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await episodeThumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Fact]
    public async Task Get_with_poster_request_calls_poster_renderer_and_returns_file()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out var seasonPosterRenderer,
            out var thumbnailRenderer,
            out var episodeThumbnailRenderer
        );
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/poster.jpg",
            new LogoSource(LogoSourceKind.Local, "/logo.png"),
            "preset-a"
        );
        var imageBytes = new byte[] { 1, 2, 3 };
        var rendered = new RenderedImage(imageBytes, "image/jpeg");
        tokenProtection.Unprotect("poster-token").Returns(SerializeRequest(request));
        posterRenderer
            .RenderAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RenderedImage?>(rendered));

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await controller.Get("poster-token", cancellationToken);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(imageBytes, fileResult.FileContents);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        await posterRenderer.Received(1).RenderAsync(request, cancellationToken);
        await seasonPosterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await thumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await episodeThumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Fact]
    public async Task Get_with_season_request_calls_season_renderer_and_returns_file()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out var seasonPosterRenderer,
            out var thumbnailRenderer,
            out var episodeThumbnailRenderer
        );
        var request = new SeasonPosterRenderRequest(77, 2, "/season.png", null);
        var imageBytes = new byte[] { 9, 8, 7 };
        var rendered = new RenderedImage(imageBytes, "image/png");
        tokenProtection.Unprotect("season-token").Returns(SerializeRequest(request));
        seasonPosterRenderer
            .RenderAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RenderedImage?>(rendered));

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await controller.Get("season-token", cancellationToken);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(imageBytes, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
        await seasonPosterRenderer.Received(1).RenderAsync(request, cancellationToken);
        await posterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await thumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await episodeThumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Fact]
    public async Task Get_with_thumbnail_request_calls_thumbnail_renderer_and_returns_file()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out var seasonPosterRenderer,
            out var thumbnailRenderer,
            out var episodeThumbnailRenderer
        );
        var request = new ThumbnailRenderRequest(
            MediaKind.TvShow,
            99,
            "/thumb.png",
            new LogoSource(LogoSourceKind.TMDb, "/logo2.png"),
            null
        );
        var imageBytes = new byte[] { 5, 6, 7 };
        var rendered = new RenderedImage(imageBytes, "image/png");
        tokenProtection.Unprotect("thumb-token").Returns(SerializeRequest(request));
        thumbnailRenderer
            .RenderAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RenderedImage?>(rendered));

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await controller.Get("thumb-token", cancellationToken);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(imageBytes, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
        await thumbnailRenderer.Received(1).RenderAsync(request, cancellationToken);
        await posterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await seasonPosterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await episodeThumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Fact]
    public async Task Get_with_episode_thumbnail_request_calls_episode_thumbnail_renderer_and_returns_file()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out var seasonPosterRenderer,
            out var thumbnailRenderer,
            out var episodeThumbnailRenderer
        );
        var request = new EpisodeThumbnailRenderRequest(
            55,
            "/episode-thumb.png",
            3,
            "The Galactic Adventure",
            "preset-b"
        );
        var imageBytes = new byte[] { 4, 3, 2, 1 };
        var rendered = new RenderedImage(imageBytes, "image/png");
        tokenProtection.Unprotect("episode-thumb-token").Returns(SerializeRequest(request));
        episodeThumbnailRenderer
            .RenderAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RenderedImage?>(rendered));

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await controller.Get("episode-thumb-token", cancellationToken);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(imageBytes, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
        await episodeThumbnailRenderer.Received(1).RenderAsync(request, cancellationToken);
        await posterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await seasonPosterRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        await thumbnailRenderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Fact]
    public async Task Get_with_null_render_result_returns_not_found()
    {
        var controller = CreateController(
            out var tokenProtection,
            out var posterRenderer,
            out _,
            out _,
            out _
        );
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            12,
            "/poster-missing.jpg",
            new LogoSource(LogoSourceKind.Local, "/logo.png"),
            null
        );
        tokenProtection.Unprotect("poster-token").Returns(SerializeRequest(request));
        posterRenderer
            .RenderAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RenderedImage?>(null));

        var result = await controller.Get("poster-token", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_with_invalid_type_discriminator_returns_bad_request()
    {
        var controller = CreateController(out var tokenProtection, out _, out _, out _, out _);
        // Create a JSON payload with an unknown type discriminator that isn't registered.
        // This tests the exception handling path in RenderController.
        var unsupportedJson = """{"$type":"unsupported","someProperty":"someValue"}""";
        var unsupportedBytes = System.Text.Encoding.UTF8.GetBytes(unsupportedJson);
        tokenProtection.Unprotect("unsupported-token").Returns(unsupportedBytes);

        var result = await controller.Get("unsupported-token", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid token.", badRequest.Value);
    }
}
