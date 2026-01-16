namespace Jellyfin.Plugin.Consistart.Infrastructure;

internal sealed class LocalFileReader : ILocalFileReader
{
    public async Task<byte[]?> TryReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
