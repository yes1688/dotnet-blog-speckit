using BlogSystem.Core.Interfaces;
using BlogSystem.Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// ImageService 單元測試
/// </summary>
public class ImageServiceTests
{
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly string _testUploadsPath;
    private readonly IImageService _service;

    public ImageServiceTests()
    {
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _testUploadsPath = Path.Combine(Path.GetTempPath(), "test-uploads");

        // 設定測試上傳路徑
        _mockWebHostEnvironment
            .Setup(env => env.WebRootPath)
            .Returns(Path.GetTempPath());

        // 確保測試目錄存在
        if (!Directory.Exists(_testUploadsPath))
        {
            Directory.CreateDirectory(_testUploadsPath);
        }

        _service = new ImageService(_mockWebHostEnvironment.Object);
    }

    #region UploadImageAsync Tests

    [Fact]
    public async Task UploadImageAsync_ShouldReturnRelativePath_WhenValidImageUploaded()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var content = "fake image content";
        var file = CreateMockFormFile(fileName, content, "image/jpeg");

        // Act
        var result = await _service.UploadImageAsync(file);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("/uploads/");
        result.Should().EndWith(".jpg");
        result.Should().NotContain(fileName); // 應該使用 GUID 重命名
    }

    [Fact]
    public async Task UploadImageAsync_ShouldGenerateUniqueFileName_UsingGuid()
    {
        // Arrange
        var file1 = CreateMockFormFile("image1.jpg", "content1", "image/jpeg");
        var file2 = CreateMockFormFile("image1.jpg", "content2", "image/jpeg");

        // Act
        var result1 = await _service.UploadImageAsync(file1);
        var result2 = await _service.UploadImageAsync(file2);

        // Assert
        result1.Should().NotBe(result2);
        Path.GetFileNameWithoutExtension(result1).Should().NotBe(Path.GetFileNameWithoutExtension(result2));
    }

    [Fact]
    public async Task UploadImageAsync_ShouldPreserveOriginalFileExtension()
    {
        // Arrange
        var jpgFile = CreateMockFormFile("image.jpg", "content", "image/jpeg");
        var pngFile = CreateMockFormFile("image.png", "content", "image/png");
        var gifFile = CreateMockFormFile("image.gif", "content", "image/gif");

        // Act
        var jpgResult = await _service.UploadImageAsync(jpgFile);
        var pngResult = await _service.UploadImageAsync(pngFile);
        var gifResult = await _service.UploadImageAsync(gifFile);

        // Assert
        jpgResult.Should().EndWith(".jpg");
        pngResult.Should().EndWith(".png");
        gifResult.Should().EndWith(".gif");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldSaveFileToUploadsDirectory()
    {
        // Arrange
        var fileName = "test-image.png";
        var content = "fake png content";
        var file = CreateMockFormFile(fileName, content, "image/png");

        // Act
        var result = await _service.UploadImageAsync(file);

        // Assert
        var fullPath = Path.Combine(Path.GetTempPath(), result.TrimStart('/'));
        File.Exists(fullPath).Should().BeTrue();

        // Cleanup
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrowArgumentNullException_WhenFileIsNull()
    {
        // Act
        var act = async () => await _service.UploadImageAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrowArgumentException_WhenFileIsEmpty()
    {
        // Arrange
        var emptyFile = CreateMockFormFile("empty.jpg", "", "image/jpeg");

        // Act
        var act = async () => await _service.UploadImageAsync(emptyFile);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrowArgumentException_WhenFileExceedsSizeLimit()
    {
        // Arrange
        var largeContent = new string('x', 6 * 1024 * 1024); // 6MB
        var largeFile = CreateMockFormFile("large.jpg", largeContent, "image/jpeg");

        // Act
        var act = async () => await _service.UploadImageAsync(largeFile);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*5MB*");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrowArgumentException_WhenFileExtensionIsInvalid()
    {
        // Arrange
        var invalidFile = CreateMockFormFile("document.txt", "content", "text/plain");

        // Act
        var act = async () => await _service.UploadImageAsync(invalidFile);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*allowed*");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldAcceptJpegExtension()
    {
        // Arrange
        var jpegFile = CreateMockFormFile("image.jpeg", "content", "image/jpeg");

        // Act
        var result = await _service.UploadImageAsync(jpegFile);

        // Assert
        result.Should().EndWith(".jpeg");
    }

    #endregion

    #region ValidateImage Tests

    [Theory]
    [InlineData("image.jpg", "image/jpeg", true)]
    [InlineData("image.jpeg", "image/jpeg", true)]
    [InlineData("image.png", "image/png", true)]
    [InlineData("image.gif", "image/gif", true)]
    public void ValidateImage_ShouldReturnTrue_ForValidImageFormats(string fileName, string contentType, bool expected)
    {
        // Arrange
        var file = CreateMockFormFile(fileName, "valid content", contentType);

        // Act
        var result = _service.ValidateImage(file);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("document.txt", "text/plain")]
    [InlineData("script.js", "application/javascript")]
    [InlineData("app.exe", "application/x-msdownload")]
    [InlineData("data.json", "application/json")]
    [InlineData("style.css", "text/css")]
    public void ValidateImage_ShouldReturnFalse_ForInvalidFormats(string fileName, string contentType)
    {
        // Arrange
        var file = CreateMockFormFile(fileName, "content", contentType);

        // Act
        var result = _service.ValidateImage(file);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateImage_ShouldReturnFalse_WhenFileIsNull()
    {
        // Act
        var result = _service.ValidateImage(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateImage_ShouldReturnFalse_WhenFileIsEmpty()
    {
        // Arrange
        var emptyFile = CreateMockFormFile("empty.jpg", "", "image/jpeg");

        // Act
        var result = _service.ValidateImage(emptyFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateImage_ShouldReturnFalse_WhenFileSizeExceeds5MB()
    {
        // Arrange
        var largeContent = new string('x', 6 * 1024 * 1024); // 6MB
        var largeFile = CreateMockFormFile("large.jpg", largeContent, "image/jpeg");

        // Act
        var result = _service.ValidateImage(largeFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateImage_ShouldReturnTrue_WhenFileSizeIsExactly5MB()
    {
        // Arrange
        var exactContent = new string('x', 5 * 1024 * 1024); // Exactly 5MB
        var exactFile = CreateMockFormFile("exact.jpg", exactContent, "image/jpeg");

        // Act
        var result = _service.ValidateImage(exactFile);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateImage_ShouldBeCaseInsensitive_ForFileExtension()
    {
        // Arrange
        var upperFile = CreateMockFormFile("IMAGE.JPG", "content", "image/jpeg");
        var mixedFile = CreateMockFormFile("Image.JpG", "content", "image/jpeg");

        // Act
        var upperResult = _service.ValidateImage(upperFile);
        var mixedResult = _service.ValidateImage(mixedFile);

        // Assert
        upperResult.Should().BeTrue();
        mixedResult.Should().BeTrue();
    }

    #endregion

    #region DeleteImageAsync Tests

    [Fact]
    public async Task DeleteImageAsync_ShouldDeleteFile_WhenFileExists()
    {
        // Arrange
        var testFilePath = Path.Combine(_testUploadsPath, "test-delete.jpg");
        await File.WriteAllTextAsync(testFilePath, "test content");
        var relativePath = $"/test-uploads/test-delete.jpg";

        // Act
        await _service.DeleteImageAsync(relativePath);

        // Assert
        File.Exists(testFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldNotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = "/uploads/non-existent-file.jpg";

        // Act
        var act = async () => await _service.DeleteImageAsync(nonExistentPath);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldHandleNullPath_Gracefully()
    {
        // Act
        var act = async () => await _service.DeleteImageAsync(null!);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldHandleEmptyPath_Gracefully()
    {
        // Act
        var act = async () => await _service.DeleteImageAsync(string.Empty);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldHandleWhitespacePath_Gracefully()
    {
        // Act
        var act = async () => await _service.DeleteImageAsync("   ");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldConvertRelativePathToAbsolutePath()
    {
        // Arrange
        var testFilePath = Path.Combine(_testUploadsPath, "path-test.jpg");
        await File.WriteAllTextAsync(testFilePath, "test content");
        var relativePath = "/test-uploads/path-test.jpg";

        // Pre-verify file exists
        File.Exists(testFilePath).Should().BeTrue();

        // Act
        await _service.DeleteImageAsync(relativePath);

        // Assert
        File.Exists(testFilePath).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立 Mock IFormFile 用於測試
    /// </summary>
    private IFormFile CreateMockFormFile(string fileName, string content, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(bytes.Length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((targetStream, token) =>
            {
                stream.Position = 0;
                stream.CopyTo(targetStream);
            })
            .Returns(Task.CompletedTask);

        return fileMock.Object;
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        // 清理測試檔案
        if (Directory.Exists(_testUploadsPath))
        {
            try
            {
                Directory.Delete(_testUploadsPath, true);
            }
            catch
            {
                // 忽略清理錯誤
            }
        }
    }

    #endregion
}
