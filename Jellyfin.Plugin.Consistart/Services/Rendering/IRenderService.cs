namespace Jellyfin.Plugin.Consistart.Services.Rendering;

/// <summary>
/// A service that prepares the necessary data for rendering an image based on the provided request.
/// </summary>
/// <typeparam name="T">The type of render request.</typeparam>
public interface IRenderService<T>
    where T : IRenderRequest
{
    Task<RenderedImage?> RenderAsync(T request, CancellationToken cancellationToken);
}

public readonly record struct RenderedImage(byte[] Bytes, string MimeType);
