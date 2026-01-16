using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Consistart;

[ExcludeFromCodeCoverage]
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Consistart";
    public override Guid Id => Guid.Parse("44cec167-6151-4faa-ba99-73bfe4cce9b5");
    public static Plugin Instance { get; private set; } = null!;

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages() =>
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.wwwroot.configuration.html",
                    GetType().Namespace
                ),
            },
        ];
}
