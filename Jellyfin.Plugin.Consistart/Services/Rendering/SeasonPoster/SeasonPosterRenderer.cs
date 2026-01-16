using Jellyfin.Plugin.Consistart.Infrastructure;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;

internal sealed class SeasonPosterRenderer(IFontProvider fontProvider) : ISeasonPosterRenderer
{
    private const double ResultAspectRatio = 2d / 3d;
    private const int ResultOutputWidth = 1000;
    private const int ResultOutputHeight = 1500;

    public async Task<byte[]?> RenderAsync(
        byte[] posterBytes,
        int seasonNumber,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var poster = Image.Load<Rgba32>(posterBytes);

        RenderUtilities.NormaliseImageAspectRatio(poster, ResultAspectRatio);
        RenderUtilities.Resize(poster, ResultOutputWidth, ResultOutputHeight);

        var seasonTextSafeZone = CalculateSeasonTextSafeZone(poster);
        OverlaySeasonText(poster, $"SEASON {seasonNumber}", seasonTextSafeZone);

        cancellationToken.ThrowIfCancellationRequested();

        await using var ms = new MemoryStream();
        await poster.SaveAsJpegAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    private static Rectangle CalculateSeasonTextSafeZone(Image<Rgba32> poster) =>
        RenderUtilities.CalculateOverlaySafeZone(
            poster,
            new OverlaySafeZoneOptions
            {
                SafeZoneHeightInPixels = poster.Height / 5,
                BottomPaddingInPixels = 25,
                SidePaddingInPixels = 25,
            }
        );

    /// <summary>
    /// Overlays the season text onto the poster within the specified safe zone using the embedded Colus Regular font.
    /// </summary>
    /// <param name="poster">The poster image to overlay the text on.</param>
    /// <param name="text">The text to overlay on the poster.</param>
    /// <param name="safeZone">The area within the poster where the text should be safely rendered.</param>
    private void OverlaySeasonText(Image<Rgba32> poster, string text, Rectangle safeZone)
    {
        var fontFamily = fontProvider.GetFont("ColusRegular");
        var font = fontFamily.CreateFont(120, FontStyle.Regular);

        var textOptions = new RichTextOptions(font)
        {
            Origin = new PointF(
                safeZone.X + safeZone.Width / 2f,
                safeZone.Y + safeZone.Height / 2f
            ),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        RenderUtilities.DrawBottomGradientOverlay(poster, safeZone.Top, 180);

        poster.Mutate(ctx =>
        {
            ctx.DrawText(textOptions, text, Color.White);
        });
    }
}
