using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Artwork;
using Jellyfin.Plugin.Consistart.Services.Artwork.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Artwork.Poster;
using Jellyfin.Plugin.Consistart.Services.Artwork.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Artwork.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.Configuration;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.EpisodeThumbnail;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.Rendering.SeasonPoster;
using Jellyfin.Plugin.Consistart.Services.Rendering.Thumbnail;
using Jellyfin.Plugin.Consistart.Services.TMDb.Images;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Consistart;

[ExcludeFromCodeCoverage]
public sealed class PluginServiceRegister : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost host)
    {
        RegisterBaseServices(services);
        RegisterTMDbServices(services);
        RegisterArtworkServices(services);
        RegisterRenderingServices(services);
    }

    private static void RegisterBaseServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
        services.AddSingleton<ITokenProtectionService, TokenProtectionService>();
        services.AddSingleton<ILocalFileReader, LocalFileReader>();
        services.AddSingleton<IItemImagePathReader, ItemImagePathReader>();
        services.AddSingleton<IFontProvider, FontProvider>();
    }

    private static void RegisterTMDbServices(IServiceCollection services)
    {
        services.AddSingleton<
            Services.TMDb.Client.ITMDbClientAdapter,
            Services.TMDb.Client.TMDbClientAdapter
        >();
        services.AddSingleton<
            Services.TMDb.Client.ITMDbClientFactory,
            Services.TMDb.Client.TMDbClientFactory
        >();
        services.AddSingleton<ITMDbImagesClient, TMDbImagesClient>();
    }

    private static void RegisterArtworkServices(IServiceCollection services)
    {
        services.AddSingleton<IArtworkCandidateService, ArtworkCandidateService>();

        // Poster
        services.AddSingleton<IArtworkImageProvider<PosterSource>, PosterSourceProvider>();
        services.AddSingleton<IArtworkImageSelector<PosterSource>, PosterSelector>();
        services.AddSingleton<IArtworkCandidateGenerator, PosterCandidateGenerator>();

        // Logo
        services.AddSingleton<ILocalLogoProvider, LocalLogoProvider>();
        services.AddSingleton<IArtworkImageProvider<LogoSource>, LogoSourceProvider>();
        services.AddSingleton<IArtworkImageSelector<LogoSource>, LogoSelector>();
        services.AddSingleton<IArtworkCandidateGenerator, LogoCandidateGenerator>();

        // Season Poster
        services.AddSingleton<
            IArtworkImageProvider<SeasonPosterSource>,
            SeasonPosterSourceProvider
        >();
        services.AddSingleton<IArtworkImageSelector<SeasonPosterSource>, SeasonPosterSelector>();
        services.AddSingleton<IArtworkCandidateGenerator, SeasonPosterCandidateGenerator>();

        // Thumbnail
        services.AddSingleton<IArtworkImageProvider<ThumbnailSource>, ThumbnailSourceProvider>();
        services.AddSingleton<IArtworkImageSelector<ThumbnailSource>, ThumbnailSelector>();
        services.AddSingleton<IArtworkCandidateGenerator, ThumbnailCandidateGenerator>();

        // Episode Thumbnail
        services.AddSingleton<
            IArtworkImageProvider<EpisodeThumbnailSource>,
            EpisodeThumbnailSourceProvider
        >();
        services.AddSingleton<
            IArtworkImageSelector<EpisodeThumbnailSource>,
            EpisodeThumbnailSelector
        >();
        services.AddSingleton<IArtworkCandidateGenerator, EpisodeThumbnailCandidateGenerator>();
    }

    private static void RegisterRenderingServices(IServiceCollection services)
    {
        // Poster
        services.AddSingleton<
            IRenderRequestBuilder<PosterRenderRequest>,
            RenderRequestBuilder<PosterRenderRequest>
        >();
        services.AddSingleton<IRenderService<PosterRenderRequest>, PosterRenderService>();
        services.AddSingleton<IPosterRenderer, PosterRenderer>();

        // Season poster
        services.AddSingleton<
            IRenderRequestBuilder<SeasonPosterRenderRequest>,
            RenderRequestBuilder<SeasonPosterRenderRequest>
        >();
        services.AddSingleton<
            IRenderService<SeasonPosterRenderRequest>,
            SeasonPosterRenderService
        >();
        services.AddSingleton<ISeasonPosterRenderer, SeasonPosterRenderer>();

        // Thumbnail
        services.AddSingleton<
            IRenderRequestBuilder<ThumbnailRenderRequest>,
            RenderRequestBuilder<ThumbnailRenderRequest>
        >();
        services.AddSingleton<IRenderService<ThumbnailRenderRequest>, ThumbnailRenderService>();
        services.AddSingleton<IThumbnailRenderer, ThumbnailRenderer>();

        // Episode Thumbnail
        services.AddSingleton<
            IRenderRequestBuilder<EpisodeThumbnailRenderRequest>,
            RenderRequestBuilder<EpisodeThumbnailRenderRequest>
        >();
        services.AddSingleton<
            IRenderService<EpisodeThumbnailRenderRequest>,
            EpisodeThumbnailRenderService
        >();
        services.AddSingleton<IEpisodeThumbnailRenderer, EpisodeThumbnailRenderer>();
    }
}
