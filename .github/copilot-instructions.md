# Consistart Plugin - AI Agent Guidelines

## Project Overview
A Jellyfin plugin providing custom artwork rendering (posters, season posters, logos, thumbnails, episode thumbnails) with TMDb integration. Uses image transformation, token-protected rendering URLs, and abstract provider patterns to deliver customized media artwork.

## Architecture & Data Flow

### Core Layers
1. **Providers** (Jellyfin.Plugin.Consistart/Providers/): `ConsistartProvider<T>` abstract base class implementing Jellyfin's `IRemoteImageProvider`. Concrete providers (Poster/Logo/Thumbnail/SeasonPoster/EpisodeThumbnail) delegate artwork candidate retrieval to `IArtworkCandidateService`.

2. **Artwork Service** (Jellyfin.Plugin.Consistart/Services/Artwork/):
   - `ArtworkCandidateService`: Single entry point; selects appropriate `IArtworkCandidateGenerator` based on media item type + image type
   - Per-type subdirectories (Logo/, Poster/, SeasonPoster/, Thumbnail/, EpisodeThumbnail/) contain:
     - `I{Type}CandidateGenerator`: Queries TMDb for artwork candidates
     - `I{Type}ImageProvider`: Wraps external image sources (TMDb, local filesystem)
     - `I{Type}Selector`: Filters and ranks candidates by preference
   - `IArtworkImageSource` types specify source location (local path or TMDb file path)

3. **Rendering Service** (Jellyfin.Plugin.Consistart/Services/Rendering/):
   - `IRenderService<T>`: Generic interface rendering specific artwork types (Poster/SeasonPoster/Thumbnail/EpisodeThumbnail)
   - `RenderRequestBuilder<T>`: Serializes render requests to JSON with `JsonSerializerDefaults.Web` (camelCase), encrypts via `ITokenProtectionService`, generates safe URLs (`/consistart/render?token=...`)
   - `RenderController`: Receives encrypted token, decrypts, deserializes polymorphically via type discriminator, routes to appropriate `IRenderService<T>`

4. **Infrastructure**:
   - `ITokenProtectionService`: ASP.NET Core Data Protection API wrapper; encrypts/decrypts `IRenderRequest` objects
   - `ILocalFileReader`: File system abstraction for loading fonts/images
   - `IFontProvider`: Manages embedded fonts (ColusRegular.otf)
   - `IItemImagePathReader`: Reads Jellyfin's local item image paths

5. **TMDb Integration** (Services/TMDb/):
   - `ITMDbClientAdapter`: Wrapper around TMDbLib client; handles authentication & caching
   - `ITMDbImagesClient`: Retrieves and caches image bytes from TMDb API

### Key Patterns
- **Strategy Pattern**: Multiple `IArtworkCandidateGenerator` implementations (one per artwork type); `ArtworkCandidateService` dispatches via first matching `CanHandle()`
- **Generic Services**: `IRenderService<T>` and `RenderRequestBuilder<T>` provide type-safe, extensible rendering pipeline
- **URL Token Protection**: All render parameters encrypted in token query param—never exposed in URL (security + cleanliness)
- **Polymorphic Serialization**: `IRenderRequest` marker interface + JSON type discriminator enables `RenderController` to deserialize arbitrary request types
- **DI at Plugin Startup**: `PluginServiceRegister` (marked `[ExcludeFromCodeCoverage]`) registers all services in `IServiceCollection`; Jellyfin invokes via `IPluginServiceRegistrator`

## Critical Developer Workflows

### Build & Deployment
- **IDE Build**: Use VS Code task `build-and-copy` (bundles build + plugin directory copy) or run individual tasks: `shell: build`
- **Manual Build**: `dotnet build [solution]` compiles with Jellyfin framework references marked `ExcludeAssets=runtime` (prevents assembly conflicts)
- **Local Testing Setup**:
  1. Edit Directory.Build.local.props to set `<JellyfinPluginsDir>` to your Jellyfin plugins directory
  2. Build Debug config; post-build target auto-copies DLL
  3. Restart Jellyfin to load updated plugin
- **Release Build**: `dotnet build --configuration Release` produces optimized binaries

