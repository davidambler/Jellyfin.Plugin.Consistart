using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork;

internal interface IArtworkImageProvider<IArtworkImageSource>
{
    Task<IReadOnlyList<IArtworkImageSource>> GetImagesAsync(
        BaseItem item,
        CancellationToken cancellationToken
    );
}
