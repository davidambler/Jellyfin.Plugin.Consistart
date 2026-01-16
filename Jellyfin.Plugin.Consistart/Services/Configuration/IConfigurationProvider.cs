namespace Jellyfin.Plugin.Consistart.Services.Configuration;

public interface IConfigurationProvider
{
    /// <summary>
    /// The name of the Plugin.
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// The base URL of the Jellyfin instance.
    /// e.g. http://localhost:8086
    /// </summary>
    string? BaseUrl { get; }

    /// <summary>
    /// The users TMDb API key.
    /// </summary>
    string? TMDbApiKey { get; }
}
