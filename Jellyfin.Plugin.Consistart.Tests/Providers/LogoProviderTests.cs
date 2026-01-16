using Jellyfin.Plugin.Consistart.Providers;
using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Providers;

public class LogoProviderTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LogoProvider> _logger;
    private readonly IArtworkCandidateService _candidateService;
    private readonly LogoProvider _provider;

    public LogoProviderTests()
    {
        _configuration = Substitute.For<IConfigurationProvider>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = NullLogger<LogoProvider>.Instance;
        _candidateService = Substitute.For<IArtworkCandidateService>();

        _provider = new LogoProvider(
            _configuration,
            _httpClientFactory,
            _logger,
            _candidateService
        );
    }

    [Fact]
    public void GetSupportedImages_returns_logo_image_type()
    {
        var item = Substitute.For<BaseItem>();

        var result = _provider.GetSupportedImages(item);

        Assert.Single(result);
        Assert.Contains(ImageType.Logo, result);
    }

    [Theory]
    [InlineData(typeof(Movie))]
    [InlineData(typeof(Series))]
    public void Supports_with_movie_or_series_returns_true(Type itemType)
    {
        var item = (BaseItem)Activator.CreateInstance(itemType)!;

        var result = _provider.Supports(item);

        Assert.True(result);
    }

    [Fact]
    public void Supports_with_other_item_returns_false()
    {
        var item = Substitute.For<BaseItem>();

        var result = _provider.Supports(item);

        Assert.False(result);
    }

    [Fact]
    public async Task GetImages_calls_candidate_service_with_logo_image_type()
    {
        var item = new Movie();
        var candidates = new[]
        {
            new ArtworkCandidateDto("id1", "http://example.com/logo1.png", 200, 100, "en"),
            new ArtworkCandidateDto("id2", "http://example.com/logo2.png", 200, 100, "fr"),
        };
        _candidateService
            .GetCandidatesAsync(item, ImageType.Logo, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)candidates));

        var result = await _provider.GetImages(item, CancellationToken.None);

        Assert.Equal(2, result.Count());
        await _candidateService
            .Received(1)
            .GetCandidatesAsync(item, ImageType.Logo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImages_returns_candidates_as_remote_image_info()
    {
        var item = new Series();
        var candidate = new ArtworkCandidateDto(
            "id1",
            "http://example.com/logo.png",
            200,
            100,
            "en"
        );
        _candidateService
            .GetCandidatesAsync(item, ImageType.Logo, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)new[] { candidate }));
        _configuration.PluginName.Returns("Consistart");

        var result = await _provider.GetImages(item, CancellationToken.None);

        var imageInfo = result.Single();
        Assert.Equal("http://example.com/logo.png", imageInfo.Url);
        Assert.Equal(ImageType.Logo, imageInfo.Type);
        Assert.Equal("Consistart", imageInfo.ProviderName);
    }
}
