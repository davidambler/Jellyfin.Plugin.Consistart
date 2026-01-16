using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.Poster;

public class PosterRenderServiceTests
{
    private readonly ITMDbImagesClient _tMDbImagesClient;
    private readonly IPosterRenderer _renderer;
    private readonly ILocalFileReader _localFileReader;
    private readonly PosterRenderService _service;

    public PosterRenderServiceTests()
    {
        _tMDbImagesClient = Substitute.For<ITMDbImagesClient>();
        _renderer = Substitute.For<IPosterRenderer>();
        _localFileReader = Substitute.For<ILocalFileReader>();
        _service = new PosterRenderService(_tMDbImagesClient, _renderer, _localFileReader);
    }

    [Fact]
    public async Task RenderAsync_with_valid_request_and_tmdb_logo_returns_rendered_image()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 123,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3, 4 };
        var logoBytes = new byte[] { 5, 6, 7, 8 };
        var renderedBytes = new byte[] { 10, 20, 30, 40 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _tMDbImagesClient
            .GetImageBytesAsync(
                logoSource.FilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(logoBytes);
        _renderer
            .RenderAsync(posterBytes, logoBytes, Arg.Any<CancellationToken>())
            .Returns(renderedBytes);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }

    [Fact]
    public async Task RenderAsync_with_valid_request_and_local_logo_returns_rendered_image()
    {
        var logoSource = new LogoSource(LogoSourceKind.Local, "/local-logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 456,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: "custom"
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var logoBytes = new byte[] { 9, 10, 11 };
        var renderedBytes = new byte[] { 50, 60, 70 };

        var localFileReader = Substitute.For<ILocalFileReader>();
        var service = new PosterRenderService(_tMDbImagesClient, _renderer, localFileReader);

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        localFileReader
            .TryReadAllBytesAsync(logoSource.FilePath, Arg.Any<CancellationToken>())
            .Returns(logoBytes);
        _renderer
            .RenderAsync(posterBytes, logoBytes, Arg.Any<CancellationToken>())
            .Returns(renderedBytes);

        var result = await service.RenderAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }

    [Fact]
    public async Task RenderAsync_calls_tmdb_client_with_correct_poster_parameters()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.TvShow,
            TmdbId: 789,
            PosterFilePath: "/custom-poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var cancellationToken = new CancellationTokenSource().Token;

        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 99 });

        await _service.RenderAsync(request, cancellationToken);

        await _tMDbImagesClient
            .Received(1)
            .GetImageBytesAsync(request.PosterFilePath, ImageSize.Original, cancellationToken);
    }

    [Fact]
    public async Task RenderAsync_calls_tmdb_client_with_correct_logo_parameters_for_tmdb_logo()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/tmdb-logo.png", 100, 50, "en");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 111,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var cancellationToken = new CancellationTokenSource().Token;

        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 99 });

        await _service.RenderAsync(request, cancellationToken);

        await _tMDbImagesClient
            .Received(1)
            .GetImageBytesAsync(logoSource.FilePath, ImageSize.Original, cancellationToken);
    }

    [Fact]
    public async Task RenderAsync_calls_local_file_reader_with_correct_parameters_for_local_logo()
    {
        var logoSource = new LogoSource(LogoSourceKind.Local, "/local/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 222,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var localFileReader = Substitute.For<ILocalFileReader>();
        var service = new PosterRenderService(_tMDbImagesClient, _renderer, localFileReader);
        var cancellationToken = new CancellationTokenSource().Token;

        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });
        localFileReader
            .TryReadAllBytesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 99 });

        await service.RenderAsync(request, cancellationToken);

        await localFileReader
            .Received(1)
            .TryReadAllBytesAsync(logoSource.FilePath, cancellationToken);
    }

    [Fact]
    public async Task RenderAsync_calls_renderer_with_correct_parameters()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 333,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: "preset-a"
        );
        var posterBytes = new byte[] { 11, 22, 33 };
        var logoBytes = new byte[] { 44, 55, 66 };
        var cancellationToken = new CancellationTokenSource().Token;

        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => x[0].Equals(request.PosterFilePath) ? posterBytes : logoBytes);
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 99 });

        await _service.RenderAsync(request, cancellationToken);

        await _renderer.Received(1).RenderAsync(posterBytes, logoBytes, cancellationToken);
    }

    [Fact]
    public async Task RenderAsync_when_poster_bytes_empty_returns_null()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 555,
            PosterFilePath: "/empty-poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(Array.Empty<byte>());

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_logo_bytes_empty_returns_null()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/empty-logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 777,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _tMDbImagesClient
            .GetImageBytesAsync(
                logoSource.FilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(Array.Empty<byte>());

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_local_logo_bytes_null_returns_null()
    {
        var logoSource = new LogoSource(LogoSourceKind.Local, "/missing-local-logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 888,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var localFileReader = Substitute.For<ILocalFileReader>();
        var service = new PosterRenderService(_tMDbImagesClient, _renderer, localFileReader);
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        localFileReader
            .TryReadAllBytesAsync(logoSource.FilePath, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_local_logo_bytes_empty_returns_null()
    {
        var logoSource = new LogoSource(LogoSourceKind.Local, "/empty-local-logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 999,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var localFileReader = Substitute.For<ILocalFileReader>();
        var service = new PosterRenderService(_tMDbImagesClient, _renderer, localFileReader);
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        localFileReader
            .TryReadAllBytesAsync(logoSource.FilePath, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var result = await service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_renderer_returns_null_returns_null()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1000,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: "preset-b"
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var logoBytes = new byte[] { 4, 5, 6 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _tMDbImagesClient
            .GetImageBytesAsync(
                logoSource.FilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(logoBytes);
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_poster_throws_exception_propagates_exception()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1001,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<byte[]>(new HttpRequestException("Network error")));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.RenderAsync(request, CancellationToken.None)
        );

        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_tmdb_logo_throws_exception_propagates_exception()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1002,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _tMDbImagesClient
            .GetImageBytesAsync(
                logoSource.FilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<byte[]>(new InvalidOperationException("TMDb error")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RenderAsync(request, CancellationToken.None)
        );

        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_local_file_reader_throws_exception_propagates_exception()
    {
        var logoSource = new LogoSource(LogoSourceKind.Local, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1003,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var localFileReader = Substitute.For<ILocalFileReader>();
        var service = new PosterRenderService(_tMDbImagesClient, _renderer, localFileReader);
        var posterBytes = new byte[] { 1, 2, 3 };

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        localFileReader
            .TryReadAllBytesAsync(logoSource.FilePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]?>(new FileNotFoundException("File not found")));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RenderAsync(request, CancellationToken.None)
        );

        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!, default!);
    }

    [Fact]
    public async Task RenderAsync_when_renderer_throws_exception_propagates_exception()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1004,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var posterBytes = new byte[] { 1, 2, 3 };
        var logoBytes = new byte[] { 4, 5, 6 };

        _tMDbImagesClient
            .GetImageBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => x[0].Equals(request.PosterFilePath) ? posterBytes : logoBytes);
        _renderer
            .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]?>(new InvalidOperationException("Render error")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RenderAsync(request, CancellationToken.None)
        );
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 1005,
            PosterFilePath: "/poster.jpg",
            LogoSource: logoSource,
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
    public async Task RenderAsync_with_different_media_kinds_renders_successfully()
    {
        var mediaKinds = new[] { MediaKind.Movie, MediaKind.TvShow, MediaKind.TvEpisode };

        foreach (var mediaKind in mediaKinds)
        {
            var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
            var request = new PosterRenderRequest(
                MediaKind: mediaKind,
                TmdbId: 2000,
                PosterFilePath: "/poster.jpg",
                LogoSource: logoSource,
                Preset: null
            );
            var posterBytes = new byte[] { 1, 2, 3 };
            var logoBytes = new byte[] { 4, 5, 6 };
            var renderedBytes = new byte[] { 99, 100 };

            _tMDbImagesClient
                .GetImageBytesAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(x => x[0].Equals(request.PosterFilePath) ? posterBytes : logoBytes);
            _renderer
                .RenderAsync(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                .Returns(renderedBytes);

            var result = await _service.RenderAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("image/jpeg", result.Value.MimeType);
        }
    }

    [Fact]
    public async Task RenderAsync_with_large_byte_arrays_renders_successfully()
    {
        var logoSource = new LogoSource(LogoSourceKind.TMDb, "/logo.png");
        var request = new PosterRenderRequest(
            MediaKind: MediaKind.Movie,
            TmdbId: 3000,
            PosterFilePath: "/large-poster.jpg",
            LogoSource: logoSource,
            Preset: null
        );
        var posterBytes = new byte[1024 * 1024]; // 1MB
        var logoBytes = new byte[512 * 1024]; // 512KB
        var renderedBytes = new byte[2048 * 1024]; // 2MB

        Array.Fill(posterBytes, (byte)42);
        Array.Fill(logoBytes, (byte)99);
        Array.Fill(renderedBytes, (byte)128);

        _tMDbImagesClient
            .GetImageBytesAsync(
                request.PosterFilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(posterBytes);
        _tMDbImagesClient
            .GetImageBytesAsync(
                logoSource.FilePath,
                ImageSize.Original,
                Arg.Any<CancellationToken>()
            )
            .Returns(logoBytes);
        _renderer
            .RenderAsync(posterBytes, logoBytes, Arg.Any<CancellationToken>())
            .Returns(renderedBytes);

        var result = await _service.RenderAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(renderedBytes, result.Value.Bytes);
        Assert.Equal("image/jpeg", result.Value.MimeType);
    }
}
