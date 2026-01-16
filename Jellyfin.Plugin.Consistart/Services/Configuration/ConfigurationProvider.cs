using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Consistart.Services.Configuration;

[ExcludeFromCodeCoverage]
public sealed class ConfigurationProvider : IConfigurationProvider
{
    public string PluginName => Plugin.Instance.Name ?? "Consistart";

    public string? BaseUrl => Plugin.Instance.Configuration.BaseUrl;

    public string? TMDbApiKey => Plugin.Instance.Configuration.TMDbApiKey;
}
