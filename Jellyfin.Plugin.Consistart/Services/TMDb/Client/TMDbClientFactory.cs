using Jellyfin.Plugin.Consistart.Services.Configuration;
using TMDbLib.Client;

namespace Jellyfin.Plugin.Consistart.Services.TMDb.Client;

internal sealed class TMDbClientFactory(IConfigurationProvider configuration) : ITMDbClientFactory
{
    public ITMDbClientAdapter CreateClient()
    {
        var apiKey = configuration.TMDbApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("TMDb API key is not configured.");
        }

        var client = new TMDbClient(apiKey);
        return new TMDbClientAdapter(client);
    }
}
