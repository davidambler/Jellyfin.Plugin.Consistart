namespace Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;

/// <summary>
/// Renders thumbnail images.
/// </summary>
public interface IThumbnailRenderer
{
    /// <summary>
    /// Renders a thumbnail image with an overlaid logo.
    /// </summary>
    /// <param name="posterBytes">The byte array representing the thumbnail image.</param>
    /// <param name="logoBytes">The byte array representing the logo image.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array representing the rendered thumbnail image with the logo overlaid, or null if rendering fails.</returns>
    Task<byte[]?> RenderAsync(
        byte[] posterBytes,
        byte[] logoBytes,
        CancellationToken cancellationToken = default
    );
}
