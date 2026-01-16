using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

public interface ILocalLogoProvider
{
    /// <summary>
    /// Tries to get a local logo for the given item.
    /// </summary>
    /// <param name="item">The media item for which to get a local logo.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The local logo source if found; otherwise, null.</returns>
    Task<LogoSource?> TryGetLocalLogoAsync(
        BaseItem item,
        CancellationToken cancellationToken = default
    );
}
