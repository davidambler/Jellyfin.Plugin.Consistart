using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Consistart.Services.Artwork.Logo;
using Jellyfin.Plugin.Consistart.Services.Rendering;
using Jellyfin.Plugin.Consistart.Services.Rendering.Poster;
using Jellyfin.Plugin.Consistart.Services.TMDb;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Rendering;

public class RenderRequestBuilderTests
{
    private static readonly LogoSource TestLogo = new(
        Kind: LogoSourceKind.Local,
        FilePath: "/test/logo.png"
    );

    #region Basic Functionality Tests

    [Fact]
    public void BuildUrl_with_simple_request_returns_url_with_token()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("encrypted-token-123");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            12345,
            "/path/to/poster.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=encrypted-token-123", result);
    }

    [Fact]
    public void BuildUrl_calls_token_protection_with_serialized_request()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        byte[]? capturedBytes = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedBytes = callInfo.ArgAt<byte[]>(0);
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            54321,
            "/another/path.jpg",
            TestLogo,
            "preset1"
        );

        builder.BuildUrl(request);

        tokenProtection.Received(1).Protect(Arg.Any<byte[]>());
        Assert.NotNull(capturedBytes);

        var json = Encoding.UTF8.GetString(capturedBytes);
        var deserialized = JsonSerializer.Deserialize<PosterRenderRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );

        Assert.NotNull(deserialized);
        Assert.Equal(MediaKind.Movie, deserialized.MediaKind);
        Assert.Equal(54321, deserialized.TmdbId);
        Assert.Equal("/another/path.jpg", deserialized.PosterFilePath);
        Assert.Equal("preset1", deserialized.Preset);
    }

    [Fact]
    public void BuildUrl_serializes_with_web_defaults()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                var bytes = callInfo.ArgAt<byte[]>(0);
                capturedJson = Encoding.UTF8.GetString(bytes);
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.TvShow,
            999,
            "/file.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        // Web defaults use camelCase
        Assert.Contains("\"mediaKind\":", capturedJson);
        Assert.Contains("\"tmdbId\":999", capturedJson);
        Assert.Contains("\"posterFilePath\":\"/file.jpg\"", capturedJson);
    }

    #endregion

    #region Request Variations Tests

    [Fact]
    public void BuildUrl_with_null_preset_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(MediaKind.Movie, 123, "/path.jpg", TestLogo, null);

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        var deserialized = JsonSerializer.Deserialize<PosterRenderRequest>(
            capturedJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Preset);
    }

    [Fact]
    public void BuildUrl_with_non_null_preset_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.TvShow,
            456,
            "/poster.png",
            TestLogo,
            "custom"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        var deserialized = JsonSerializer.Deserialize<PosterRenderRequest>(
            capturedJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );
        Assert.NotNull(deserialized);
        Assert.Equal("custom", deserialized.Preset);
    }

    [Fact]
    public void BuildUrl_with_movie_media_kind_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            789,
            "/movie.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains("\"mediaKind\":0", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_tv_show_media_kind_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.TvShow,
            321,
            "/show.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains("\"mediaKind\":1", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_special_characters_in_path_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            111,
            "/path/with spaces & symbols/poster.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=token", result);
        tokenProtection.Received(1).Protect(Arg.Any<byte[]>());
    }

    #endregion

    #region Token Encoding Tests

    [Fact]
    public void BuildUrl_with_different_tokens_returns_different_urls()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("first-token", "second-token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "preset"
        );

        var result1 = builder.BuildUrl(request);
        var result2 = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=first-token", result1);
        Assert.Equal("/consistart/render?token=second-token", result2);
    }

    [Fact]
    public void BuildUrl_with_token_containing_special_characters_preserves_token()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("token+with/special=chars");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=token+with/special=chars", result);
    }

    [Fact]
    public void BuildUrl_with_empty_token_returns_url_with_empty_token()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=", result);
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public void BuildUrl_called_multiple_times_with_same_request_produces_consistent_serialization()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        var capturedBytes = new List<byte[]>();
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedBytes.Add(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "preset"
        );

        builder.BuildUrl(request);
        builder.BuildUrl(request);

        Assert.Equal(2, capturedBytes.Count);
        Assert.Equal(capturedBytes[0], capturedBytes[1]);
    }

    [Fact]
    public void BuildUrl_called_with_different_requests_produces_different_serializations()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        var capturedBytes = new List<byte[]>();
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedBytes.Add(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request1 = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path1.jpg",
            TestLogo,
            "default"
        );
        var request2 = new PosterRenderRequest(
            MediaKind.Movie,
            456,
            "/path2.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request1);
        builder.BuildUrl(request2);

        Assert.Equal(2, capturedBytes.Count);
        Assert.NotEqual(capturedBytes[0], capturedBytes[1]);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void BuildUrl_with_zero_tmdb_id_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(MediaKind.Movie, 0, "/path.jpg", TestLogo, "default");

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains("\"tmdbId\":0", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_negative_tmdb_id_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            -1,
            "/path.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains("\"tmdbId\":-1", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_large_tmdb_id_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            int.MaxValue,
            "/path.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains($"\"tmdbId\":{int.MaxValue}", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_empty_string_path_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(MediaKind.Movie, 123, "", TestLogo, "default");

        var result = builder.BuildUrl(request);

        Assert.Equal("/consistart/render?token=token", result);
    }

    [Fact]
    public void BuildUrl_with_empty_string_preset_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(MediaKind.Movie, 123, "/path.jpg", TestLogo, "");

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        Assert.Contains("\"preset\":\"\"", capturedJson);
    }

    [Fact]
    public void BuildUrl_with_unicode_characters_in_path_serializes_correctly()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path/文件.jpg",
            TestLogo,
            "default"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        var deserialized = JsonSerializer.Deserialize<PosterRenderRequest>(
            capturedJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );
        Assert.NotNull(deserialized);
        Assert.Equal("/path/文件.jpg", deserialized.PosterFilePath);
    }

    #endregion

    #region URL Format Tests

    [Fact]
    public void BuildUrl_returns_relative_url_starting_with_slash()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.StartsWith("/", result);
    }

    [Fact]
    public void BuildUrl_returns_url_with_consistart_path()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Contains("/consistart/render", result);
    }

    [Fact]
    public void BuildUrl_returns_url_with_token_query_parameter()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        tokenProtection.Protect(Arg.Any<byte[]>()).Returns("my-token");

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "default"
        );

        var result = builder.BuildUrl(request);

        Assert.Contains("?token=my-token", result);
    }

    #endregion

    #region Serialization Consistency Tests

    [Fact]
    public void BuildUrl_produces_json_without_whitespace()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var request = new PosterRenderRequest(
            MediaKind.Movie,
            123,
            "/path.jpg",
            TestLogo,
            "preset"
        );

        builder.BuildUrl(request);

        Assert.NotNull(capturedJson);
        // WriteIndented = false means no extra whitespace
        Assert.DoesNotContain("\n", capturedJson);
        Assert.DoesNotContain("\r", capturedJson);
    }

    [Fact]
    public void BuildUrl_produces_deserializable_json()
    {
        var tokenProtection = Substitute.For<ITokenProtectionService>();
        string? capturedJson = null;
        tokenProtection
            .Protect(Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                capturedJson = Encoding.UTF8.GetString(callInfo.ArgAt<byte[]>(0));
                return "token";
            });

        var builder = new RenderRequestBuilder<PosterRenderRequest>(tokenProtection);
        var originalRequest = new PosterRenderRequest(
            MediaKind.TvShow,
            98765,
            "/some/file.png",
            TestLogo,
            "preset-xyz"
        );

        builder.BuildUrl(originalRequest);

        Assert.NotNull(capturedJson);
        var deserialized = JsonSerializer.Deserialize<PosterRenderRequest>(
            capturedJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );

        Assert.NotNull(deserialized);
        Assert.Equal(originalRequest.MediaKind, deserialized.MediaKind);
        Assert.Equal(originalRequest.TmdbId, deserialized.TmdbId);
        Assert.Equal(originalRequest.PosterFilePath, deserialized.PosterFilePath);
        Assert.Equal(originalRequest.Preset, deserialized.Preset);
    }

    #endregion
}
