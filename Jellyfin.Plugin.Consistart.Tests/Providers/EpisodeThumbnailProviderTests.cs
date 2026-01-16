using Jellyfin.Plugin.Consistart.Providers;
using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Providers;

public class EpisodeThumbnailProviderTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IArtworkCandidateService _candidateService;
    private readonly EpisodeThumbnailProvider _provider;

    public EpisodeThumbnailProviderTests()
    {
        _configuration = Substitute.For<IConfigurationProvider>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _candidateService = Substitute.For<IArtworkCandidateService>();

        _provider = new EpisodeThumbnailProvider(
            _configuration,
            _httpClientFactory,
            NullLogger<EpisodeThumbnailProvider>.Instance,
            _candidateService
        );
    }

    [Fact]
    public void GetSupportedImages_returns_primary_image_type()
    {
        var item = Substitute.For<BaseItem>();

        var result = _provider.GetSupportedImages(item);

        Assert.Single(result);
        Assert.Contains(ImageType.Primary, result);
    }

    [Fact]
    public void Supports_with_episode_returns_true()
    {
        var item = new Episode();

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
    public async Task GetImages_calls_candidate_service_with_primary_image_type()
    {
        var item = new Episode();
        var candidates = new[]
        {
            new ArtworkCandidateDto("id1", "http://example.com/still1.jpg", 1920, 1080, null),
            new ArtworkCandidateDto("id2", "http://example.com/still2.jpg", 1920, 1080, "en"),
        };
        _candidateService
            .GetCandidatesAsync(item, ImageType.Primary, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)candidates));

        var result = await _provider.GetImages(item, CancellationToken.None);

        Assert.Equal(2, result.Count());
        await _candidateService
            .Received(1)
            .GetCandidatesAsync(item, ImageType.Primary, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImages_returns_candidates_as_remote_image_info()
    {
        var item = new Episode();
        var candidate = new ArtworkCandidateDto(
            "id1",
            "http://example.com/still.jpg",
            1920,
            1080,
            null
        );
        _candidateService
            .GetCandidatesAsync(item, ImageType.Primary, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)[candidate]));

        var result = await _provider.GetImages(item, CancellationToken.None);

        var imageInfo = result.Single();
        Assert.Equal("http://example.com/still.jpg", imageInfo.Url);
        Assert.Equal(1920, imageInfo.Width);
        Assert.Equal(1080, imageInfo.Height);
        Assert.Equal(ImageType.Primary, imageInfo.Type);
        Assert.Null(imageInfo.Language);
    }
}
