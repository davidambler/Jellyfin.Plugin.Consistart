using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Logo;

public class LogoSelectorTests
{
    #region Basic Functionality Tests

    [Fact]
    public void SelectImages_with_empty_list_returns_empty()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>();

        var result = selector.SelectImages(logos);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_with_single_logo_returns_it()
    {
        var selector = new LogoSelector();
        var logo = new LogoSource(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en");
        var logos = new List<LogoSource> { logo };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Single(result);
        Assert.Same(logo, result[0]);
    }

    [Fact]
    public void SelectImages_respects_max_count()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo3.png", 700, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo4.png", 800, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo5.png", 900, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 3, language: "en");

        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Language Filtering Tests

    [Fact]
    public void SelectImages_filters_by_language_when_specified()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/en1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/fr1.png", 500, 200, "fr"),
            new(LogoSourceKind.TMDb, "/en2.png", 600, 200, "en"),
            new(LogoSourceKind.TMDb, "/fr2.png", 600, 200, "fr"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.All(result, logo => Assert.Equal("en", logo.Language));
    }

    [Fact]
    public void SelectImages_language_filter_is_case_insensitive()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/en1.png", 500, 200, "EN"),
            new(LogoSourceKind.TMDb, "/fr1.png", 500, 200, "fr"),
            new(LogoSourceKind.TMDb, "/en2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "En");

        Assert.Equal(2, result.Count);
        Assert.All(
            result,
            logo => Assert.Equal("en", logo.Language, StringComparer.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void SelectImages_with_null_language_returns_empty()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "fr"),
        };

        var result = selector.SelectImages(logos, language: null);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_with_no_matching_language_returns_empty()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "fr"),
        };

