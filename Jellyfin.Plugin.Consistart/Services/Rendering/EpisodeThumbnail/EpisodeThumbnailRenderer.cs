using Jellyfin.Plugin.Consistart.Infrastructure;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;

internal sealed class EpisodeThumbnailRenderer(IFontProvider fontProvider)
    : IEpisodeThumbnailRenderer
{
    private const double ResultAspectRatio = 16d / 9d;
    private const int ResultOutputWidth = 1920;
    private const int ResultOutputHeight = 1080;

    // Text overlay constants
    private const int GradientOverlayOffset = 200;
    private const int GradientOverlayHeight = 220;
    private const int BottomPadding = 75;
    private const double SidePaddingRatio = 0.1;
    private const int EpisodeNumberMaxFontSizeWithName = 60;
    private const int EpisodeNumberMaxFontSizeWithoutName = 80;
    private const int EpisodeNameMaxFontSize = 80;
    private const int LineThickness = 2;
    private const int LinePadding = 15;
    private const int LineVerticalPadding = 5;

    public async Task<byte[]?> RenderAsync(
        byte[] thumbnailBytes,
        int episodeNumber,
        string? episodeName = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var thumbnail = Image.Load<Rgba32>(thumbnailBytes);

        RenderUtilities.NormaliseImageAspectRatio(thumbnail, ResultAspectRatio);
        RenderUtilities.Resize(thumbnail, ResultOutputWidth, ResultOutputHeight);
        OverlayText(thumbnail, $"EPISODE {episodeNumber}", episodeName);

        cancellationToken.ThrowIfCancellationRequested();

        await using var ms = new MemoryStream();
        await thumbnail.SaveAsJpegAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <summary>
    /// Overlays the episode number and name onto the thumbnail image.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image.</param>
    /// <param name="episodeNumberText">The episode number text.</param>
    /// <param name="episodeName">The episode name.</param>
    private void OverlayText(Image<Rgba32> thumbnail, string episodeNumberText, string? episodeName)
    {
        var hasEpisodeName = HasValidEpisodeName(episodeName, episodeNumberText);
        var episodeNameSafeZone = CalculateEpisodeNameSafeZone(thumbnail, hasEpisodeName);
        var episodeNumberSafeZone = CalculateEpisodeNumberSafeZone(
            thumbnail,
            episodeNameSafeZone,
            hasEpisodeName
        );

        var fontFamily = fontProvider.GetFont("ColusRegular");

        RenderUtilities.DrawBottomGradientOverlay(
            thumbnail,
            episodeNumberSafeZone.Top - GradientOverlayOffset,
            GradientOverlayHeight
        );

        var episodeNumberBounds = DrawEpisodeNumber(
            thumbnail,
            episodeNumberText,
            episodeNumberSafeZone,
            fontFamily,
            hasEpisodeName
        );

        if (episodeNameSafeZone.HasValue && episodeName != null)
        {
            DrawEpisodeName(
                thumbnail,
                episodeName,
                episodeNameSafeZone.Value,
                fontFamily,
                episodeNumberBounds
            );
        }
    }

    /// <summary>
    /// Determines if the episode name is valid for rendering.
    /// </summary>
    /// <param name="episodeName">The episode name.</param>
    /// <param name="episodeNumberText">The episode number text.</param>
    /// <returns>True if the episode name is valid; otherwise, false.</returns>
    private static bool HasValidEpisodeName(string? episodeName, string episodeNumberText)
    {
        return !string.IsNullOrWhiteSpace(episodeName)
            && !string.Equals(episodeName, episodeNumberText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates the safe zone for the episode name overlay.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image.</param>
    /// <param name="hasEpisodeName">Indicates whether the episode name is present.</param>
    /// <returns>The safe zone rectangle for the episode name overlay, or null if no episode name is present.</returns>
    private static Rectangle? CalculateEpisodeNameSafeZone(
        Image<Rgba32> thumbnail,
        bool hasEpisodeName
    )
    {
        if (!hasEpisodeName)
        {
            return null;
        }

        return RenderUtilities.CalculateOverlaySafeZone(
            thumbnail,
            new OverlaySafeZoneOptions
            {
                SafeZoneHeightInPixels = thumbnail.Height / 12,
                BottomPaddingInPixels = BottomPadding,
                SidePaddingInPixels = (int)(thumbnail.Width * SidePaddingRatio),
            }
        );
    }

    /// <summary>
    /// Calculates the safe zone for the episode number overlay.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image.</param>
    /// <param name="episodeNameSafeZone">The safe zone for the episode name overlay.</param>
    /// <param name="hasEpisodeName">Indicates whether the episode name is present.</param>
    /// <returns>The safe zone rectangle for the episode number overlay.</returns>
    private static Rectangle CalculateEpisodeNumberSafeZone(
        Image<Rgba32> thumbnail,
        Rectangle? episodeNameSafeZone,
        bool hasEpisodeName
    )
    {
        var episodeNumberHeight = hasEpisodeName ? thumbnail.Height / 15 : thumbnail.Height / 12;

        if (episodeNameSafeZone.HasValue)
        {
            return new Rectangle(
                episodeNameSafeZone.Value.X,
                episodeNameSafeZone.Value.Y - episodeNumberHeight,
                episodeNameSafeZone.Value.Width,
                episodeNumberHeight
            );
        }

        return RenderUtilities.CalculateOverlaySafeZone(
            thumbnail,
            new OverlaySafeZoneOptions
            {
                SafeZoneHeightInPixels = episodeNumberHeight,
                BottomPaddingInPixels = BottomPadding,
                SidePaddingInPixels = (int)(thumbnail.Width * SidePaddingRatio),
            }
        );
    }

    /// <summary>
    /// Draws the episode number onto the thumbnail image.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image.</param>
    /// <param name="episodeNumberText">The episode number text.</param>
    /// <param name="safeZone">The safe zone rectangle for the episode number overlay.</param>
    /// <param name="fontFamily">The font family to use for rendering the text.</param>
    /// <param name="hasEpisodeName">Indicates whether the episode name is present.</param>
    /// <returns>The bounds of the rendered episode number text.</returns>
    private static TextBounds DrawEpisodeNumber(
        Image<Rgba32> thumbnail,
        string episodeNumberText,
        Rectangle safeZone,
        FontFamily fontFamily,
        bool hasEpisodeName
    )
    {
        var maxFontSize = hasEpisodeName
            ? EpisodeNumberMaxFontSizeWithName
            : EpisodeNumberMaxFontSizeWithoutName;

        var font = fontFamily.CreateFont(
            RenderUtilities.CalculateOptimalFontSize(
                fontFamily,
                episodeNumberText,
                safeZone,
                maxFontSize: maxFontSize
            ),
            FontStyle.Regular
        );

        var textOptions = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var textSize = TextMeasurer.MeasureBounds(episodeNumberText, textOptions);
        var x = safeZone.Left + ((safeZone.Width - (int)textSize.Width) / 2);
        var y = safeZone.Top + ((safeZone.Height - (int)textSize.Height) / 2);

        thumbnail.Mutate(ctx =>
            ctx.DrawText(episodeNumberText, font, Color.White, new PointF(x, y))
        );

        return new TextBounds(x, y, (int)textSize.Width, (int)textSize.Height);
    }

    /// <summary>
    /// Draws the episode name onto the thumbnail image.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image.</param>
    /// <param name="episodeName">The episode name text.</param>
    /// <param name="safeZone">The safe zone rectangle for the episode name overlay.</param>
    /// <param name="fontFamily">The font family to use for rendering the text.</param>
    /// <param name="episodeNumberBounds">The bounds of the rendered episode number text.</param>
    private static void DrawEpisodeName(
        Image<Rgba32> thumbnail,
        string episodeName,
        Rectangle safeZone,
        FontFamily fontFamily,
        TextBounds episodeNumberBounds
    )
    {
        var font = fontFamily.CreateFont(
            RenderUtilities.CalculateOptimalFontSize(
                fontFamily,
                episodeName,
                safeZone,
                maxFontSize: EpisodeNameMaxFontSize
            ),
            FontStyle.Regular
        );

        var textOptions = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var textSize = TextMeasurer.MeasureBounds(episodeName, textOptions);
        var x = safeZone.Left + ((safeZone.Width - (int)textSize.Width) / 2);
        var y = safeZone.Top + ((safeZone.Height - (int)textSize.Height) / 2);
        var nameTextBounds = new TextBounds(x, y, (int)textSize.Width, (int)textSize.Height);

        thumbnail.Mutate(ctx =>
        {
            DrawDecorativeLines(ctx, episodeNumberBounds, nameTextBounds);
            ctx.DrawText(episodeName, font, Color.White, new PointF(x, y));
        });
    }

    /// <summary>
    /// Draws decorative lines flanking the episode number text.
    /// </summary>
    /// <param name="ctx">The image processing context.</param>
    /// <param name="episodeNumberBounds">The bounds of the episode number text.</param>
    /// <param name="episodeNameBounds">The bounds of the episode name text.</param>
    private static void DrawDecorativeLines(
        IImageProcessingContext ctx,
        TextBounds episodeNumberBounds,
        TextBounds episodeNameBounds
    )
    {
        var verticalCenter = episodeNumberBounds.VerticalCenter + LineVerticalPadding;

        // Left line: from episode name left edge to episode number text start
        RenderUtilities.DrawHorizontalLine(
            ctx,
            episodeNameBounds.Left,
            episodeNumberBounds.Left - LinePadding,
            verticalCenter,
            LineThickness
        );

        // Right line: from episode number text end to episode name right edge
        RenderUtilities.DrawHorizontalLine(
            ctx,
            episodeNumberBounds.Right + LinePadding,
            episodeNameBounds.Right,
            verticalCenter,
            LineThickness
        );
    }

    /// <summary>
    /// Represents the bounds of rendered text.
    /// </summary>
    /// <param name="X">The X-coordinate of the text bounds.</param>
    /// <param name="Y">The Y-coordinate of the text bounds.</param>
    /// <param name="Width">The width of the text bounds.</param>
    /// <param name="Height">The height of the text bounds.</param>
    private readonly record struct TextBounds(int X, int Y, int Width, int Height)
    {
        public int Left => X;
        public int Right => X + Width;
        public int VerticalCenter => Y + Height / 2;
    }
}
