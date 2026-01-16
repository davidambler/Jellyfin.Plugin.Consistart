<p align="center" style="margin-bottom: 40px;">
  <img src="logo.png" alt="Consistart Logo" width="900px">
</p>

A Jellyfin plugin that provides opinionated, consistent artwork rendering for your media library using [The Movie Database (TMDb)](https://www.themoviedb.org/) as the artwork source.

## What It Does

Consistart enhances your Jellyfin media library by generating custom artwork with a unified visual style across:

- **Movie & Series Posters**: Customized poster designs with integrated logos
- **Season Posters**: Consistent season artwork with embedded branding
- **Logos**: Clean, consistent logo overlays
- **Thumbnails**: Backdrop thumbnails for movies and series
- **Episode Thumbnails**: Custom episode preview images

The plugin applies opinionated design choices to create a cohesive look throughout your library, but **requires user curation** to achieve the best results. Not all automatically selected artwork will be perfect for every media item—you may need to refresh images or adjust selections to match your preferences.

## Key Features

- **TMDb Integration**: Leverages TMDb's extensive artwork database
- **Token-Protected Rendering**: Secure, encrypted render URLs prevent parameter tampering
- **Smart Artwork Selection**: Intelligent filtering and ranking of artwork candidates
- **On-Demand Rendering**: Images generated dynamically with caching

## Installation

### From Repository

1. Download the latest release zip from the releases page
2. Extract it to your Jellyfin plugins directory
3. Restart Jellyfin
4. Navigate to Dashboard → Plugins → Consistart to configure

### Building from Source

```bash
dotnet build Jellyfin.Plugin.Consistart.sln --configuration Release
```

The compiled plugin will be in `Jellyfin.Plugin.Consistart/bin/Release/net9.0/`

## Configuration

1. In Jellyfin, go to **Dashboard → Plugins → Consistart**
2. Enter your TMDb API key (required)

## Usage Notes

### User Curation Required

While Consistart automatically selects artwork based on intelligent heuristics, **manual curation is recommended**:

- Some media items may receive suboptimal artwork choices
- Refresh images for individual items if the automatic selection doesn't match your taste
- The plugin aims for consistency over perfection—you define what "perfect" means for your library

### Artwork Refresh

After installing or reconfiguring:

1. Navigate to a library or individual item
2. Click **More (...)** → **Identify**
3. Select **Replace All Images** to regenerate with Consistart

## Development

### Prerequisites

- .NET 9 SDK
- Jellyfin 10.10.0+ (for testing)

### Testing

```bash
dotnet test
```

### Code Style

```bash
dotnet format --verbosity diagnostic
```

## Project Structure

```
Jellyfin.Plugin.Consistart/
├── Providers/           # Jellyfin image provider implementations
├── Services/
│   ├── Artwork/         # Artwork candidate generation & selection
│   ├── Rendering/       # Image rendering services
│   ├── TMDb/            # TMDb API integration
│   └── TokenProtection/ # URL encryption/decryption
├── Controllers/         # ASP.NET Core controllers
└── wwwroot/             # Web UI assets

Jellyfin.Plugin.Consistart.Tests/
└── [mirrors main project structure]
```

## License

See [LICENSE](LICENSE) file for details.

## Contributing

Contributions welcome! Please ensure:
- All tests pass (`dotnet test`)
- Code follows project style (`dotnet format`)
- New features include unit tests

## Acknowledgments

- Artwork sourced from [The Movie Database (TMDb)](https://www.themoviedb.org/)
- Built for [Jellyfin](https://jellyfin.org/)
