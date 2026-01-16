using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Consistart.Extensions;

[ExcludeFromCodeCoverage]
public static class BaseItemExtensions
{
    public static bool TryGetProviderIdAsInt(
        this BaseItem item,
        MetadataProvider provider,
        out int id
    )
    {
        var idString = item.GetProviderId(provider);
        if (int.TryParse(idString, out id) && id > 0)
        {
            return true;
        }

        id = default;
        return false;
    }

    public static string GetPreferredMetadataLanguageSubtag(this BaseItem item)
    {
        try
        {
            var language = item.GetPreferredMetadataLanguage();
            if (string.IsNullOrEmpty(language))
            {
                return "en";
            }

            return language.Split('-')[0];
        }
        catch (NullReferenceException)
        {
            return "en";
        }
    }
}
