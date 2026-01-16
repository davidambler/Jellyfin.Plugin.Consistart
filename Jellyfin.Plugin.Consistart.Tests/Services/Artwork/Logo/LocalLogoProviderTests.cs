using Jellyfin.Plugin.Consistart.Infrastructure;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork.Logo;

public class LocalLogoProviderTests
{
    [Fact]
    public async Task TryGetLocalLogoAsync_when_path_exists_returns_local_logo_source()
    {
        var pathReader = Substitute.For<IItemImagePathReader>();
        var provider = new LocalLogoProvider(pathReader);
        var item = new Movie();

        pathReader.TryGetImagePath(item, ImageType.Logo).Returns("/path/logo.png");

        var result = await provider.TryGetLocalLogoAsync(item);

        Assert.NotNull(result);
        Assert.Equal(LogoSourceKind.Local, result!.Kind);
        Assert.Equal("/path/logo.png", result.FilePath);
        Assert.Equal(0, result.Width);
        Assert.Equal(0, result.Height);
        Assert.Null(result.Language);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryGetLocalLogoAsync_when_no_path_returns_null(string? path)
    {
        var pathReader = Substitute.For<IItemImagePathReader>();
        var provider = new LocalLogoProvider(pathReader);
        var item = new Movie();

        pathReader.TryGetImagePath(item, ImageType.Logo).Returns(path);

        var result = await provider.TryGetLocalLogoAsync(item);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetLocalLogoAsync_calls_path_reader_with_logo_image_type()
    {
        var pathReader = Substitute.For<IItemImagePathReader>();
        var provider = new LocalLogoProvider(pathReader);
        var item = new Movie();

        pathReader.TryGetImagePath(item, ImageType.Logo).Returns((string?)null);

        await provider.TryGetLocalLogoAsync(item);

        pathReader.Received(1).TryGetImagePath(item, ImageType.Logo);
    }
}
