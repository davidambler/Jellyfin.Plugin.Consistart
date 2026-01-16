using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

internal static class RenderUtilities
{
    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    public static void Resize(Image<Rgba32> image, int width, int height) =>
        image.Mutate(ctx =>
        {
            ctx.Resize(width, height);
        });

    /// <summary>
    /// Normalises the aspect ratio of an image by cropping it to the specified aspect ratio.
    /// </summary>
    /// <param name="image">The image to normalise.</param>
    /// <param name="aspectRatio">The target aspect ratio.</param>
    /// <param name="anchorPositionMode">The anchor position mode for cropping.</param>
    /// <param name="resizeMode">The resize mode to use.</param>
    public static void NormaliseImageAspectRatio(
        Image<Rgba32> image,
        double aspectRatio,
        AnchorPositionMode anchorPositionMode = AnchorPositionMode.Center,
        ResizeMode resizeMode = ResizeMode.Crop
    )
    {
        var currentAspectRatio = (double)image.Width / image.Height;
        if (Math.Abs(currentAspectRatio - aspectRatio) < 0.01)
            return; // Close enough!

        int targetWidth;
        int targetHeight;

        if (currentAspectRatio > aspectRatio)
        {
            // Image is too wide
            targetHeight = image.Height;
            targetWidth = (int)(targetHeight * aspectRatio);
        }
        else
        {
            // Image is too tall
            targetWidth = image.Width;
            targetHeight = (int)(targetWidth / aspectRatio);
        }

        image.Mutate(ctx =>
        {
            ctx.Resize(
                new ResizeOptions
                {
                    Size = new Size(targetWidth, targetHeight),
                    Position = anchorPositionMode,
                    Mode = resizeMode,
                }
            );
        });
    }

    /// <summary>
    /// Draws a bottom gradient overlay on the given image starting from the specified Y coordinate.
    /// </summary>
    /// <param name="image">The image to draw the gradient on.</param>
    /// <param name="startY">The Y coordinate to start the gradient from.</param>
    /// <param name="maxAlpha">The maximum alpha value for the gradient.</param>
    public static void DrawBottomGradientOverlay(Image<Rgba32> image, int startY, byte maxAlpha)
    {
        var gradientHeight = Math.Max(0, image.Height - startY);
        if (gradientHeight <= 0)
        {
            return;
        }

        using var gradient = CreateVerticalGradient(image.Width, gradientHeight, maxAlpha);
        image.Mutate(ctx => ctx.DrawImage(gradient, new Point(0, startY), 1f));
    }

    /// <summary>
    /// Calculates the safe zone rectangle for overlaying content on an image.
    /// </summary>
    /// <param name="image">The image to calculate the safe zone for.</param>
    /// <param name="options">Options specifying the safe zone dimensions and padding.</param>
    /// <returns>A rectangle representing the safe zone for overlaying content.</returns>
    public static Rectangle CalculateOverlaySafeZone(
        Image<Rgba32> image,
        OverlaySafeZoneOptions options
    )
    {
        var safeZoneWidth = image.Width - (2 * options.SidePaddingInPixels);
        var safeZoneHeight = options.SafeZoneHeightInPixels;
        var safeZoneX = options.SidePaddingInPixels;
        var safeZoneY = image.Height - safeZoneHeight - options.BottomPaddingInPixels;
        return new Rectangle(
            (int)safeZoneX,
            (int)safeZoneY,
            (int)safeZoneWidth,
            (int)safeZoneHeight
        );
    }

