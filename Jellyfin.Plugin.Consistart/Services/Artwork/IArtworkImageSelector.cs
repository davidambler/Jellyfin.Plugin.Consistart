namespace Jellyfin.Plugin.Consistart.Services.Artwork;

internal interface IArtworkImageSelector<IArtworkImageSource>
{
    IReadOnlyList<IArtworkImageSource> SelectImages(
        IReadOnlyList<IArtworkImageSource> images,
        int maxCount = 10,
        string? language = null
    );
}