### Testing
- `dotnet test` runs XUnit tests with NSubstitute mocking; coverlet collects coverage
- **Request Records**: Immutable record types used for render requests (e.g., `PosterRenderRequest(mediaKind, tmdbId, posterFilePath, logo, preset?)`)
- **Mocking Pattern**: `Substitute.For<IInterface>()` captures arguments for verification
- **Test Organization**: Mirror Providers/ and Services/ folder structure in Jellyfin.Plugin.Consistart.Tests/
- **Test Naming**: Use descriptive method names like `GetImages_calls_candidate_service_with_primary_image_type()`
- **Logging in Tests**: Use `NullLogger<T>` for dependencies unless testing logging behavior specifically

### Code Style
- Run `dotnet format --verbosity diagnostic` to enforce `.editorconfig` rules (requires .NET 9+)
- **Preferred Patterns**:
  - Sealed classes: `internal sealed class X` (prevents accidental inheritance; improves performance)
  - File-scoped namespaces: `namespace X;` (cleaner, requires C# 10+)
  - Nullable reference types enabled; use `T?` to indicate optional values
  - Record constructors for DTOs (immutability + concise syntax)
  - Async methods with `Async` suffix (e.g., `GetCandidatesAsync`)
- **Code Coverage Exclusion**: Mark plugin bootstrap (`Plugin.cs`, `PluginServiceRegister`, integration test fixtures) with `[ExcludeFromCodeCoverage]`

## Project-Specific Conventions

### Render Request Types
Each artwork type has its own `RenderRequest` record in Jellyfin.Plugin.Consistart/Services/Rendering/{Type}/:
- `PosterRenderRequest(mediaKind, tmdbId, posterFilePath, logo, preset?)`
- `ThumbnailRenderRequest(...)`
- `SeasonPosterRenderRequest(...)`

All implement `IRenderRequest` marker interface for polymorphic JSON serialization in RenderController.

### Artwork Candidate Generation
To add new artwork type:
1. Create `Services/Artwork/{NewType}/I{NewType}CandidateGenerator.cs` implementing `IArtworkCandidateGenerator`
2. Implement `CanHandle(BaseItem, ImageType)` to match your media + image type filter
3. Implement `GetCandidatesAsync()` to query TMDb and return `IReadOnlyList<ArtworkCandidateDto>`
4. Register in `PluginServiceRegister.RegisterArtworkServices()`: `services.AddSingleton<I{NewType}CandidateGenerator, {NewType}CandidateGenerator>()`

### Render Service Implementation
New render service must implement `IRenderService<TRequest>`:
- `async Task<RenderedImage> RenderAsync(TRequest request, CancellationToken)`
- Register in `PluginServiceRegister.RegisterRenderingServices()` and inject into `RenderController`
- Use `RenderUtilities` for common image operations (overlays, drop shadows, safe zones)

### Integration Points
- **Jellyfin Framework**: `IRemoteImageProvider`, `BaseItem`, `ImageType`, `IPluginServiceRegistrator` from `MediaBrowser.*` packages
- **External APIs**: TMDb client (`TMDbLib` NuGet) wrapped by `ITMDbClientAdapter`; TMDb API key configured via `IConfigurationProvider`
- **Image Library**: SixLabors.ImageSharp for all image transforms; no direct GDI+ calls

## File Locations for Key Patterns

| Pattern | Location |
|---------|----------|
| Provider base | Jellyfin.Plugin.Consistart/Providers/ConsistartProvider.cs |
| Artwork candidates | Jellyfin.Plugin.Consistart/Services/Artwork/ |
| Rendering | Jellyfin.Plugin.Consistart/Services/Rendering/ |
| DI setup | Jellyfin.Plugin.Consistart/PluginServiceRegister.cs |
| Token protection | Jellyfin.Plugin.Consistart/Services/TokenProtection/ |
| Tests | Jellyfin.Plugin.Consistart.Tests/ (mirror Providers/ and Services/ structure) |

## Important Caveats
- Render requests serialized with `JsonSerializerDefaults.Web` (camelCase, case-insensitive on deserialize)
- All classes in `Services/TokenProtection/` and internal renderers marked `internal sealed` - not part of public plugin API
- Jellyfin controller packages have `ExcludeAssets=runtime` to avoid runtime conflicts; framework provided by Jellyfin host