        var result = selector.SelectImages(logos, language: "de");

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_handles_logos_with_null_language()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, null),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Single(result);
        Assert.Equal("en", result[0].Language);
    }

    #endregion

    #region Ordering Tests - Width > Height Priority

    [Fact]
    public void SelectImages_prefers_wider_than_taller_logos()
    {
        var selector = new LogoSelector();
        var widerLogo = new LogoSource(LogoSourceKind.TMDb, "/wide.png", 500, 200, "en");
        var tallerLogo = new LogoSource(LogoSourceKind.TMDb, "/tall.png", 200, 500, "en");
        var logos = new List<LogoSource> { tallerLogo, widerLogo };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.Same(widerLogo, result[0]);
        Assert.Same(tallerLogo, result[1]);
    }

    [Fact]
    public void SelectImages_correctly_identifies_square_logos()
    {
        var selector = new LogoSelector();
        var squareLogo = new LogoSource(LogoSourceKind.TMDb, "/square.png", 500, 500, "en");
        var wideLogo = new LogoSource(LogoSourceKind.TMDb, "/wide.png", 600, 200, "en");
        var logos = new List<LogoSource> { squareLogo, wideLogo };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        // Wide logo (600x200) should come first
        Assert.Same(wideLogo, result[0]);
        // Square logo (500x500) is not wider than tall, so comes second
        Assert.Same(squareLogo, result[1]);
    }

    #endregion

    #region Ordering Tests - Aspect Ratio Priority

    [Fact]
    public void SelectImages_orders_by_aspect_ratio_for_wide_logos()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 400, 200, "en"), // 2:1 ratio
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "en"), // 3:1 ratio (highest)
            new(LogoSourceKind.TMDb, "/logo3.png", 500, 250, "en"), // 2:1 ratio
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(3, result.Count);
        // 3:1 ratio should be first
        Assert.Equal("/logo2.png", result[0].FilePath);
        // 2:1 ratios should follow (then sorted by width)
        Assert.Equal("/logo3.png", result[1].FilePath);
        Assert.Equal("/logo1.png", result[2].FilePath);
    }

    [Fact]
    public void SelectImages_handles_extreme_aspect_ratios()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/normal.png", 500, 200, "en"), // 2.5:1
            new(LogoSourceKind.TMDb, "/extreme.png", 1000, 100, "en"), // 10:1 (highest)
            new(LogoSourceKind.TMDb, "/narrow.png", 300, 200, "en"), // 1.5:1
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(3, result.Count);
        Assert.Equal("/extreme.png", result[0].FilePath);
        Assert.Equal("/normal.png", result[1].FilePath);
        Assert.Equal("/narrow.png", result[2].FilePath);
    }

    #endregion

    #region Ordering Tests - Width Priority

    [Fact]
    public void SelectImages_orders_by_width_when_aspect_ratios_equal()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/small.png", 400, 200, "en"), // 2:1, 400px
            new(LogoSourceKind.TMDb, "/large.png", 800, 400, "en"), // 2:1, 800px
            new(LogoSourceKind.TMDb, "/medium.png", 600, 300, "en"), // 2:1, 600px
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(3, result.Count);
        Assert.Equal("/large.png", result[0].FilePath);
        Assert.Equal("/medium.png", result[1].FilePath);
        Assert.Equal("/small.png", result[2].FilePath);
    }

    [Fact]
    public void SelectImages_prefers_larger_width_over_smaller_with_same_ratio()
    {
        var selector = new LogoSelector();
        var smallLogo = new LogoSource(LogoSourceKind.TMDb, "/small.png", 200, 100, "en");
        var largeLogo = new LogoSource(LogoSourceKind.TMDb, "/large.png", 1000, 500, "en");
        var logos = new List<LogoSource> { smallLogo, largeLogo };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.Same(largeLogo, result[0]);
        Assert.Same(smallLogo, result[1]);
    }

    #endregion

    #region Edge Cases - Dimensions

    [Fact]
    public void SelectImages_handles_zero_width()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/zero.png", 0, 100, "en"),
            new(LogoSourceKind.TMDb, "/normal.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        // Normal logo should come first (wider than tall)
        Assert.Equal("/normal.png", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_handles_zero_height()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/zero.png", 100, 0, "en"),
            new(LogoSourceKind.TMDb, "/normal.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        // Both are "wider than tall" but aspect ratio calculation handles division by zero protection
    }

    [Fact]
    public void SelectImages_handles_both_dimensions_zero()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/zero.png", 0, 0, "en"),
            new(LogoSourceKind.TMDb, "/normal.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.Equal("/normal.png", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_handles_negative_dimensions()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/negative.png", -100, 200, "en"),
            new(LogoSourceKind.TMDb, "/normal.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.Equal("/normal.png", result[0].FilePath);
    }

    #endregion

    #region Max Count Edge Cases

    [Fact]
    public void SelectImages_with_max_count_zero_returns_empty()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 0, language: "en");

        Assert.Empty(result);
    }

    [Fact]
    public void SelectImages_with_max_count_one_returns_single_best()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/small.png", 300, 200, "en"),
            new(LogoSourceKind.TMDb, "/large.png", 1000, 200, "en"), // Best
            new(LogoSourceKind.TMDb, "/medium.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 1, language: "en");

        Assert.Single(result);
        Assert.Equal("/large.png", result[0].FilePath);
    }

    [Fact]
    public void SelectImages_with_max_count_larger_than_list_returns_all()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 100, language: "en");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectImages_default_max_count_is_10()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>();
        for (var i = 0; i < 20; i++)
        {
            logos.Add(new LogoSource(LogoSourceKind.TMDb, $"/logo{i}.png", 500 + i, 200, "en"));
        }

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(10, result.Count);
    }

    #endregion

    #region Logo Source Kind Tests

    [Fact]
    public void SelectImages_works_with_local_logos()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.Local, "/local1.png", 500, 200, "en"),
            new(LogoSourceKind.Local, "/local2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.All(result, logo => Assert.Equal(LogoSourceKind.Local, logo.Kind));
    }

    [Fact]
    public void SelectImages_works_with_tmdb_logos()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/tmdb1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/tmdb2.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        Assert.All(result, logo => Assert.Equal(LogoSourceKind.TMDb, logo.Kind));
    }

    [Fact]
    public void SelectImages_works_with_mixed_source_kinds()
    {
        var selector = new LogoSelector();
        var localLogo = new LogoSource(LogoSourceKind.Local, "/local.png", 600, 200, "en");
        var tmdbLogo = new LogoSource(LogoSourceKind.TMDb, "/tmdb.png", 500, 200, "en");
        var logos = new List<LogoSource> { tmdbLogo, localLogo };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        // Local logo has higher width, should be first
        Assert.Equal(LogoSourceKind.Local, result[0].Kind);
        Assert.Equal(LogoSourceKind.TMDb, result[1].Kind);
    }

    #endregion

    #region Complex Ordering Scenarios

    [Fact]
    public void SelectImages_applies_all_ordering_criteria_correctly()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            // Tall logo (should be last)
            new(LogoSourceKind.TMDb, "/tall.png", 200, 500, "en"),
            // Wide with small aspect ratio and small width
            new(LogoSourceKind.TMDb, "/wide1.png", 300, 200, "en"), // 1.5:1, 300px
            // Wide with medium aspect ratio and medium width
            new(LogoSourceKind.TMDb, "/wide2.png", 600, 200, "en"), // 3:1, 600px - BEST
            // Wide with same aspect ratio but smaller width
            new(LogoSourceKind.TMDb, "/wide3.png", 400, 200, "en"), // 2:1, 400px
            // Wide with same aspect ratio but larger width
            new(LogoSourceKind.TMDb, "/wide4.png", 800, 400, "en"), // 2:1, 800px
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(5, result.Count);
        // Highest aspect ratio first
        Assert.Equal("/wide2.png", result[0].FilePath);
        // Same aspect ratio, larger width first
        Assert.Equal("/wide4.png", result[1].FilePath);
        Assert.Equal("/wide3.png", result[2].FilePath);
        // Lower aspect ratio
        Assert.Equal("/wide1.png", result[3].FilePath);
        // Not wider than tall, last
        Assert.Equal("/tall.png", result[4].FilePath);
    }

    [Fact]
    public void SelectImages_with_identical_dimensions_maintains_stable_order()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/logo1.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo2.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/logo3.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(3, result.Count);
        // Order should be maintained from input when all criteria are equal
        Assert.Equal("/logo1.png", result[0].FilePath);
        Assert.Equal("/logo2.png", result[1].FilePath);
        Assert.Equal("/logo3.png", result[2].FilePath);
    }

    #endregion

    #region Integration with Language and Max Count

    [Fact]
    public void SelectImages_applies_language_filter_before_ordering()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/en-small.png", 300, 200, "en"),
            new(LogoSourceKind.TMDb, "/fr-large.png", 1000, 200, "fr"),
            new(LogoSourceKind.TMDb, "/en-large.png", 800, 200, "en"),
        };

        var result = selector.SelectImages(logos, language: "en");

        Assert.Equal(2, result.Count);
        // FR logo should not be included even though it's larger
        Assert.Equal("/en-large.png", result[0].FilePath);
        Assert.Equal("/en-small.png", result[1].FilePath);
    }

    [Fact]
    public void SelectImages_applies_max_count_after_ordering()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/small.png", 300, 200, "en"),
            new(LogoSourceKind.TMDb, "/large.png", 800, 200, "en"),
            new(LogoSourceKind.TMDb, "/medium.png", 500, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 2, language: "en");

        Assert.Equal(2, result.Count);
        // Should get top 2 after ordering
        Assert.Equal("/large.png", result[0].FilePath);
        Assert.Equal("/medium.png", result[1].FilePath);
    }

    [Fact]
    public void SelectImages_with_all_parameters_works_correctly()
    {
        var selector = new LogoSelector();
        var logos = new List<LogoSource>
        {
            new(LogoSourceKind.TMDb, "/en1.png", 300, 200, "en"),
            new(LogoSourceKind.TMDb, "/en2.png", 800, 200, "en"),
            new(LogoSourceKind.TMDb, "/en3.png", 500, 200, "en"),
            new(LogoSourceKind.TMDb, "/fr1.png", 1000, 200, "fr"),
            new(LogoSourceKind.TMDb, "/en4.png", 600, 200, "en"),
        };

        var result = selector.SelectImages(logos, maxCount: 3, language: "en");

        Assert.Equal(3, result.Count);
        // Should get top 3 English logos by width
        Assert.Equal("/en2.png", result[0].FilePath); // 800px
        Assert.Equal("/en4.png", result[1].FilePath); // 600px
        Assert.Equal("/en3.png", result[2].FilePath); // 500px
    }

    #endregion
}
