using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Controllers;

[ApiController]
[Route("consistart/render")]
public sealed class RenderController(
    ILogger<RenderController> logger,
    ITokenProtectionService tokenProtection,
    IRenderService<PosterRenderRequest> posterRenderer,
    IRenderService<SeasonPosterRenderRequest> seasonPosterRenderer,
    IRenderService<ThumbnailRenderRequest> thumbnailRenderService,
    IRenderService<EpisodeThumbnailRenderRequest> episodeThumbnailRenderer
) : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string token,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Missing token.");

        IRenderRequest request;
        try
        {
            var bytes = tokenProtection.Unprotect(token);
            var json = Encoding.UTF8.GetString(bytes);
            request = DeserializeRequest(json);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Invalid token.");
            return BadRequest("Invalid token.");
        }

        var result = request switch
        {
            PosterRenderRequest posterRequest => await posterRenderer
                .RenderAsync(posterRequest, cancellationToken)
                .ConfigureAwait(false),
            SeasonPosterRenderRequest seasonPosterRequest => await seasonPosterRenderer
                .RenderAsync(seasonPosterRequest, cancellationToken)
                .ConfigureAwait(false),
            ThumbnailRenderRequest thumbnailRenderRequest => await thumbnailRenderService
                .RenderAsync(thumbnailRenderRequest, cancellationToken)
                .ConfigureAwait(false),
            EpisodeThumbnailRenderRequest episodeThumbnailRequest => await episodeThumbnailRenderer
                .RenderAsync(episodeThumbnailRequest, cancellationToken)
                .ConfigureAwait(false),
            _ => ThrowUnsupportedRequestType(request),
        };

        if (result is null)
            return NotFound();

        return File(result.Value.Bytes, result.Value.MimeType);
    }

    // Defensive: System.Text.Json will throw if deserialization fails or type is unknown,
    // so the null check is never hit. Left for future-proofing if polymorphic handling changes.
    [ExcludeFromCodeCoverage]
    private static IRenderRequest DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<IRenderRequest>(json, _jsonOptions)
        ?? throw new InvalidOperationException("Deserialized request is null.");

    // Defensive: All known IRenderRequest types are handled in the switch.
    // This would only be reached if a new type was added but not handled.
    [ExcludeFromCodeCoverage]
    private static RenderedImage? ThrowUnsupportedRequestType(IRenderRequest request) =>
        throw new NotSupportedException(
            $"Render request type '{request.GetType().Name}' is not supported."
        );
}
