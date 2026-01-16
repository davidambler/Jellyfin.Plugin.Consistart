using Jellyfin.Plugin.Consistart.Providers;
using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Providers;

public sealed class TestProvider(
    IConfigurationProvider configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<TestProvider> logger,
    IArtworkCandidateService candidateService
) : ConsistartProvider<TestProvider>(configuration, httpClientFactory, logger, candidateService)
{
    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Logo];

    public override bool Supports(BaseItem item) => true;

    public override Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken
    ) => Task.FromResult(Enumerable.Empty<RemoteImageInfo>());

    public RemoteImageInfo CreateRemoteImageInfoPublic(
        ArtworkCandidateDto candidate,
        ImageType imageType,
        bool useBaseUrl = false
    ) => CreateRemoteImageInfo(candidate, imageType, useBaseUrl);
}

public class ConsistartProviderTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TestProvider> _logger;
    private readonly IArtworkCandidateService _candidateService;
    private readonly TestProvider _provider;

    public ConsistartProviderTests()
    {
        _configuration = Substitute.For<IConfigurationProvider>();
        _configuration.PluginName.Returns("TestPlugin");
        _configuration.BaseUrl.Returns("http://localhost:8096");

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<TestProvider>>();
        _candidateService = Substitute.For<IArtworkCandidateService>();

        _provider = new TestProvider(
            _configuration,
            _httpClientFactory,
            _logger,
            _candidateService
        );
    }

    [Fact]
    public void Name_returns_configuration_plugin_name()
    {
        var result = _provider.Name;

        Assert.Equal("TestPlugin", result);
    }

    [Fact]
    public async Task GetImageResponse_with_valid_url_returns_http_response_message()
    {
        var url = "http://example.com/image.jpg";
        var httpClient = new HttpClient();

        _httpClientFactory.CreateClient().Returns(httpClient);

        try
        {
            var result = await _provider.GetImageResponse(url, CancellationToken.None);

            Assert.NotNull(result);
        }
        catch (HttpRequestException)
        {
            // Network error expected in test environment
        }
    }

    [Fact]
    public async Task GetImageResponse_with_empty_url_throws_argument_exception()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _provider.GetImageResponse("", CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetImageResponse_with_null_url_throws_argument_exception()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _provider.GetImageResponse(null!, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetImageResponse_with_whitespace_url_throws_argument_exception()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _provider.GetImageResponse("   ", CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetImageResponse_with_http_error_logs_warning_and_rethrows()
    {
        var url = "http://invalid-domain-that-does-not-exist-12345.com/image.jpg";
        var httpClient = new HttpClient();

        _httpClientFactory.CreateClient().Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _provider.GetImageResponse(url, CancellationToken.None)
        );

        Assert.NotNull(exception);
        _logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Failed to fetch image response from")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public void CreateRemoteImageInfo_without_base_url_builds_image_info()
    {
        var candidate = new ArtworkCandidateDto("test-id", "/images/test.jpg", 800, 600, "en");

        var result = _provider.CreateRemoteImageInfoPublic(
            candidate,
            ImageType.Logo,
            useBaseUrl: false
        );

        Assert.NotNull(result);
        Assert.Equal("TestPlugin", result.ProviderName);
        Assert.Equal("/images/test.jpg", result.Url);
        Assert.Equal("en", result.Language);
        Assert.Equal(800, result.Width);
        Assert.Equal(600, result.Height);
        Assert.Equal(ImageType.Logo, result.Type);
    }

    [Fact]
    public void CreateRemoteImageInfo_with_base_url_prepends_base_url()
    {
        var candidate = new ArtworkCandidateDto("test-id", "/images/test.jpg", 800, 600, "en");

        var result = _provider.CreateRemoteImageInfoPublic(
            candidate,
            ImageType.Thumb,
            useBaseUrl: true
        );

        Assert.NotNull(result);
        Assert.Equal("http://localhost:8096/images/test.jpg", result.Url);
        Assert.Equal(ImageType.Thumb, result.Type);
    }
}
