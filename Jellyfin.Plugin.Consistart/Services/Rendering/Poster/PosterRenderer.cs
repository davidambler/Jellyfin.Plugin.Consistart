using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jellyfin.Plugin.Consistart.Services.Rendering.Poster;

internal sealed class PosterRenderer : IPosterRenderer
{
    private const double ResultAspectRatio = 2d / 3d;
    private const int ResultOutputWidth = 1000;
    private const int ResultOutputHeight = 1500;

    public async Task<byte[]?> RenderAsync(
        byte[] posterBytes,
        byte[] logoBytes,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var poster = Image.Load<Rgba32>(posterBytes);
        using var logo = Image.Load<Rgba32>(logoBytes);

        RenderUtilities.NormaliseImageAspectRatio(poster, ResultAspectRatio);
        RenderUtilities.Resize(poster, ResultOutputWidth, ResultOutputHeight);
        RenderUtilities.DrawImageWithDropShadow(
            poster,
            logo,
            RenderUtilities.CalculateOverlaySafeZone(
                poster,
                new OverlaySafeZoneOptions
                {
                    SafeZoneHeightInPixels = poster.Height / 5,
                    BottomPaddingInPixels = 25,
                    SidePaddingInPixels = 25,
                }
            ),
            new DropShadowOptions { DropShadowBlurRadius = 4, DropShadowOpacity = 0.75f },
            shadowOffset: new Point(4, 4)
        );

        cancellationToken.ThrowIfCancellationRequested();

        await using var ms = new MemoryStream();
        await poster.SaveAsJpegAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }
}
