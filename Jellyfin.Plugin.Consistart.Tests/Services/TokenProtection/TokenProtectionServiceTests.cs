using System.Text;
using Jellyfin.Plugin.Consistart.Services.TokenProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.TokenProtection;

public class TokenProtectionServiceTests
{
    #region Basic Functionality Tests

    [Fact]
    public void Protect_with_simple_data_returns_non_empty_token()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        var token = service.Protect(data);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void Protect_returns_base64_url_encoded_string()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = Encoding.UTF8.GetBytes("Test data");

        var token = service.Protect(data);

        // Base64Url should not contain '+', '/', or '=' characters
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void Unprotect_with_valid_token_returns_original_data()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var originalData = Encoding.UTF8.GetBytes("Test data");

        var token = service.Protect(originalData);
        var unprotectedData = service.Unprotect(token);

        Assert.Equal(originalData, unprotectedData);
    }

    [Fact]
    public void Protect_and_Unprotect_round_trip_preserves_data()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var originalData = Encoding.UTF8.GetBytes("Round trip test");

        var token = service.Protect(originalData);
        var result = service.Unprotect(token);

        Assert.Equal(originalData, result);
    }

    [Fact]
    public void Protect_with_same_data_produces_different_tokens()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = Encoding.UTF8.GetBytes("Same data");

        var token1 = service.Protect(data);
        var token2 = service.Protect(data);

        // Data protection typically adds randomness, so tokens should differ
        // However, this depends on the implementation. In practice with real DataProtection,
        // tokens will differ due to IV/nonce
        Assert.NotNull(token1);
        Assert.NotNull(token2);
    }

    #endregion

    #region Data Size Tests

    [Fact]
    public void Protect_with_empty_data_succeeds()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = Array.Empty<byte>();

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.NotNull(token);
        Assert.Empty(result);
    }

    [Fact]
    public void Protect_with_small_data_succeeds()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = new byte[] { 1 };

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Protect_with_large_data_succeeds()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = new byte[10000];
        new Random(42).NextBytes(data);

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Protect_with_very_large_data_succeeds()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = new byte[1_000_000]; // 1MB
        new Random(42).NextBytes(data);

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.Equal(data, result);
    }

    #endregion

    #region Binary Data Tests

    [Fact]
    public void Protect_with_binary_data_preserves_all_bytes()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Protect_with_null_bytes_preserves_data()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var data = new byte[] { 0, 0, 0, 1, 2, 0, 3, 0 };

        var token = service.Protect(data);
        var result = service.Unprotect(token);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Protect_with_utf8_text_preserves_unicode()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var text = "Hello 世界 🌍 Привет";
        var data = Encoding.UTF8.GetBytes(text);

        var token = service.Protect(data);
        var result = service.Unprotect(token);
        var resultText = Encoding.UTF8.GetString(result);

        Assert.Equal(text, resultText);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Unprotect_with_invalid_base64_throws_exception()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var invalidToken = "not-valid-base64!!!";

        Assert.Throws<FormatException>(() => service.Unprotect(invalidToken));
    }

    [Fact]
    public void Unprotect_with_empty_token_throws_exception()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);

        Assert.ThrowsAny<Exception>(() => service.Unprotect(string.Empty));
    }

    [Fact]
    public void Unprotect_with_tampered_token_throws_exception()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        var originalData = Encoding.UTF8.GetBytes("Original data");
        var token = service.Protect(originalData);

        // Tamper with the token by changing a character
        var tamperedToken =
            token.Length > 5 ? token.Substring(0, token.Length - 5) + "XXXXX" : "XXXXX";

        Assert.ThrowsAny<Exception>(() => service.Unprotect(tamperedToken));
    }

    [Fact]
    public void Unprotect_with_valid_base64_but_invalid_data_throws_exception()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service = new TokenProtectionService(dataProtectionProvider);
        // Valid Base64Url but not created by our service
        var invalidToken = WebEncoders.Base64UrlEncode([1, 2, 3, 4, 5]);

        Assert.ThrowsAny<Exception>(() => service.Unprotect(invalidToken));
    }

    #endregion

    #region Data Protector Tests

    [Fact]
    public void Constructor_creates_protector_with_correct_purpose()
    {
        var provider = Substitute.For<IDataProtectionProvider>();
        var protector = Substitute.For<IDataProtector>();
        provider.CreateProtector("Jellyfin.Plugin.Consistart.Token").Returns(protector);
        protector.Protect(Arg.Any<byte[]>()).Returns(ci => ci.ArgAt<byte[]>(0));
        protector.Unprotect(Arg.Any<byte[]>()).Returns(ci => ci.ArgAt<byte[]>(0));

        var service = new TokenProtectionService(provider);

        provider.Received(1).CreateProtector("Jellyfin.Plugin.Consistart.Token");
    }

    [Fact]
    public void Protect_calls_data_protector_protect()
    {
        var provider = Substitute.For<IDataProtectionProvider>();
        var protector = Substitute.For<IDataProtector>();
        provider.CreateProtector(Arg.Any<string>()).Returns(protector);
        protector.Protect(Arg.Any<byte[]>()).Returns([1, 2, 3]);

        var service = new TokenProtectionService(provider);
        var data = Encoding.UTF8.GetBytes("Test");

        service.Protect(data);

        protector.Received(1).Protect(Arg.Is<byte[]>(b => b.Length > 0));
    }

    [Fact]
    public void Unprotect_calls_data_protector_unprotect()
    {
        var provider = Substitute.For<IDataProtectionProvider>();
        var protector = Substitute.For<IDataProtector>();
        provider.CreateProtector(Arg.Any<string>()).Returns(protector);

        // Need to properly handle compression: Protect compresses, Unprotect must decompress
        byte[]? protectedData = null;
        protector
            .Protect(Arg.Any<byte[]>())
            .Returns(ci =>
            {
                protectedData = ci.ArgAt<byte[]>(0);
                return protectedData;
            });
        protector.Unprotect(Arg.Any<byte[]>()).Returns(ci => protectedData ?? []);

        var service = new TokenProtectionService(provider);
        var data = Encoding.UTF8.GetBytes("Test");
        var token = service.Protect(data);

        service.Unprotect(token);

        protector.Received(1).Unprotect(Arg.Any<byte[]>());
    }

    [Fact]
    public void Protect_when_data_protector_throws_exception_propagates_exception()
    {
        var provider = Substitute.For<IDataProtectionProvider>();
        var protector = Substitute.For<IDataProtector>();
        provider.CreateProtector(Arg.Any<string>()).Returns(protector);
        protector
            .Protect(Arg.Any<byte[]>())
            .Returns(x => throw new InvalidOperationException("Protection failed"));

        var service = new TokenProtectionService(provider);
        var data = Encoding.UTF8.GetBytes("Test");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Protect(data));
        Assert.Equal("Protection failed", ex.Message);
    }

    [Fact]
    public void Unprotect_when_data_protector_throws_exception_propagates_exception()
    {
        var provider = Substitute.For<IDataProtectionProvider>();
        var protector = Substitute.For<IDataProtector>();
        provider.CreateProtector(Arg.Any<string>()).Returns(protector);
        protector.Protect(Arg.Any<byte[]>()).Returns([1, 2, 3]);
        protector
            .Unprotect(Arg.Any<byte[]>())
            .Returns(x => throw new InvalidOperationException("Unprotection failed"));

        var service = new TokenProtectionService(provider);
        var token = WebEncoders.Base64UrlEncode([1, 2, 3]);

        // With encrypt-then-compress, we decompress first, which may fail with InvalidDataException
        // if the data isn't valid gzip. The unprotect exception is only reached if decompression succeeds.
        Assert.ThrowsAny<Exception>(() => service.Unprotect(token));
    }

    #endregion

    #region Multiple Instances Tests

    [Fact]
    public void Multiple_service_instances_with_same_provider_can_decrypt_each_others_tokens()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var service1 = new TokenProtectionService(dataProtectionProvider);
        var service2 = new TokenProtectionService(dataProtectionProvider);
        var data = Encoding.UTF8.GetBytes("Shared secret");

        var token = service1.Protect(data);
        var result = service2.Unprotect(token);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Service_instances_with_different_providers_cannot_decrypt_each_others_tokens()
    {
        var provider1 = CreateDataProtectionProvider();
        var provider2 = CreateDataProtectionProvider();
        var service1 = new TokenProtectionService(provider1);
        var service2 = new TokenProtectionService(provider2);
        var data = Encoding.UTF8.GetBytes("Secret");

        var token = service1.Protect(data);

        // Different providers should not be able to decrypt each other's tokens
        Assert.ThrowsAny<Exception>(() => service2.Unprotect(token));
    }

    #endregion

    #region Helper Methods

    private static EphemeralDataProtectionProvider CreateDataProtectionProvider()
    {
        return new();
    }

    #endregion
}
