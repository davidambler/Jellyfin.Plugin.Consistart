using Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Thumbnail;

public class ThumbnailSelectorTests
{
    #region Helper Methods

    private static ThumbnailSelector CreateSelector() => new();

    private static ThumbnailSource CreateThumbnail(
        string filePath = "image.jpg",
        int width = 100,
        int height = 100,
        string? language = null
    ) => new(filePath, width, height, language);

    #endregion

    #region Empty Input Tests

    [Fact]
    public void SelectImages_with_empty_list_returns_empty_list()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>();

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_with_empty_list_and_max_count_returns_empty_list()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>();

        // Act
        var result = selector.SelectImages(images, maxCount: 5);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Language Filtering Tests

    [Fact]
    public void SelectImages_filters_out_images_with_language()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("en.jpg", 100, 100, "en"),
            CreateThumbnail("fr.jpg", 100, 100, "fr"),
            CreateThumbnail("neutral.jpg", 100, 100, null),
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Single(result);
        Assert.Equal("neutral.jpg", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_includes_images_with_whitespace_language()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("space.jpg", 100, 100, " "),
            CreateThumbnail("tab.jpg", 100, 100, "\t"),
            CreateThumbnail("empty.jpg", 100, 100, string.Empty),
            CreateThumbnail("neutral.jpg", 100, 100, null),
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        // All 4 images pass through because string.IsNullOrWhiteSpace() returns true for all
        Assert.Equal(4, result.Count);
        Assert.Contains(result, t => t.FilePath == "space.jpg");
        Assert.Contains(result, t => t.FilePath == "tab.jpg");
        Assert.Contains(result, t => t.FilePath == "empty.jpg");
        Assert.Contains(result, t => t.FilePath == "neutral.jpg");
    }

    [Fact]
    public void SelectImages_returns_empty_when_all_images_have_language()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("en.jpg", 100, 100, "en"),
            CreateThumbnail("fr.jpg", 100, 100, "fr"),
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void SelectImages_orders_by_dimension_descending()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("small.jpg", 50, 50, null), // area: 2500
            CreateThumbnail("large.jpg", 200, 200, null), // area: 40000
            CreateThumbnail("medium.jpg", 100, 100, null), // area: 10000
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("large.jpg", result[0].FilePath);
        Assert.Equal("medium.jpg", result[1].FilePath);
        Assert.Equal("small.jpg", result[2].FilePath);
    }

    [Fact]
    public void SelectImages_correctly_calculates_area_for_different_aspect_ratios()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("portrait.jpg", 100, 200, null), // area: 20000
            CreateThumbnail("landscape.jpg", 400, 100, null), // area: 40000
            CreateThumbnail("square.jpg", 150, 150, null), // area: 22500
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("landscape.jpg", result[0].FilePath);
        Assert.Equal("square.jpg", result[1].FilePath);
        Assert.Equal("portrait.jpg", result[2].FilePath);
    }

    #endregion

    #region MaxCount Tests

    [Fact]
    public void SelectImages_respects_max_count()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("image1.jpg", 10, 10, null),
            CreateThumbnail("image2.jpg", 20, 20, null),
            CreateThumbnail("image3.jpg", 30, 30, null),
            CreateThumbnail("image4.jpg", 40, 40, null),
            CreateThumbnail("image5.jpg", 50, 50, null),
        };

        // Act
        var result = selector.SelectImages(images, maxCount: 3);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("image5.jpg", result[0].FilePath);
        Assert.Equal("image4.jpg", result[1].FilePath);
        Assert.Equal("image3.jpg", result[2].FilePath);
    }

    [Fact]
    public void SelectImages_returns_all_when_count_less_than_max_count()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("image1.jpg", 10, 10, null),
            CreateThumbnail("image2.jpg", 20, 20, null),
        };

        // Act
        var result = selector.SelectImages(images, maxCount: 5);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectImages_returns_empty_when_max_count_is_zero()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("image1.jpg", 10, 10, null),
            CreateThumbnail("image2.jpg", 20, 20, null),
        };

        // Act
        var result = selector.SelectImages(images, maxCount: 0);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Combined Filter and Order Tests

    [Fact]
    public void SelectImages_filters_language_then_orders_by_dimension()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("large_en.jpg", 200, 200, "en"), // filtered
            CreateThumbnail("small_null.jpg", 50, 50, null), // area: 2500
            CreateThumbnail("medium_null.jpg", 100, 100, null), // area: 10000
            CreateThumbnail("large_null.jpg", 150, 150, null), // area: 22500
            CreateThumbnail("medium_fr.jpg", 100, 100, "fr"), // filtered
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("large_null.jpg", result[0].FilePath);
        Assert.Equal("medium_null.jpg", result[1].FilePath);
        Assert.Equal("small_null.jpg", result[2].FilePath);
    }

    [Fact]
    public void SelectImages_filters_language_then_orders_and_applies_max_count()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("large_en.jpg", 500, 500, "en"),
            CreateThumbnail("small.jpg", 10, 10, null),
            CreateThumbnail("medium.jpg", 100, 100, null),
            CreateThumbnail("large.jpg", 200, 200, null),
            CreateThumbnail("xl.jpg", 300, 300, null),
            CreateThumbnail("medium_fr.jpg", 100, 100, "fr"),
        };

        // Act
        var result = selector.SelectImages(images, maxCount: 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("xl.jpg", result[0].FilePath);
        Assert.Equal("large.jpg", result[1].FilePath);
    }

    #endregion

    #region Language Parameter Tests

    [Fact]
    public void SelectImages_with_language_parameter_still_filters_by_null_language()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("en.jpg", 100, 100, "en"),
            CreateThumbnail("null.jpg", 100, 100, null),
        };

        // Act
        // Note: The language parameter is currently not used in the implementation
        var result = selector.SelectImages(images, language: "en");

        // Assert
        Assert.Single(result);
        Assert.Equal("null.jpg", result[0].FilePath);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void SelectImages_handles_single_image()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource> { CreateThumbnail("single.jpg", 100, 100, null) };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Single(result);
        Assert.Equal("single.jpg", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_handles_zero_dimensions()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("zero.jpg", 0, 0, null),
            CreateThumbnail("small.jpg", 10, 10, null),
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("small.jpg", result[0].FilePath);
        Assert.Equal("zero.jpg", result[1].FilePath);
    }

    [Fact]
    public void SelectImages_handles_large_dimensions()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("large.jpg", 4000, 6000, null), // area: 24,000,000
            CreateThumbnail("small.jpg", 100, 100, null), // area: 10,000
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("large.jpg", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_handles_identical_dimensions()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>
        {
            CreateThumbnail("image1.jpg", 100, 100, null),
            CreateThumbnail("image2.jpg", 100, 100, null),
            CreateThumbnail("image3.jpg", 100, 100, null),
        };

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(3, result.Count);
        // Order is stable for equal items
        Assert.Contains("image1.jpg", result.Select(t => t.FilePath));
        Assert.Contains("image2.jpg", result.Select(t => t.FilePath));
        Assert.Contains("image3.jpg", result.Select(t => t.FilePath));
    }

    [Fact]
    public void SelectImages_default_max_count_is_ten()
    {
        // Arrange
        var selector = CreateSelector();
        var images = new List<ThumbnailSource>();
        for (int i = 0; i < 15; i++)
        {
            images.Add(CreateThumbnail($"image{i}.jpg", 100 + i, 100 + i, null));
        }

        // Act
        var result = selector.SelectImages(images);

        // Assert
        Assert.Equal(10, result.Count);
    }

    #endregion
}
