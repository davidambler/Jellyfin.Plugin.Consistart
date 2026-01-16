namespace Jellyfin.Plugin.Consistart.Services.Rendering.Poster;

/// <summary>
/// Renders poster images.
/// </summary>
public interface IPosterRenderer
{
    /// <summary>
    /// Renders a poster image with an overlaid logo.
    /// </summary>
    /// <param name="posterBytes">The byte array representing the poster image.</param>
    /// <param name="logoBytes">The byte array representing the logo image.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array representing the rendered poster image with the logo overlaid, or null if rendering fails.</returns>
    Task<byte[]?> RenderAsync(
        byte[] posterBytes,
        byte[] logoBytes,
        CancellationToken cancellationToken = default
    );
}
