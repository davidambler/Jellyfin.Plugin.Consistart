namespace Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;

/// <summary>
/// Renders episode thumbnail images.
/// </summary>
public interface IEpisodeThumbnailRenderer
{
    /// <summary>
    /// Renders an episode thumbnail image with overlay displaying episode number and optional name.
    /// </summary>
    /// <param name="thumbnailBytes">The byte array representing the episode thumbnail image.</param>
    /// <param name="episodeNumber">The episode number to display.</param>
    /// <param name="episodeName">The episode name to display, or null if not available.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array representing the rendered episode thumbnail image with episode information overlaid, or null if rendering fails.</returns>
    Task<byte[]?> RenderAsync(
        byte[] thumbnailBytes,
        int episodeNumber,
        string? episodeName = null,
        CancellationToken cancellationToken = default
    );
}
