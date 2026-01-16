using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.SeasonPoster;

public class SeasonPosterRenderServiceTests
{
    private readonly ITMDbImagesClient _tMDbImagesClient;
    private readonly ISeasonPosterRenderer _renderer;
    private readonly SeasonPosterRenderService _service;

    public SeasonPosterRenderServiceTests()
    {
        _tMDbImagesClient = Substitute.For<ITMDbImagesClient>();
        _renderer = Substitute.For<ISeasonPosterRenderer>();
        _service = new SeasonPosterRenderService(_tMDbImagesClient, _renderer);
    }

    [Fact]
    public async Task RenderAsync_with_valid_request_returns_rendered_image()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 1234,
            SeasonNumber: 2,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3, 4 };
        var renderedBytes = new byte[] { 10, 20, 30, 40 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        var result = await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }

    [Fact]
    public async Task RenderAsync_with_valid_request_and_preset_returns_rendered_image()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 5678,
            SeasonNumber: 5,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: "custom"
        );
        var posterBytes = new byte[] { 5, 6, 7, 8 };
        var renderedBytes = new byte[] { 50, 60, 70, 80 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        var result = await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_returns_empty_array_returns_null()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 8888,
            SeasonNumber: 3,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(Array.Empty<byte>());

        // Act
        var result = await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_when_renderer_returns_null_returns_null()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 7777,
            SeasonNumber: 4,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_calls_tmdb_client_with_correct_parameters()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 1111,
            SeasonNumber: 1,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var renderedBytes = new byte[] { 10, 20, 30 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        await _tMDbImagesClient
            .Received(1)
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RenderAsync_calls_renderer_with_correct_parameters()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 2222,
            SeasonNumber: 2,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var renderedBytes = new byte[] { 10, 20, 30 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        await _renderer
            .Received(1)
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenderAsync_passes_cancellation_token_to_tmdb_client()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 3333,
            SeasonNumber: 3,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var renderedBytes = new byte[] { 10, 20, 30 };
        var cancellationToken = new CancellationToken();

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        await _service.RenderAsync(request, cancellationToken);

        // Assert
        await _tMDbImagesClient
            .Received(1)
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                cancellationToken
            );
    }

    [Fact]
    public async Task RenderAsync_passes_cancellation_token_to_renderer()
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 4444,
            SeasonNumber: 4,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var renderedBytes = new byte[] { 10, 20, 30 };
        var cancellationToken = new CancellationToken();

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, request.SeasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        await _service.RenderAsync(request, cancellationToken);

        // Assert
        await _renderer
            .Received(1)
            .RenderAsync(posterBytes, request.SeasonNumber, cancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task RenderAsync_with_different_season_numbers_renders_correctly(int seasonNumber)
    {
        // Arrange
        var request = new SeasonPosterRenderRequest(
            TmdbId: 5555,
            SeasonNumber: seasonNumber,
            SeasonPosterFilePath: "/season/poster.jpg",
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var renderedBytes = new byte[] { 10, 20, 30 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.SeasonPosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _renderer
            .RenderAsync(posterBytes, seasonNumber, Arg.Any<CancellationToken>())
            .Returns((byte[]?)renderedBytes);

        // Act
        var result = await _service.RenderAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
    }
}
