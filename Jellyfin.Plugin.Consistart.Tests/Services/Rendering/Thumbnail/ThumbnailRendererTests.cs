using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;
using Jellyfin.Plugin.Consistart.Tests.TestDoubles;
using SixLabors.ImageSharp;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.Thumbnail;

public class ThumbnailRendererTests
{
    [Fact]
    public async Task RenderAsync_produces_valid_image()
    {
        var renderer = new ThumbnailRenderer();
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(thumbnailBytes, logoBytes);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RenderAsync_produces_correct_dimensions()
    {
        var renderer = new ThumbnailRenderer();
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(thumbnailBytes, logoBytes);

        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_normalizes_aspect_ratio_when_input_is_wrong_ratio()
    {
        var renderer = new ThumbnailRenderer();
        // Create thumbnail with wrong aspect ratio (4:3 instead of 16:9)
        var thumbnailBytes = TestImageHelper.CreateSolidColorImage(1600, 1200, Color.Red);
        var logoBytes = TestImageHelper.CreateTestLogo();

        var result = await renderer.RenderAsync(thumbnailBytes, logoBytes);

        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var renderer = new ThumbnailRenderer();
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        var logoBytes = TestImageHelper.CreateTestLogo();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await renderer.RenderAsync(thumbnailBytes, logoBytes, cts.Token)
        );
    }

    [Fact]
    public async Task RenderAsync_with_small_logo_produces_valid_result()
    {
        var renderer = new ThumbnailRenderer();
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        var logoBytes = TestImageHelper.CreateTestLogo(100, 50);

        var result = await renderer.RenderAsync(thumbnailBytes, logoBytes);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_large_logo_produces_valid_result()
    {
        var renderer = new ThumbnailRenderer();
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        var logoBytes = TestImageHelper.CreateTestLogo(1200, 600);

        var result = await renderer.RenderAsync(thumbnailBytes, logoBytes);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }
}
