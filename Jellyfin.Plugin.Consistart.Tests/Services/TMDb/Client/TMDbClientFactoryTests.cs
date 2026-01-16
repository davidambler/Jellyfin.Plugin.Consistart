using Jellyfin.Plugin.Consistart.Services.Configuration;
using Jellyfin.Plugin.Consistart.Services.TMDb.Client;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.TMDb.Client;

public class TMDbClientFactoryTests
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly TMDbClientFactory _sut;

    public TMDbClientFactoryTests()
    {
        _configurationProvider = Substitute.For<IConfigurationProvider>();
        _sut = new TMDbClientFactory(_configurationProvider);
    }

    [Fact]
    public void CreateClient_returns_client_adapter_when_api_key_is_valid()
    {
        const string ApiKey = "valid-api-key-123";
        _configurationProvider.TMDbApiKey.Returns(ApiKey);

        var result = _sut.CreateClient();

        Assert.NotNull(result);
    }

    [Fact]
    public void CreateClient_throws_invalid_operation_exception_when_api_key_is_null()
    {
        _configurationProvider.TMDbApiKey.Returns((string?)null);

        Assert.Throws<InvalidOperationException>(() => _sut.CreateClient());
    }

    [Fact]
    public void CreateClient_throws_invalid_operation_exception_when_api_key_is_empty()
    {
        _configurationProvider.TMDbApiKey.Returns(string.Empty);

        Assert.Throws<InvalidOperationException>(() => _sut.CreateClient());
    }

    [Fact]
    public void CreateClient_throws_invalid_operation_exception_when_api_key_is_whitespace()
    {
        _configurationProvider.TMDbApiKey.Returns("   ");

        Assert.Throws<InvalidOperationException>(() => _sut.CreateClient());
    }

    [Fact]
    public void CreateClient_creates_new_client_instance_on_each_call()
    {
        const string ApiKey = "valid-api-key-123";
        _configurationProvider.TMDbApiKey.Returns(ApiKey);

        var result1 = _sut.CreateClient();
        var result2 = _sut.CreateClient();

        Assert.NotSame(result1, result2);
    }
}