    /// <summary>
    /// Fits an image inside a specified safe zone by scaling it down while maintaining its aspect ratio.
    /// </summary>
    /// <param name="image">The image to fit inside the safe zone.</param>
    /// <param name="safeZone">The safe zone rectangle to fit the image into.</param>
    /// <param name="maxWidthPercentage">The maximum width percentage of the safe zone the image can occupy.</param>
    /// <param name="maxHeightPercentage">The maximum height percentage of the safe zone the image can occupy.</param>
    public static void FitImageInsideSafeZone(
        Image<Rgba32> image,
        Rectangle safeZone,
        int maxWidthPercentage = 80,
        int maxHeightPercentage = 80
    )
    {
        var maxWidth = safeZone.Width * maxWidthPercentage / 100;
        var maxHeight = safeZone.Height * maxHeightPercentage / 100;

        var widthRatio = (double)maxWidth / image.Width;
        var heightRatio = (double)maxHeight / image.Height;
        var scaleRatio = Math.Min(widthRatio, heightRatio);

        var targetWidth = (int)(image.Width * scaleRatio);
        var targetHeight = (int)(image.Height * scaleRatio);

        Resize(image, targetWidth, targetHeight);
    }

    /// <summary>
    /// Draws an image with a drop shadow onto a canvas within a specified safe zone.
    /// </summary>
    /// <param name="canvas">The canvas image to draw onto.</param>
    /// <param name="overlay">The overlay image to draw with a drop shadow.</param>
    /// <param name="safeZone">The safe zone rectangle within which to draw the overlay.</param>
    /// <param name="dropShadowOptions">Options for the drop shadow effect.</param>
    /// <param name="shadowOffset">The offset of the drop shadow relative to the overlay.</param>
    /// <param name="maxWidthPercentage">The maximum width percentage of the safe zone the overlay can occupy.</param>
    /// <param name="maxHeightPercentage">The maximum height percentage of the safe zone the overlay can occupy.</param>
    public static void DrawImageWithDropShadow(
        Image<Rgba32> canvas,
        Image<Rgba32> overlay,
        Rectangle safeZone,
        DropShadowOptions dropShadowOptions,
        Point shadowOffset,
        int maxWidthPercentage = 80,
        int maxHeightPercentage = 80
    )
    {
        FitImageInsideSafeZone(overlay, safeZone, maxWidthPercentage, maxHeightPercentage);

        var overlayPosition = CalculateCenteredPosition(safeZone, overlay.Width, overlay.Height);
        var dropShadow = GenerateDropShadow(overlay, dropShadowOptions);
        var dropShadowOrigin = CalculateCenteredPosition(
            safeZone,
            dropShadow.Width,
            dropShadow.Height
        );
        var dropShadowPosition = new Point(
            dropShadowOrigin.X + shadowOffset.X,
            dropShadowOrigin.Y + shadowOffset.Y
        );

        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(dropShadow, dropShadowPosition, 1f);
            ctx.DrawImage(overlay, overlayPosition, 1f);
        });
    }

    /// <summary>
    /// Creates a drop shadow effect for an image based on its alpha channel.
    /// The canvas is expanded to accommodate the blur without clipping at the edges.
    /// </summary>
    /// <param name="image">The original image.</param>
    /// <param name="options">Options for the drop shadow effect.</param>
    /// <returns>A new image containing the drop shadow effect with extra padding for blur.</returns>
    public static Image<Rgba32> GenerateDropShadow(Image<Rgba32> image, DropShadowOptions options)
    {
        var canvas = image.Clone(ctx =>
        {
            ctx.Resize(
                new ResizeOptions
                {
                    Size = new Size(
                        image.Width + (options.DropShadowBlurRadius * options.DropShadowBlurRadius),
                        image.Height + (options.DropShadowBlurRadius * options.DropShadowBlurRadius)
                    ),
                    Position = AnchorPositionMode.Center,
                    Mode = ResizeMode.BoxPad,
                    PadColor = Color.Transparent,
                }
            );
        });

        var shadowColor = Color.Black.WithAlpha(options.DropShadowOpacity);
        canvas.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (var x = 0; x < pixelRow.Length; x++)
                {
                    if (pixelRow[x].A > 0)
                    {
                        pixelRow[x] = shadowColor;
                    }
                }
            }
        });

        canvas.Mutate(ctx =>
        {
            ctx.GaussianBlur(options.DropShadowBlurRadius);
        });

        return canvas;
    }

    /// <summary>
    /// Calculates the optimal font size to fit the given text within the specified safe zone.
    /// </summary>
    /// <param name="fontFamily">The font family to use for measuring the text.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="safeZone">The safe zone rectangle within which the text should fit.</param>
    /// <param name="minFontSize">The minimum font size to consider.</param>
    /// <param name="maxFontSize">The maximum font size to consider.</param>
    /// <param name="fontSizeStep">The step size to decrement the font size during calculation.</param>
    /// <returns>The optimal font size that fits within the safe zone.</returns>
    public static float CalculateOptimalFontSize(
        FontFamily fontFamily,
        string text,
        Rectangle safeZone,
        float minFontSize = 20f,
        float maxFontSize = 120f,
        float fontSizeStep = 2f
    )
    {
        for (var size = maxFontSize; size >= minFontSize; size -= fontSizeStep)
        {
            var font = fontFamily.CreateFont(size, FontStyle.Regular);
            var options = new TextOptions(font)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var bounds = TextMeasurer.MeasureBounds(text, options);

            if (bounds.Width < safeZone.Width - 20 && bounds.Height < safeZone.Height - 20)
            {
                return size;
            }
        }

        return minFontSize;
    }

    /// <summary>
    /// Draws a horizontal line on the given image processing context.
    /// </summary>
    /// <param name="ctx">The image processing context to draw on.</param>
    /// <param name="startX">The starting x-coordinate of the line.</param>
    /// <param name="endX">The ending x-coordinate of the line.</param>
    /// <param name="centerY">The y-coordinate of the center of the line.</param>
    /// <param name="lineThickness">The thickness of the line.</param>
    public static void DrawHorizontalLine(
        IImageProcessingContext ctx,
        int startX,
        int endX,
        int centerY,
        int lineThickness
    )
    {
        if (endX <= startX)
        {
            return;
        }

        var rect = new Rectangle(startX, centerY - lineThickness / 2, endX - startX, lineThickness);
        ctx.Fill(Color.White, rect);
    }

    /// <summary>
    /// Creates a vertical gradient image from transparent to the specified maximum alpha.
    /// </summary>
    /// <param name="width">The width of the gradient image.</param>
    /// <param name="height">The height of the gradient image.</param>
    /// <param name="maxAlpha">The maximum alpha value at the bottom of the gradient.</param>
    /// <returns>The created vertical gradient image.</returns>
    private static Image<Rgba32> CreateVerticalGradient(int width, int height, byte maxAlpha)
    {
        var gradient = new Image<Rgba32>(width, height);
        for (var row = 0; row < gradient.Height; row++)
        {
            var progress = (float)row / gradient.Height;
            var alpha = (byte)(maxAlpha * progress);
            for (var col = 0; col < gradient.Width; col++)
            {
                gradient[col, row] = Color.FromRgba(0, 0, 0, alpha);
            }
        }

        return gradient;
    }

    /// <summary>
    /// Calculates the centered position for an image within a safe zone.
    /// </summary>
    /// <param name="safeZone">The safe zone rectangle within which the image should be centered.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>The point representing the top-left position to center the image within the safe zone.</returns>
    private static Point CalculateCenteredPosition(Rectangle safeZone, int width, int height) =>
        new(
            safeZone.X + ((safeZone.Width - width) / 2),
            safeZone.Y + ((safeZone.Height - height) / 2)
        );
}

internal sealed class OverlaySafeZoneOptions
{
    /// <summary>
    /// The number of pixels that define the safe zone height.
    /// </summary>
    public double SafeZoneHeightInPixels { get; set; }

    /// <summary>
    /// The number of pixels that define the side padding of the safe zone.
    /// </summary>
    public double SidePaddingInPixels { get; set; }

    /// <summary>
    /// The number of pixels that define the bottom padding of the safe zone.
    /// </summary>
    public double BottomPaddingInPixels { get; set; }
}

internal sealed class DropShadowOptions
{
    public int DropShadowBlurRadius { get; set; } = 8;
    public float DropShadowOpacity { get; set; } = 0.8f;
}
