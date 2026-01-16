using Jellyfin.Plugin.Consistart.Services.Artwork.Poster;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Poster;

public class PosterSelectorTests
{
    [Fact]
    public void SelectImages_with_empty_list_returns_empty()
    {
        var selector = new PosterSelector();

        var result = selector.SelectImages([]);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_filters_to_language_neutral_posters()
    {
        var selector = new PosterSelector();
        var posters = new List<PosterSource>
        {
            new("/neutral1.jpg", 1000, 500, null),
            new("/neutral2.jpg", 800, 400, ""),
            new("/ignored.jpg", 1200, 600, "en"),
            new("/also-ignored.jpg", 900, 450, " fr "),
        };

        var result = selector.SelectImages(posters, language: "en");

        Assert.Equal(2, result.Count);
        Assert.All(result, poster => Assert.True(string.IsNullOrWhiteSpace(poster.Language)));
        Assert.DoesNotContain(result, poster => poster.FilePath == "/ignored.jpg");
        Assert.DoesNotContain(result, poster => poster.FilePath == "/also-ignored.jpg");
    }

    [Fact]
    public void SelectImages_orders_by_area_descending()
    {
        var selector = new PosterSelector();
        var posters = new List<PosterSource>
        {
            new("/small.jpg", 400, 200, null), // 80_000
            new("/medium.jpg", 600, 300, null), // 180_000
            new("/large.jpg", 1000, 400, null), // 400_000
        };

        var result = selector.SelectImages(posters);

        Assert.Equal(
            new[] { "/large.jpg", "/medium.jpg", "/small.jpg" },
            result.Select(p => p.FilePath)
        );
    }

    [Fact]
    public void SelectImages_respects_max_count()
    {
        var selector = new PosterSelector();
        var posters = Enumerable
            .Range(0, 5)
            .Select(i => new PosterSource($"/poster{i}.jpg", 100 + i, 100 + i, null))
            .ToList();

        var result = selector.SelectImages(posters, maxCount: 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectImages_default_max_count_is_ten()
    {
        var selector = new PosterSelector();
        var posters = Enumerable
            .Range(0, 20)
            .Select(i => new PosterSource($"/poster{i}.jpg", 200 + i, 100 + i, null))
            .ToList();

        var result = selector.SelectImages(posters);

        Assert.Equal(10, result.Count);
    }
}
