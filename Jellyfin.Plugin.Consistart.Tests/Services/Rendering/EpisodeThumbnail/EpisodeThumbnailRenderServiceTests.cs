using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.EpisodeThumbnail;

public class EpisodeThumbnailRenderServiceTests
{
    private readonly ITMDbImagesClient _tMDbImagesClient;
    private readonly IEpisodeThumbnailRenderer _renderer;
    private readonly EpisodeThumbnailRenderService _service;

    public EpisodeThumbnailRenderServiceTests()
    {
        _tMDbImagesClient = Substitute.For<ITMDbImagesClient>();
        _renderer = Substitute.For<IEpisodeThumbnailRenderer>();
        _service = new EpisodeThumbnailRenderService(_tMDbImagesClient, _renderer);
    }

    [Fact]
    public async Task RenderAsync_with_valid_request_returns_rendered_image()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 123,
            ThumbnailFilePath: "/episode-thumbnail.jpg",
            EpisodeNumber: 5,
            EpisodeName: "The One Where Ross Moves In",
            Preset: null
        );
        var thumbnailBytes = new byte[] { 1, 2, 3, 4 };
        var renderedBytes = new byte[] { 10, 20, 30, 40 };
        _tMDbImagesClient
            .GetImageBytesAsync(
                request.ThumbnailFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(
                thumbnailBytes,
                request.EpisodeNumber,
                request.EpisodeName,
                Arg.Any<CancellationToken>()
            )
            .Returns(renderedBytes);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }

    [Fact]
    public async Task RenderAsync_calls_tmdb_client_with_correct_parameters()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 456,
            ThumbnailFilePath: "/custom-path.jpg",
            EpisodeNumber: 12,
            EpisodeName: "Final Episode",
            Preset: "preset-x"
        );
        var thumbnailBytes = new byte[] { 5, 6, 7 };
        _tMDbImagesClient
            .GetImageBytesAsync(
                request.ThumbnailFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(
                Arg.Any<byte[]>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new byte[] { 99 });
        var cancellationToken = new CancellationTokenSource().Token;

        await _service.RenderAsync(request, cancellationToken);

        await _tMDbImagesClient
            .Received(1)
            .GetImageBytesAsync(request.ThumbnailFilePath, ImageSize.Original, cancellationToken);
    }

    [Fact]
    public async Task RenderAsync_calls_renderer_with_correct_parameters()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 789,
            ThumbnailFilePath: "/episode.jpg",
            EpisodeNumber: 3,
            EpisodeName: "Pilot",
            Preset: null
        );
        var thumbnailBytes = new byte[] { 11, 22, 33 };
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(
                Arg.Any<byte[]>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new byte[] { 44 });
        var cancellationToken = new CancellationTokenSource().Token;

        await _service.RenderAsync(request, cancellationToken);

        await _renderer
            .Received(1)
            .RenderAsync(
                thumbnailBytes,
                request.EpisodeNumber,
                request.EpisodeName,
                cancellationToken
            );
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_throws_exception_propagates_exception()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 100,
            ThumbnailFilePath: "/missing.jpg",
            EpisodeNumber: 1,
            EpisodeName: "Episode",
            Preset: null
        );
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new InvalidOperationException("TMDb error")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RenderAsync(request, CancellationToken.None)
        );

        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default, default, default);
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_returns_empty_bytes_returns_null()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 200,
            ThumbnailFilePath: "/empty.jpg",
            EpisodeNumber: 2,
            EpisodeName: "Empty",
            Preset: null
        );
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default, default, default);
    }

    [Fact]
    public async Task RenderAsync_when_renderer_returns_null_returns_null()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 300,
            ThumbnailFilePath: "/valid.jpg",
            EpisodeNumber: 8,
            EpisodeName: "Test Episode",
            Preset: "preset-y"
        );
        var thumbnailBytes = new byte[] { 1, 2, 3 };
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(
                Arg.Any<byte[]>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns((byte[]?)null);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_with_null_episode_name_passes_null_to_renderer()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 400,
            ThumbnailFilePath: "/thumbnail.jpg",
            EpisodeNumber: 7,
            EpisodeName: "Some Name",
            Preset: null
        );
        var thumbnailBytes = new byte[] { 5, 6, 7, 8 };
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(
                Arg.Any<byte[]>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new byte[] { 99, 88 });

        await _service.RenderAsync(request, CancellationToken.None);

        await _renderer
            .Received(1)
            .RenderAsync(
                thumbnailBytes,
                request.EpisodeNumber,
                request.EpisodeName,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 500,
            ThumbnailFilePath: "/path.jpg",
            EpisodeNumber: 9,
            EpisodeName: "Cancellable",
            Preset: null
        );
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _tMDbImagesClient
            .GetImageBytesAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested)
            )
            .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.RenderAsync(request, cts.Token)
        );
    }

    [Fact]
    public async Task RenderAsync_with_zero_episode_number_still_renders()
    {
        var request = new EpisodeThumbnailRenderRequest(
            TmdbId: 600,
            ThumbnailFilePath: "/zero.jpg",
            EpisodeNumber: 0,
            EpisodeName: "Special",
            Preset: null
        );
        var thumbnailBytes = new byte[] { 1, 2 };
        var renderedBytes = new byte[] { 3, 4 };
        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(thumbnailBytes);
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), 0, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(renderedBytes);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        await _renderer
            .Received(1)
            .RenderAsync(thumbnailBytes, 0, request.EpisodeName, Arg.Any<CancellationToken>());
    }
}
