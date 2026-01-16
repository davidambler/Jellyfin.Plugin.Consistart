using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.SeasonPoster;

public class SeasonPosterCandidateGeneratorTests
{
    #region CanHandle Tests

    [Fact]
    public void CanHandle_with_season_and_primary_image_type_returns_true()
    {
        var library = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var item = new Season();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_with_season_and_non_primary_image_type_returns_false()
    {
        var library = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var item = new Season();
        var result = generator.CanHandle(item, ImageType.Logo);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_non_season_item_and_primary_image_type_returns_false()
    {
        var library = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var item = new Series();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    [Fact]
    public void CanHandle_with_movie_and_primary_image_type_returns_false()
    {
        var library = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var item = new Movie();
        var result = generator.CanHandle(item, ImageType.Primary);

        Assert.False(result);
    }

    #endregion

    #region GetCandidatesAsync Tests

    [Fact]
    public async Task GetCandidatesAsync_with_unsupported_item_throws_not_supported_exception()
    {
        var library = Substitute.For<ILibraryManager>();
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var item = new Series();
        var imageType = ImageType.Logo;

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            generator.GetCandidatesAsync(item, imageType)
        );

        Assert.Contains("Series", exception.Message);
        Assert.Contains("Logo", exception.Message);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_null_parent_returns_empty_list()
    {
        var library = Substitute.For<ILibraryManager>();
        library.GetItemById(Arg.Any<Guid>()).Returns((BaseItem)null!);
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = Guid.NewGuid() };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_parent_without_tmdb_id_returns_empty_list()
    {
        var library = Substitute.For<ILibraryManager>();
        var parent = new Series();
        library.GetItemById(Arg.Any<Guid>()).Returns(parent);
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = Guid.NewGuid() };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_season_without_index_number_returns_empty_list()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "12345");
        library.GetItemById(parentId).Returns(parent);
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = null };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_no_posters_returns_empty_list()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, "12345");
        library.GetItemById(parentId).Returns(parent);
        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        provider
            .GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>())
            .Returns(new List<SeasonPosterSource>());
        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector
            .SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>())
            .Returns(new List<SeasonPosterSource>());
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_single_poster_returns_one_candidate()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var posters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster.jpg", Language: "en", Width: 500, Height: 750),
        };
        provider.GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>()).Returns(posters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector.SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>()).Returns(posters);

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        renderRequest
            .BuildUrl(Arg.Any<SeasonPosterRenderRequest>())
            .Returns("http://localhost:8096/consistart/render?token=abc123");

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Single(result);
        var candidate = result[0];
        Assert.Equal("12345:season:poster:1:/path/to/poster.jpg:season.default", candidate.Id);
        Assert.Equal("http://localhost:8096/consistart/render?token=abc123", candidate.Url);
        Assert.Equal(500, candidate.Width);
        Assert.Equal(750, candidate.Height);
        Assert.Equal("en", candidate.Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_multiple_posters_returns_multiple_candidates()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var posters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster1.jpg", Language: "en", Width: 500, Height: 750),
            new(FilePath: "/path/to/poster2.jpg", Language: "fr", Width: 500, Height: 750),
            new(FilePath: "/path/to/poster3.jpg", Language: null, Width: 500, Height: 750),
        };
        provider.GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>()).Returns(posters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector.SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>()).Returns(posters);

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        renderRequest
            .BuildUrl(Arg.Any<SeasonPosterRenderRequest>())
            .Returns(
                "http://localhost:8096/consistart/render?token=abc1",
                "http://localhost:8096/consistart/render?token=abc2",
                "http://localhost:8096/consistart/render?token=abc3"
            );

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Equal(3, result.Count);
        Assert.Equal("12345:season:poster:1:/path/to/poster1.jpg:season.default", result[0].Id);
        Assert.Equal("12345:season:poster:1:/path/to/poster2.jpg:season.default", result[1].Id);
        Assert.NotNull(result[2].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_provider_with_correct_season()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var posters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster.jpg", Language: "en", Width: 500, Height: 750),
        };
        provider.GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>()).Returns(posters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector.SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>()).Returns(posters);

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        renderRequest
            .BuildUrl(Arg.Any<SeasonPosterRenderRequest>())
            .Returns("http://localhost:8096/consistart/render?token=abc123");

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        await generator.GetCandidatesAsync(season, ImageType.Primary);

        await provider.Received(1).GetImagesAsync(season, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_selector_with_posters_from_provider()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var posters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster1.jpg", Language: "en", Width: 500, Height: 750),
            new(FilePath: "/path/to/poster2.jpg", Language: "fr", Width: 500, Height: 750),
        };
        provider.GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>()).Returns(posters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector
            .SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>())
            .Returns(new List<SeasonPosterSource>());

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        await generator.GetCandidatesAsync(season, ImageType.Primary);

        selector
            .Received(1)
            .SelectImages(Arg.Is<IReadOnlyList<SeasonPosterSource>>(p => p.Count == 2));
    }

    [Fact]
    public async Task GetCandidatesAsync_passes_correct_parameters_to_render_request_builder()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var seasonNumber = 2;
        var posterPath = "/path/to/poster.jpg";
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var posters = new List<SeasonPosterSource>
        {
            new(FilePath: posterPath, Language: "en", Width: 500, Height: 750),
        };
        provider.GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>()).Returns(posters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        selector.SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>()).Returns(posters);

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        renderRequest
            .BuildUrl(Arg.Any<SeasonPosterRenderRequest>())
            .Returns("http://localhost:8096/consistart/render?token=abc123");

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = seasonNumber };

        await generator.GetCandidatesAsync(season, ImageType.Primary);

        renderRequest
            .Received(1)
            .BuildUrl(
                Arg.Is<SeasonPosterRenderRequest>(r =>
                    r.TmdbId == tmdbId
                    && r.SeasonNumber == seasonNumber
                    && r.SeasonPosterFilePath == posterPath
                    && r.Preset == "season.default"
                )
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_with_selector_filtering_posters_returns_filtered_candidates()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var allPosters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster1.jpg", Language: "en", Width: 500, Height: 750),
            new(FilePath: "/path/to/poster2.jpg", Language: "fr", Width: 500, Height: 750),
            new(FilePath: "/path/to/poster3.jpg", Language: "de", Width: 500, Height: 750),
        };
        provider
            .GetImagesAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>())
            .Returns(allPosters);

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var selectedPosters = new List<SeasonPosterSource>
        {
            new(FilePath: "/path/to/poster1.jpg", Language: "en", Width: 500, Height: 750),
        };
        selector
            .SelectImages(Arg.Any<IReadOnlyList<SeasonPosterSource>>())
            .Returns(selectedPosters);

        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();
        renderRequest
            .BuildUrl(Arg.Any<SeasonPosterRenderRequest>())
            .Returns("http://localhost:8096/consistart/render?token=abc123");

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        var result = await generator.GetCandidatesAsync(season, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("12345:season:poster:1:/path/to/poster1.jpg:season.default", result[0].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_respects_cancellation_token()
    {
        var library = Substitute.For<ILibraryManager>();
        var parentId = Guid.NewGuid();
        var tmdbId = 12345;
        var parent = new Series();
        parent.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
        library.GetItemById(parentId).Returns(parent);

        var provider = Substitute.For<IArtworkImageProvider<SeasonPosterSource>>();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        provider
            .GetImagesAsync(Arg.Any<BaseItem>(), cts.Token)
            .Returns<Task<IReadOnlyList<SeasonPosterSource>>>(_ =>
                throw new OperationCanceledException()
            );

        var selector = Substitute.For<IArtworkImageSelector<SeasonPosterSource>>();
        var renderRequest = Substitute.For<IRenderRequestBuilder<SeasonPosterRenderRequest>>();

        var generator = new SeasonPosterCandidateGenerator(
            library,
            provider,
            selector,
            renderRequest
        );

        var season = new Season { ParentId = parentId, IndexNumber = 1 };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            generator.GetCandidatesAsync(season, ImageType.Primary, cts.Token)
        );
    }

    #endregion
}
