using Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.SeasonPoster;

public class SeasonPosterSelectorTests
{
    [Fact]
    public void SelectImages_with_empty_list_returns_empty()
    {
        var selector = new SeasonPosterSelector();

        var result = selector.SelectImages([]);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_filters_to_language_neutral_posters()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource>
        {
            new("/neutral1.jpg", 1000, 500, null),
            new("/neutral2.jpg", 800, 400, ""),
            new("/ignored.jpg", 1200, 600, "en"),
            new("/also-ignored.jpg", 900, 450, " fr "),
        };

        var result = selector.SelectImages(posters);

        Assert.Equal(2, result.Count);
        Assert.All(result, poster => Assert.True(string.IsNullOrWhiteSpace(poster.Language)));
        Assert.DoesNotContain(result, poster => poster.FilePath == "/ignored.jpg");
        Assert.DoesNotContain(result, poster => poster.FilePath == "/also-ignored.jpg");
    }

    [Fact]
    public void SelectImages_orders_by_area_descending()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource>
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
        var selector = new SeasonPosterSelector();
        var posters = Enumerable
            .Range(0, 5)
            .Select(i => new SeasonPosterSource($"/poster{i}.jpg", 100 + i, 100 + i, null))
            .ToList();

        var result = selector.SelectImages(posters, maxCount: 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectImages_default_max_count_is_ten()
    {
        var selector = new SeasonPosterSelector();
        var posters = Enumerable
            .Range(0, 20)
            .Select(i => new SeasonPosterSource($"/poster{i}.jpg", 200 + i, 100 + i, null))
            .ToList();

        var result = selector.SelectImages(posters);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public void SelectImages_ignores_language_parameter()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource> { new("/neutral.jpg", 1000, 500, null) };

        var result = selector.SelectImages(posters, language: "en");

        Assert.Single(result);
        Assert.Equal("/neutral.jpg", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_with_all_language_specific_posters_returns_empty()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource>
        {
            new("/en.jpg", 1000, 500, "en"),
            new("/fr.jpg", 800, 400, "fr"),
            new("/de.jpg", 900, 450, "de"),
        };

        var result = selector.SelectImages(posters);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_handles_zero_dimensions()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource>
        {
            new("/zero.jpg", 0, 0, null),
            new("/normal.jpg", 500, 750, null),
        };

        var result = selector.SelectImages(posters);

        Assert.Equal(2, result.Count);
        Assert.Equal("/normal.jpg", result[0].FilePath);
        Assert.Equal("/zero.jpg", result[1].FilePath);
    }

    [Fact]
    public void SelectImages_preserves_equal_area_items()
    {
        var selector = new SeasonPosterSelector();
        var posters = new List<SeasonPosterSource>
        {
            new("/poster1.jpg", 100, 100, null), // 10_000
            new("/poster2.jpg", 100, 100, null), // 10_000
            new("/poster3.jpg", 100, 100, null), // 10_000
        };

        var result = selector.SelectImages(posters);

        Assert.Equal(3, result.Count);
    }
}
