using System.Reflection;
using SixLabors.Fonts;

namespace Jellyfin.Plugin.Consistart.Infrastructure;

/// <summary>
/// Provides access to embedded font resources using SixLabors.Fonts.
/// </summary>
internal sealed class FontProvider : IFontProvider
{
    private readonly FontCollection _fontCollection;
    private readonly Dictionary<string, FontFamily> _fontCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public FontProvider()
    {
        _fontCollection = new FontCollection();
        LoadEmbeddedFonts();
    }

    public FontFamily GetFont(string fontName)
    {
        if (_fontCache.TryGetValue(fontName, out var cachedFont))
            return cachedFont;

        throw new InvalidOperationException($"Font '{fontName}' is not loaded.");
    }

    private void LoadEmbeddedFonts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (
            var resourceName in resourceNames.Where(r =>
                r.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
                || r.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                continue;

            var fontFamily = _fontCollection.Add(stream);

            // Extract font name from resource name (e.g., "Jellyfin.Plugin.Consistart.Fonts.ColusRegular.otf" -> "ColusRegular")
            var fontFileName = resourceName.Split('.')[^2]; // Get the second-to-last segment
            _fontCache[fontFileName] = fontFamily;
        }
    }
}
