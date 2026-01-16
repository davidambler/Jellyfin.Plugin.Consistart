using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.Consistart.Tests.TestDoubles;

/// <summary>
/// Helper class for generating test images for unit tests.
/// </summary>
internal static class TestImageHelper
{
    /// <summary>
    /// Creates a simple solid color test image as a byte array.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="color">The solid color to fill the image with.</param>
    /// <returns>JPEG-encoded image bytes.</returns>
    public static byte[] CreateSolidColorImage(int width, int height, Color color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.BackgroundColor(color));

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a simple test poster image (2:3 aspect ratio) with a gradient.
    /// </summary>
    /// <returns>JPEG-encoded poster image bytes.</returns>
    public static byte[] CreateTestPoster(int width = 1000, int height = 1500)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.DarkSlateBlue);
            // Add simple pattern to make it visually distinct
            ctx.Fill(Color.LightSkyBlue, new Rectangle(0, 0, width / 2, height / 2));
        });

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a simple test thumbnail image (16:9 aspect ratio).
    /// </summary>
    /// <returns>JPEG-encoded thumbnail image bytes.</returns>
    public static byte[] CreateTestThumbnail(int width = 1920, int height = 1080)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.DarkGreen);
            ctx.Fill(Color.LightGreen, new Rectangle(0, 0, width / 2, height / 2));
        });

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a simple test logo with transparency.
    /// </summary>
    /// <returns>PNG-encoded logo image bytes with alpha channel.</returns>
    public static byte[] CreateTestLogo(int width = 400, int height = 200)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.Transparent);
            // Simple rectangle as "logo"
            ctx.Fill(Color.White, new Rectangle(width / 4, height / 4, width / 2, height / 2));
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
