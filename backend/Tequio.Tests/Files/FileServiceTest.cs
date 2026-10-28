using Microsoft.Extensions.Logging;
using Moq;
using Tequio.Domain.Constants;
using Tequio.Domain.Services;
using Xunit;

namespace Tequio.Tests.Files;

/// <summary>Tests the implemented size limit and storage delegation using in-memory streams.</summary>
public sealed class FileServiceTest
{
    private const int MaxFileSize = 5 * 1024 * 1024;
    private const string FileName = "profile.jpg";
    private readonly Mock<IStorageService> _storage = new(MockBehavior.Strict);
    private readonly Mock<ILogger<FileService>> _logger = new();

    [Theory]
    [InlineData(0)] // Empty files are rejected by FilesController, not FileService.
    [InlineData(1)]
    [InlineData(MaxFileSize - 1)]
    [InlineData(MaxFileSize)]
    public async Task TestUploadImageWithinLimitShouldDelegateSameStreamAndReturnUrl(int size)
    {
        using var stream = new MemoryStream(new byte[size]);
        const string url = "https://images.example.test/profile.jpg";
        _storage.Setup(storage => storage.UploadFileAsync(stream, FileName)).ReturnsAsync(url);

        var result = await CreateService().UploadImageAsync(stream, FileName);

        Assert.Equal(url, result);
        Assert.True(stream.CanRead);
        _storage.Verify(storage => storage.UploadFileAsync(stream, FileName), Times.Once);
        _storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(MaxFileSize + 1)]
    [InlineData(MaxFileSize * 2)]
    public async Task TestUploadImageAboveLimitShouldRejectBeforeStorage(int size)
    {
        using var stream = new MemoryStream(new byte[size]);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().UploadImageAsync(stream, FileName));

        Assert.Equal(ErrorMessages.FileTooLarge, error.Message);
        Assert.True(stream.CanRead);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestUploadImageWithStorageFailureShouldPropagateSameError()
    {
        using var stream = new MemoryStream(new byte[1]);
        var expected = new InvalidOperationException("Storage upload failed.");
        _storage.Setup(storage => storage.UploadFileAsync(stream, FileName)).ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().UploadImageAsync(stream, FileName));

        Assert.Same(expected, error);
        Assert.True(stream.CanRead);
        _storage.Verify(storage => storage.UploadFileAsync(stream, FileName), Times.Once);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestUploadImageWithoutLengthSupportShouldPropagateStreamErrorBeforeStorage()
    {
        var stream = new Mock<Stream>(MockBehavior.Strict);
        var expected = new NotSupportedException("Length is unavailable.");
        stream.SetupGet(value => value.Length).Throws(expected);

        var error = await Assert.ThrowsAsync<NotSupportedException>(
            () => CreateService().UploadImageAsync(stream.Object, FileName));

        Assert.Same(expected, error);
        stream.VerifyGet(value => value.Length, Times.Once);
        stream.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    private FileService CreateService() => new(_storage.Object, _logger.Object);
}
