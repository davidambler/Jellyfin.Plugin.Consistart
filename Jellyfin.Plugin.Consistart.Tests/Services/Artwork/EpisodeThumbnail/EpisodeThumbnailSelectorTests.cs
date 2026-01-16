using Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.EpisodeThumbnail;

public class EpisodeThumbnailSelectorTests
{
    #region Helper Methods

    private static EpisodeThumbnailSelector CreateSelector() => new();

    private static EpisodeThumbnailSource CreateStill(
        string filePath = "image.jpg",
        int width = 1920,
        int height = 1080,
        string? language = null
    ) => new(filePath, width, height, language);

    #endregion

    #region Empty Input Tests

    [Fact]
    public void SelectImages_with_empty_list_returns_empty_list()
    {
        var selector = CreateSelector();
        var images = new List<EpisodeThumbnailSource>();

        var result = selector.SelectImages(images);

        Assert.Empty(result);
    }

    #endregion

    #region Language Prioritization Tests

    [Fact]
    public void SelectImages_prioritizes_no_language_images()
    {
        var selector = CreateSelector();
        var images = new List<EpisodeThumbnailSource>
        {
            CreateStill("en.jpg", 1920, 1080, "en"),
            CreateStill("fr.jpg", 1920, 1080, "fr"),
            CreateStill("neutral1.jpg", 1920, 1080, null),
            CreateStill("neutral2.jpg", 1920, 1080, null),
        };

        var result = selector.SelectImages(images, maxCount: 2);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Null(r.Language));
    }

    [Fact]
    public void SelectImages_orders_by_resolution()
    {
        var selector = CreateSelector();
        var images = new List<EpisodeThumbnailSource>
        {
            CreateStill("small.jpg", 1280, 720, null),
            CreateStill("large.jpg", 1920, 1080, null),
            CreateStill("medium.jpg", 1600, 900, null),
        };

        var result = selector.SelectImages(images);

        Assert.Equal("large.jpg", result[0].FilePath);
        Assert.Equal("medium.jpg", result[1].FilePath);
        Assert.Equal("small.jpg", result[2].FilePath);
    }

    #endregion

    #region Max Count Tests

    [Fact]
    public void SelectImages_respects_max_count()
    {
        var selector = CreateSelector();
        var images = Enumerable
            .Range(1, 20)
            .Select(i => CreateStill($"image{i}.jpg", 1920, 1080, null))
            .ToList();

        var result = selector.SelectImages(images, maxCount: 5);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void SelectImages_uses_default_max_count_of_10()
    {
        var selector = CreateSelector();
        var images = Enumerable
            .Range(1, 20)
            .Select(i => CreateStill($"image{i}.jpg", 1920, 1080, null))
            .ToList();

        var result = selector.SelectImages(images);

        Assert.Equal(10, result.Count);
    }

    #endregion
}
