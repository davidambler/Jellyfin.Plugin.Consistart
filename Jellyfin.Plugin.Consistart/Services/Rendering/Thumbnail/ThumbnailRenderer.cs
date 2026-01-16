using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;

internal sealed class ThumbnailRenderer : IThumbnailRenderer
{
    private const double ResultAspectRatio = 16d / 9d;
    private const int ResultOutputWidth = 1920;
    private const int ResultOutputHeight = 1080;

    public async Task<byte[]?> RenderAsync(
        byte[] thumbnailBytes,
        byte[] logoBytes,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var thumbnail = Image.Load<Rgba32>(thumbnailBytes);
        using var logo = Image.Load<Rgba32>(logoBytes);

        RenderUtilities.NormaliseImageAspectRatio(thumbnail, ResultAspectRatio);
        RenderUtilities.Resize(thumbnail, ResultOutputWidth, ResultOutputHeight);
        RenderUtilities.DrawImageWithDropShadow(
            thumbnail,
            logo,
            CalculateLogoSafeZone(thumbnail),
            new DropShadowOptions { DropShadowBlurRadius = 4, DropShadowOpacity = 0.75f },
            shadowOffset: new Point(4, 4)
        );

        cancellationToken.ThrowIfCancellationRequested();

        await using var ms = new MemoryStream();
        await thumbnail.SaveAsJpegAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    private static Rectangle CalculateLogoSafeZone(Image<Rgba32> thumbnail) =>
        RenderUtilities.CalculateOverlaySafeZone(
            thumbnail,
            new OverlaySafeZoneOptions
            {
                SafeZoneHeightInPixels = thumbnail.Height / 4,
                BottomPaddingInPixels = 20,
                SidePaddingInPixels = thumbnail.Width / 4,
            }
        );
}
