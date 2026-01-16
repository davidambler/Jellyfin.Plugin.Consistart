using Jellyfin.Plugin.Consistart.Infrastructure;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Infrastructure;

public class LocalFileReaderTests
{
    private readonly LocalFileReader _reader;
    private readonly string _tempDirectory;

    public LocalFileReaderTests()
    {
        _reader = new LocalFileReader();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    private void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Successful Read Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_valid_file_returns_file_contents()
    {
        var fileName = Path.Combine(_tempDirectory, "test.txt");
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(fileName, expectedBytes);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(expectedBytes, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_large_file_returns_all_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "large.bin");
        var largeData = new byte[1024 * 1024]; // 1 MB
        Random.Shared.NextBytes(largeData);
        await File.WriteAllBytesAsync(fileName, largeData);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(largeData.Length, result.Length);
            Assert.Equal(largeData, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_empty_file_returns_empty_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "empty.txt");
        await File.WriteAllBytesAsync(fileName, Array.Empty<byte>());

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_text_file_preserves_content()
    {
        var fileName = Path.Combine(_tempDirectory, "text.txt");
        var content = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_binary_file_returns_exact_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "binary.bin");
        var binaryData = new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0x80, 0x7F };
        await File.WriteAllBytesAsync(fileName, binaryData);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(binaryData, result);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Null/Whitespace Path Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_null_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_empty_string_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_whitespace_only_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_tab_only_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync("\t\t\t");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_newline_only_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync("\n\n");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region File Not Found Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_nonexistent_file_returns_null()
    {
        var fileName = Path.Combine(_tempDirectory, "nonexistent.txt");

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.Null(result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_invalid_path_returns_null()
    {
        // Act
        var result = await _reader.TryReadAllBytesAsync(
            "C:\\invalid\\path\\that\\does\\not\\exist\\file.txt"
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_directory_path_returns_null()
    {
        var dirPath = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(dirPath);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(dirPath);

            Assert.Null(result);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region IOException Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_file_locked_returns_null()
    {
        var fileName = Path.Combine(_tempDirectory, "locked.txt");
        await File.WriteAllTextAsync(fileName, "content");

        try
        {
            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act - file is locked by another stream
                var result = await _reader.TryReadAllBytesAsync(fileName);

                Assert.Null(result);
            }
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_deleted_file_during_read_returns_null()
    {
        var fileName = Path.Combine(_tempDirectory, "willbedeleted.txt");
        await File.WriteAllTextAsync(fileName, "content");

        try
        {
            // Delete the file immediately (simulating a race condition)
            File.Delete(fileName);

            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.Null(result);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_cancelled_token_throws_task_cancelled_exception()
    {
        var fileName = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(fileName, "content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert - File.ReadAllBytesAsync throws TaskCanceledException
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                _reader.TryReadAllBytesAsync(fileName, cts.Token)
            );
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_valid_token_succeeds()
    {
        var fileName = Path.Combine(_tempDirectory, "test.txt");
        var content = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(fileName, content);
        var cts = new CancellationTokenSource();

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName, cts.Token);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            cts.Dispose();
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_default_token_succeeds()
    {
        var fileName = Path.Combine(_tempDirectory, "test.txt");
        var content = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName, default);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task TryReadAllBytesAsync_with_special_characters_in_filename_returns_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "file with spaces & special (chars).txt");
        var content = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_unicode_filename_returns_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "文件名.txt");
        var content = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_multiple_calls_same_file_returns_same_content()
    {
        var fileName = Path.Combine(_tempDirectory, "test.txt");
        var content = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result1 = await _reader.TryReadAllBytesAsync(fileName);
            var result2 = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1, result2);
            Assert.Equal(content, result1);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_with_relative_path_returns_bytes()
    {
        var fileName = Path.Combine(_tempDirectory, "relative_test.txt");
        var content = new byte[] { 10, 20, 30 };
        await File.WriteAllBytesAsync(fileName, content);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            Assert.Equal(content, result);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task TryReadAllBytesAsync_preserves_byte_order()
    {
        var fileName = Path.Combine(_tempDirectory, "byteorder.bin");
        var original = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0xFE, 0xFF };
        await File.WriteAllBytesAsync(fileName, original);

        try
        {
            var result = await _reader.TryReadAllBytesAsync(fileName);

            Assert.NotNull(result);
            for (var i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], result[i]);
            }
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion
}
