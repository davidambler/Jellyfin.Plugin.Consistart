using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Tests.TestDoubles;
using NSubstitute;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.SeasonPoster;

public class SeasonPosterRendererTests
{
    private static IFontProvider CreateFontProvider()
    {
        var baseProvider = new FontProvider();
        var fontFamily = baseProvider.GetFont("ColusRegular");

        var fontProvider = Substitute.For<IFontProvider>();
        fontProvider.GetFont(Arg.Any<string>()).Returns(fontFamily);
        return fontProvider;
    }

    [Fact]
    public async Task RenderAsync_produces_valid_image()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        var posterBytes = TestImageHelper.CreateTestPoster();

        var result = await renderer.RenderAsync(posterBytes, 1);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RenderAsync_produces_correct_dimensions()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        var posterBytes = TestImageHelper.CreateTestPoster();

        var result = await renderer.RenderAsync(posterBytes, 1);

        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_normalizes_aspect_ratio_when_input_is_wrong_ratio()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        // Create poster with wrong aspect ratio (square)
        var posterBytes = TestImageHelper.CreateSolidColorImage(1000, 1000, Color.Purple);

        var result = await renderer.RenderAsync(posterBytes, 2);

        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        var posterBytes = TestImageHelper.CreateTestPoster();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await renderer.RenderAsync(posterBytes, 1, cts.Token)
        );
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(99)]
    public async Task RenderAsync_handles_different_season_numbers(int seasonNumber)
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        var posterBytes = TestImageHelper.CreateTestPoster();

        var result = await renderer.RenderAsync(posterBytes, seasonNumber);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_uses_font_provider()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new SeasonPosterRenderer(fontProvider);
        var posterBytes = TestImageHelper.CreateTestPoster();

        await renderer.RenderAsync(posterBytes, 1);

        fontProvider.Received(1).GetFont("ColusRegular");
    }
}
