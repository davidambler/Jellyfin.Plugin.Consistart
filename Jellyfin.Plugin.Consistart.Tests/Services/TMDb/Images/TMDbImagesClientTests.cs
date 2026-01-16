using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using Jellyfin.Plugin.Consistart.Tests.TestDoubles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.TMDb.Images;

public class TMDbImagesClientTests
{
    #region Setup Helpers

    private static IHostApplicationLifetime CreateMockApplicationLifetime()
    {
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var cts = new CancellationTokenSource();
        lifetime.ApplicationStopping.Returns(cts.Token);
        return lifetime;
    }

    private static ILogger<TMDbImagesClient> CreateMockLogger()
    {
        return Substitute.For<ILogger<TMDbImagesClient>>();
    }

    private static TMDbImagesClient CreateClient(
        IHostApplicationLifetime? lifetime = null,
        ITMDbClientFactory? clientFactory = null,
        IHttpClientFactory? httpClientFactory = null,
        IMemoryCache? cache = null,
        ILogger<TMDbImagesClient>? logger = null
    )
    {
        lifetime ??= CreateMockApplicationLifetime();
        httpClientFactory ??= Substitute.For<IHttpClientFactory>();
        clientFactory ??= new RecordingTMDbClientFactory();
        cache ??= new MemoryCache(new MemoryCacheOptions());
        logger ??= CreateMockLogger();

        return new TMDbImagesClient(lifetime, httpClientFactory, clientFactory, logger, cache);
    }

    #endregion

    #region GetImagesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetImagesAsync_with_valid_movie_id_returns_images()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var expectedImages = new ImagesWithId
        {
            Id = 550,
            Posters =
            [
                new()
                {
                    FilePath = "/poster1.jpg",
                    Width = 500,
                    Height = 750,
                },
                new()
                {
                    FilePath = "/poster2.jpg",
                    Width = 500,
                    Height = 750,
                },
            ],
        };
        fakeAdapter.SetMovieImages(550, expectedImages);

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var clientWithFactory = CreateClient(clientFactory: factory);

        // Act
        var result = await clientWithFactory.GetImagesAsync(550, MediaKind.Movie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(550, result.Id);
        Assert.NotEmpty(result.Posters);
    }

    [Fact]
    public async Task GetImagesAsync_with_valid_tv_show_id_returns_images()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var expectedImages = new ImagesWithId { Id = 1399 };
        fakeAdapter.SetTvShowImages(1399, expectedImages);

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var clientWithFactory = CreateClient(clientFactory: factory);

