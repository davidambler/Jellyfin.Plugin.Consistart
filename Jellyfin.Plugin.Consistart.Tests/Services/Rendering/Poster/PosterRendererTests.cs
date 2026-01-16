using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Tests.TestDoubles;
using SixLabors.ImageSharp;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.Poster;

public class PosterRendererTests
{
    [Fact]
    public async Task RenderAsync_produces_valid_image()
    {
        var renderer = new PosterRenderer();
        var posterBytes = TestImageHelper.CreateTestPoster();
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(posterBytes, logoBytes);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RenderAsync_produces_correct_dimensions()
    {
        var renderer = new PosterRenderer();
        var posterBytes = TestImageHelper.CreateTestPoster();
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(posterBytes, logoBytes);

        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_normalizes_aspect_ratio_when_input_is_wrong_ratio()
    {
        var renderer = new PosterRenderer();
        // Create poster with wrong aspect ratio (square instead of 2:3)
        var posterBytes = TestImageHelper.CreateSolidColorImage(1000, 1000, Color.Blue);
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(posterBytes, logoBytes);

        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var renderer = new PosterRenderer();
        var posterBytes = TestImageHelper.CreateTestPoster();
        var logoBytes = TestImageHelper.CreateTestLogo();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await renderer.RenderAsync(posterBytes, logoBytes, cts.Token)
        );
    }

    [Fact]
    public async Task RenderAsync_with_small_logo_produces_valid_result()
    {
        var renderer = new PosterRenderer();
        var posterBytes = TestImageHelper.CreateTestPoster();
        var logoBytes = TestImageHelper.CreateTestLogo(100, 50);

        var result = await renderer.RenderAsync(posterBytes, logoBytes);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_large_logo_produces_valid_result()
    {
        var renderer = new PosterRenderer();
        var posterBytes = TestImageHelper.CreateTestPoster();
        var logoBytes = TestImageHelper.CreateTestLogo(800, 400);

        var result = await renderer.RenderAsync(posterBytes, logoBytes);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1500, image.Height);
    }
}
