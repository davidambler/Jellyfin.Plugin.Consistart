using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Tests.TestDoubles;
using NSubstitute;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering.EpisodeThumbnail;

public class EpisodeThumbnailRendererTests
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
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, 1);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RenderAsync_produces_correct_dimensions()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, 1);

        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_normalizes_aspect_ratio_when_input_is_wrong_ratio()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        // Create thumbnail with wrong aspect ratio (4:3 instead of 16:9)
        var thumbnailBytes = TestImageHelper.CreateSolidColorImage(1600, 1200, Color.Orange);

        var result = await renderer.RenderAsync(thumbnailBytes, 5);

        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_respects_cancellation_token()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await renderer.RenderAsync(thumbnailBytes, 1, cancellationToken: cts.Token)
        );
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(99)]
    public async Task RenderAsync_handles_different_episode_numbers(int episodeNumber)
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, episodeNumber);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_episode_name_produces_valid_result()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, 1, "The Beginning");

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_null_episode_name_produces_valid_result()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, 2, null);

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_empty_episode_name_produces_valid_result()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(thumbnailBytes, 3, "");

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_with_long_episode_name_produces_valid_result()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        var result = await renderer.RenderAsync(
            thumbnailBytes,
            4,
            "The Very Long Episode Name That Should Be Handled Gracefully By The Renderer"
        );

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(1920, image.Width);
        Assert.Equal(1080, image.Height);
    }

    [Fact]
    public async Task RenderAsync_uses_font_provider()
    {
        var fontProvider = CreateFontProvider();
        var renderer = new EpisodeThumbnailRenderer(fontProvider);
        var thumbnailBytes = TestImageHelper.CreateTestThumbnail();

        await renderer.RenderAsync(thumbnailBytes, 1);

        fontProvider.Received().GetFont("ColusRegular");
    }
}