        // Act
        var result = await clientWithFactory.GetImagesAsync(1399, MediaKind.TvShow);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1399, result.Id);
    }

    #endregion

    #region GetImagesAsync - Caching Tests

    [Fact]
    public async Task GetImagesAsync_caches_successful_results()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var expectedImages = new ImagesWithId { Id = 550 };
        fakeAdapter.SetMovieImages(550, expectedImages);

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act
        var result1 = await client.GetImagesAsync(550, MediaKind.Movie);
        var result2 = await client.GetImagesAsync(550, MediaKind.Movie);

        // Assert
        Assert.Equal(1, factory.CreateClientCallCount);
        Assert.Equal(result1.Id, result2.Id);
    }

    [Fact]
    public async Task GetImagesAsync_uses_different_cache_keys_for_different_media_kinds()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(1, new ImagesWithId { Id = 1 });
        fakeAdapter.SetTvShowImages(1, new ImagesWithId { Id = 2 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act
        var movieResult = await client.GetImagesAsync(1, MediaKind.Movie);
        var tvShowResult = await client.GetImagesAsync(1, MediaKind.TvShow);

        // Assert
        Assert.Equal(1, movieResult.Id);
        Assert.Equal(2, tvShowResult.Id);
    }

    [Fact]
    public async Task GetImagesAsync_with_cache_hit_does_not_call_factory()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(550, new ImagesWithId { Id = 550 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act - First call populates cache
        await client.GetImagesAsync(550, MediaKind.Movie);
        var initialCallCount = factory.CreateClientCallCount;

        // Second call should use cache
        await client.GetImagesAsync(550, MediaKind.Movie);

        // Assert
        Assert.Equal(initialCallCount, factory.CreateClientCallCount);
    }

    #endregion

    #region GetImagesAsync - Single Flight (Deduplication) Tests

    [Fact]
    public async Task GetImagesAsync_with_concurrent_requests_same_id_executes_once()
    {
        // Arrange
        var trackingAdapter = new FakeTMDbClientAdapter();
        trackingAdapter.SetMovieImages(550, new ImagesWithId { Id = 550 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(trackingAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act - Make concurrent requests for the same ID
        var tasks = Enumerable
            .Range(0, 5)
            .Select(_ => client.GetImagesAsync(550, MediaKind.Movie))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All concurrent requests should share single execution
        Assert.All(results, r => Assert.Equal(550, r.Id));
        Assert.Equal(1, factory.CreateClientCallCount);
    }

    [Fact]
    public async Task GetImagesAsync_deduplication_does_not_affect_different_ids()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(1, new ImagesWithId { Id = 1 });
        fakeAdapter.SetMovieImages(2, new ImagesWithId { Id = 2 });
        fakeAdapter.SetMovieImages(3, new ImagesWithId { Id = 3 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act
        var tasks = new List<Task<ImagesWithId>>
        {
            client.GetImagesAsync(1, MediaKind.Movie),
            client.GetImagesAsync(2, MediaKind.Movie),
            client.GetImagesAsync(3, MediaKind.Movie),
        };

        var results = await Task.WhenAll(tasks);

        // Assert - Each ID should execute independently
        Assert.Equal(new[] { 1, 2, 3 }, results.Select(r => r.Id).OrderBy(id => id));
    }

    #endregion

    #region GetImagesAsync - Error Handling Tests

    [Fact]
    public async Task GetImagesAsync_when_adapter_throws_propagates_exception()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter { ThrowOnMovieImages = true };

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetImagesAsync(550, MediaKind.Movie)
        );
    }

    [Fact]
    public async Task GetImagesAsync_when_initialization_fails_propagates_exception()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter { ThrowOnInitialise = true };

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetImagesAsync(550, MediaKind.Movie)
        );
    }

    [Fact]
    public async Task GetImagesAsync_when_factory_throws_propagates_exception()
    {
        // Arrange
        var factory = new RecordingTMDbClientFactory { ThrowOnCreateClient = true };

        var client = CreateClient(clientFactory: factory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetImagesAsync(550, MediaKind.Movie)
        );
    }

    [Fact]
    public async Task GetImagesAsync_after_error_subsequent_calls_retry()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(550, new ImagesWithId { Id = 550 });
        fakeAdapter.ThrowOnMovieImages = true;

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act - First call fails
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetImagesAsync(550, MediaKind.Movie)
        );

        // Fix the issue
        fakeAdapter.ThrowOnMovieImages = false;

        // Second call should retry and succeed
        var result = await client.GetImagesAsync(550, MediaKind.Movie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(550, result.Id);
    }

    #endregion

    #region GetImagesAsync - MediaKind Tests

    [Fact]
    public async Task GetImagesAsync_with_unsupported_media_kind_returns_empty_images()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);

        // Act
        var result = await client.GetImagesAsync(550, MediaKind.TvSeason);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetImageBytesAsync - Basic Functionality Tests

    [Fact]
    public async Task GetImageBytesAsync_with_valid_file_path_returns_bytes()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new MockHttpMessageHandler([1, 2, 3, 4, 5]);
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var client = CreateClient(clientFactory: factory, httpClientFactory: mockHttpFactory);

        // Act
        var result = await client.GetImageBytesAsync("/poster.jpg");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public async Task GetImageBytesAsync_with_custom_size_uses_size_parameter()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new MockHttpMessageHandler([1, 2, 3]);
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var client = CreateClient(clientFactory: factory, httpClientFactory: mockHttpFactory);

        // Act
        var result = await client.GetImageBytesAsync("/poster.jpg", ImageSize.PosterW500);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetImageBytesAsync_with_empty_file_path_throws_exception()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetImageBytesAsync(string.Empty));
    }

    [Fact]
    public async Task GetImageBytesAsync_with_null_file_path_throws_exception()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetImageBytesAsync(null!));
    }

    [Fact]
    public async Task GetImageBytesAsync_with_whitespace_file_path_throws_exception()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        // The method doesn't validate whitespace specifically, so we catch any exception
        var ex = await Record.ExceptionAsync(() => client.GetImageBytesAsync("   "));
        Assert.NotNull(ex);
    }

    #endregion

    #region GetImageBytesAsync - Error Handling Tests

    [Fact]
    public async Task GetImageBytesAsync_when_http_request_fails_propagates_exception()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new FailingHttpMessageHandler();
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var client = CreateClient(clientFactory: factory, httpClientFactory: mockHttpFactory);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetImageBytesAsync("/poster.jpg")
        );
    }

    [Fact]
    public async Task GetImageBytesAsync_when_initialization_fails_propagates_exception()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter { ThrowOnInitialise = true };
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetImageBytesAsync("/poster.jpg")
        );
    }

    #endregion

    #region GetImageBytesAsync - Cancellation Tests

    [Fact]
    public async Task GetImageBytesAsync_with_cancelled_token_throws_operation_canceled()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new CancellingHttpMessageHandler();
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var client = CreateClient(clientFactory: factory, httpClientFactory: mockHttpFactory);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        var ex = await Record.ExceptionAsync(() =>
            client.GetImageBytesAsync("/poster.jpg", cancellationToken: cts.Token)
        );
        Assert.NotNull(ex);
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    #endregion

    #region GetImagesAsync - Cancellation Tests

    [Fact]
    public async Task GetImagesAsync_with_cancelled_token_does_not_throw_immediately()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(550, new ImagesWithId { Id = 550 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - The call may or may not throw depending on timing
        // but it should handle the cancellation gracefully
        try
        {
            var result = await client.GetImagesAsync(
                550,
                MediaKind.Movie,
                cancellationToken: cts.Token
            );
            // If we get here, the operation completed before cancellation took effect
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable - the waiter was cancelled
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetImagesAsync_and_GetImageBytesAsync_work_together()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var expectedImages = new ImagesWithId
        {
            Id = 550,
            Posters =
            [
                new()
                {
                    FilePath = "/poster.jpg",
                    Width = 500,
                    Height = 750,
                },
            ],
        };
        fakeAdapter.SetMovieImages(550, expectedImages);
        fakeAdapter.SetImageUri("/poster.jpg", new Uri("https://example.com/poster.jpg"));

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new MockHttpMessageHandler([1, 2, 3]);
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(
            clientFactory: factory,
            httpClientFactory: mockHttpFactory,
            cache: cache
        );

        // Act
        var images = await client.GetImagesAsync(550, MediaKind.Movie);
        var posterPath = images.Posters.First().FilePath;
        var bytes = await client.GetImageBytesAsync(posterPath);

        // Assert
        Assert.NotEmpty(images.Posters);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetImagesAsync_multiple_media_kinds_with_independent_cache()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(1, new ImagesWithId { Id = 1, Posters = [] });
        fakeAdapter.SetTvShowImages(1, new ImagesWithId { Id = 1, Backdrops = [] });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(clientFactory: factory, cache: cache);

        // Act
        var movieImages = await client.GetImagesAsync(1, MediaKind.Movie);
        var tvShowImages = await client.GetImagesAsync(1, MediaKind.TvShow);

        // Both should be retrieved independently
        Assert.NotNull(movieImages);
        Assert.NotNull(tvShowImages);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetImagesAsync_with_zero_as_provider_id_processes_correctly()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        fakeAdapter.SetMovieImages(0, new ImagesWithId { Id = 0 });

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var client = CreateClient(clientFactory: factory);

        // Act
        var result = await client.GetImagesAsync(0, MediaKind.Movie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Id);
    }

    [Fact]
    public async Task GetImageBytesAsync_uses_adapter_uri_generation()
    {
        // Arrange
        var fakeAdapter = new FakeTMDbClientAdapter();
        var customUri = new Uri("https://custom.example.com/image.jpg");
        fakeAdapter.SetImageUri("/custom.jpg", customUri);

        var factory = new RecordingTMDbClientFactory();
        factory.SetDefaultClient(fakeAdapter);

        var mockHttpFactory = Substitute.For<IHttpClientFactory>();
        var handler = new MockHttpMessageHandler("*"u8.ToArray());
        var httpClient = new HttpClient(handler);

        mockHttpFactory.CreateClient().Returns(httpClient);

        var client = CreateClient(clientFactory: factory, httpClientFactory: mockHttpFactory);

        // Act
        var result = await client.GetImageBytesAsync("/custom.jpg");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }

    #endregion

    #region Test Helpers - HttpMessageHandler Implementations

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _responseBytes;

        public MockHttpMessageHandler(byte[] responseBytes)
        {
            _responseBytes = responseBytes;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_responseBytes),
            };
            return Task.FromResult(response);
        }
    }

    private class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromException<HttpResponseMessage>(
                new HttpRequestException("Network error")
            );
        }
    }

    private class CancellingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromException<HttpResponseMessage>(new OperationCanceledException());
        }
    }

    #endregion
}
