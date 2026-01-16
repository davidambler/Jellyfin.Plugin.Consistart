namespace Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;

/// <summary>
/// Renders poster images.
/// </summary>
public interface ISeasonPosterRenderer
{
    /// <summary>
    /// Renders a poster image with an overlaid logo.
    /// </summary>
    /// <param name="posterBytes">The byte array representing the poster image.</param>
    /// <param name="seasonNumber">The season number to be displayed on the poster.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array representing the rendered poster image with the logo overlaid, or null if rendering fails.</returns>
    Task<byte[]?> RenderAsync(
        byte[] posterBytes,
        int seasonNumber,
        CancellationToken cancellationToken = default
    );
}
