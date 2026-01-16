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

public class ThumbnailProviderTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ThumbnailProvider> _logger;
    private readonly IArtworkCandidateService _candidateService;
    private readonly ThumbnailProvider _provider;

    public ThumbnailProviderTests()
    {
        _configuration = Substitute.For<IConfigurationProvider>();
        _configuration.BaseUrl.Returns("http://localhost:8096");
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = NullLogger<ThumbnailProvider>.Instance;
        _candidateService = Substitute.For<IArtworkCandidateService>();

        _provider = new ThumbnailProvider(
            _configuration,
            _httpClientFactory,
            _logger,
            _candidateService
        );
    }

    [Fact]
    public void GetSupportedImages_returns_thumb_image_type()
    {
        var item = Substitute.For<BaseItem>();

        var result = _provider.GetSupportedImages(item);

        Assert.Single(result);
        Assert.Contains(ImageType.Thumb, result);
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
    public async Task GetImages_calls_candidate_service_with_thumb_image_type()
    {
        var item = new Movie();
        var candidates = new[]
        {
            new ArtworkCandidateDto("id1", "/render/thumb1", 300, 225, "en"),
            new ArtworkCandidateDto("id2", "/render/thumb2", 300, 225, "fr"),
        };
        _candidateService
            .GetCandidatesAsync(item, ImageType.Thumb, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)candidates));

        var result = await _provider.GetImages(item, CancellationToken.None);

        Assert.Equal(2, result.Count());
        await _candidateService
            .Received(1)
            .GetCandidatesAsync(item, ImageType.Thumb, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImages_prepends_candidate_url_with_base_url()
    {
        var item = new Series();
        var candidate = new ArtworkCandidateDto("id1", "/render/thumb.png", 300, 225, "en");
        _candidateService
            .GetCandidatesAsync(item, ImageType.Thumb, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)new[] { candidate }));
        _configuration.PluginName.Returns("Consistart");

        var result = await _provider.GetImages(item, CancellationToken.None);

        var imageInfo = result.Single();
        Assert.Equal("http://localhost:8096/render/thumb.png", imageInfo.Url);
        Assert.Equal(ImageType.Thumb, imageInfo.Type);
        Assert.Equal("Consistart", imageInfo.ProviderName);
    }
}
