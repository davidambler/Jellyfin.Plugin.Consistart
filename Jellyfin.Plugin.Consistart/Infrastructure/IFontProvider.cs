using SixLabors.Fonts;

namespace Jellyfin.Plugin.Consistart.Infrastructure;

/// <summary>
/// Provides access to embedded font resources.
/// </summary>
internal interface IFontProvider
{
    /// <summary>
    /// Gets a font family from embedded resources.
    /// </summary>
    /// <param name="fontName">The name of the embedded font (e.g., "ColusRegular").</param>
    /// <returns>The font family.</returns>
    FontFamily GetFont(string fontName);
}
