using Jellyfin.Plugin.Consistart.Infrastructure;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Services.Artwork.Logo;

internal sealed class LocalLogoProvider(IItemImagePathReader pathReader) : ILocalLogoProvider
{
    public async Task<LogoSource?> TryGetLocalLogoAsync(
        BaseItem item,
        CancellationToken ct = default
    )
    {
        var path = pathReader.TryGetImagePath(item, ImageType.Logo);
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return new LogoSource(Kind: LogoSourceKind.Local, FilePath: path);
    }
}
