using System.Text.Json.Serialization;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;

namespace Jellyfin.Plugin.Consistart.Services.Rendering;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PosterRenderRequest), "poster")]
[JsonDerivedType(typeof(ThumbnailRenderRequest), "thumbnail")]
[JsonDerivedType(typeof(SeasonPosterRenderRequest), "seasonPoster")]
[JsonDerivedType(typeof(EpisodeThumbnailRenderRequest), "episodeThumbnail")]
public interface IRenderRequest { }
