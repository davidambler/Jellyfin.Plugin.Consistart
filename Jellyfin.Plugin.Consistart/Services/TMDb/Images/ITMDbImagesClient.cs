using TMDbLib.Objects.General;

namespace Jellyfin.Plugin.Consistart.Services.TMDb.Images;

internal interface ITMDbImagesClient
{
    Task<ImagesWithId> GetImagesAsync(
        int providerId,
        MediaKind mediaKind,
        int? seasonNumber = null,
        int? episodeNumber = null,
        CancellationToken cancellationToken = default
    );

    Task<byte[]> GetImageBytesAsync(
        string filePath,
        string size = ImageSize.Original,
        CancellationToken cancellationToken = default
    );
}
