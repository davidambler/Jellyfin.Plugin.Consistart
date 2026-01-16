using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Infrastructure;

[ExcludeFromCodeCoverage]
internal sealed class ItemImagePathReader : IItemImagePathReader
{
    public string? TryGetImagePath(BaseItem item, ImageType imageType) =>
        item.GetImagePath(imageType);
}
