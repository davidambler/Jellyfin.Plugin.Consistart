using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Consistart;

[ExcludeFromCodeCoverage]
public sealed class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        BaseUrl = "http://localhost:8096";
        TMDbApiKey = "";
    }

    /// <summary>
    /// Jellyfin server base URL.
    /// </summary>
    public string BaseUrl { get; set; }

    public string TMDbApiKey { get; set; }
}
