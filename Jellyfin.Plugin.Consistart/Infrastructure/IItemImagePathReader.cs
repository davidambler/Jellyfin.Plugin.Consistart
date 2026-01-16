using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Infrastructure;

public interface IItemImagePathReader
{
    string? TryGetImagePath(BaseItem item, ImageType imageType);
}
