namespace Jellyfin.Plugin.Consistart.Infrastructure;

public interface ILocalFileReader
{
    Task<byte[]?> TryReadAllBytesAsync(string path, CancellationToken ct = default);
}
