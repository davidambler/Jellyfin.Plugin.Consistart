using Jellyfin.Plugin.Consistart.Infrastructure;
using SixLabors.Fonts;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Infrastructure;

public class FontProviderTests
{
    private readonly FontProvider _fontProvider;

    public FontProviderTests()
    {
        _fontProvider = new FontProvider();
    }

    [Fact]
    public void GetFont_with_embedded_font_returns_font_family()
    {
        // ColusRegular.otf should be an embedded resource
        var fontFamily = _fontProvider.GetFont("ColusRegular");

        Assert.Equal("Colus", fontFamily.Name);
    }

    [Fact]
    public void GetFont_with_case_insensitive_name_returns_font_family()
    {
        var fontFamily = _fontProvider.GetFont("colusregular");

        Assert.Equal("Colus", fontFamily.Name);
    }

    [Fact]
    public void GetFont_with_mixed_case_name_returns_font_family()
    {
        var fontFamily = _fontProvider.GetFont("CoLuSrEgUlAr");

        Assert.Equal("Colus", fontFamily.Name);
    }

    [Fact]
    public void GetFont_with_nonexistent_font_throws_invalid_operation_exception()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _fontProvider.GetFont("NonexistentFont")
        );

        Assert.Contains("NonexistentFont", exception.Message);
        Assert.Contains("is not loaded", exception.Message);
    }

    [Fact]
    public void GetFont_called_twice_returns_cached_instance()
    {
        var firstCall = _fontProvider.GetFont("ColusRegular");
        var secondCall = _fontProvider.GetFont("ColusRegular");

        Assert.Equal(firstCall, secondCall);
    }

    [Fact]
    public void GetFont_with_different_casing_returns_same_cached_instance()
    {
        var lowerCase = _fontProvider.GetFont("colusregular");
        var upperCase = _fontProvider.GetFont("COLUSREGULAR");
        var mixedCase = _fontProvider.GetFont("ColusRegular");

        Assert.Equal(lowerCase, upperCase);
        Assert.Equal(upperCase, mixedCase);
    }

    [Fact]
    public void Constructor_loads_embedded_fonts()
    {
        // Creating a new instance should load fonts successfully
        var provider = new FontProvider();

        // Verify at least one font was loaded (ColusRegular)
        var font = provider.GetFont("ColusRegular");
        Assert.Equal("Colus", font.Name);
    }

    [Fact]
    public void GetFont_with_empty_string_throws_invalid_operation_exception()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _fontProvider.GetFont(string.Empty)
        );

        Assert.Contains("is not loaded", exception.Message);
    }

    [Fact]
    public void GetFont_with_whitespace_throws_invalid_operation_exception()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _fontProvider.GetFont("   ")
        );

        Assert.Contains("is not loaded", exception.Message);
    }
}
