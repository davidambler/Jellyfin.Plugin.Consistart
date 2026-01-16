using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Providers;

public abstract class ConsistartProvider<T>(
    IConfigurationProvider configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<T> logger,
    IArtworkCandidateService candidateService
) : IRemoteImageProvider
{
    protected IConfigurationProvider Configuration { get; } = configuration;
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    protected ILogger<T> Logger { get; } = logger;
    protected IArtworkCandidateService CandidateService { get; } = candidateService;

    public string Name => Configuration.PluginName;

    public abstract IEnumerable<ImageType> GetSupportedImages(BaseItem item);
    public abstract bool Supports(BaseItem item);
    public abstract Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken
    );

    public async Task<HttpResponseMessage> GetImageResponse(
        string url,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL must not be empty.", nameof(url));

        var http = HttpClientFactory.CreateClient();

        try
        {
            return await http.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.LogWarning(ex, "Failed to fetch image response from {Url}", url);
            throw;
        }
    }

    protected RemoteImageInfo CreateRemoteImageInfo(
        ArtworkCandidateDto candidate,
        ImageType imageType,
        bool useBaseUrl = false
    ) =>
        new()
        {
            ProviderName = Name,
            Type = imageType,
            Url = useBaseUrl ? $"{Configuration.BaseUrl}{candidate.Url}" : candidate.Url,
            Language = candidate.Language,
            Width = candidate.Width,
            Height = candidate.Height,
        };
}
