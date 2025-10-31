using BlogSystem.Core.Entities;
using BlogSystem.Core.Interfaces;
using BlogSystem.Core.Services;
using FluentAssertions;
using Moq;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// AuthService 單元測試
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IRepository<AdminLog>> _mockRepository;
    private string _adminEmailsConfig = string.Empty;

    public AuthServiceTests()
    {
        _mockRepository = new Mock<IRepository<AdminLog>>();
    }

    #region IsAdminAsync Tests

    [Fact]
    public async Task IsAdminAsync_ShouldReturnTrue_WhenEmailIsInAdminList()
    {
        // Arrange
        var adminEmail = "admin@example.com";
        var adminEmails = new[] { "admin@example.com", "superadmin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync(adminEmail);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_ShouldReturnFalse_WhenEmailIsNotInAdminList()
    {
        // Arrange
        var nonAdminEmail = "user@example.com";
        var adminEmails = new[] { "admin@example.com", "superadmin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync(nonAdminEmail);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_ShouldBeCaseInsensitive_WhenCheckingEmail()
    {
        // Arrange
        var adminEmails = new[] { "admin@example.com", "superadmin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var resultUpperCase = await service.IsAdminAsync("ADMIN@EXAMPLE.COM");
        var resultMixedCase = await service.IsAdminAsync("Admin@Example.Com");
        var resultLowerCase = await service.IsAdminAsync("admin@example.com");

        // Assert
        resultUpperCase.Should().BeTrue("uppercase email should match");
        resultMixedCase.Should().BeTrue("mixed case email should match");
        resultLowerCase.Should().BeTrue("lowercase email should match");
    }

    [Fact]
    public async Task IsAdminAsync_ShouldReturnFalse_WhenEmailIsNull()
    {
        // Arrange
        var adminEmails = new[] { "admin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_ShouldReturnFalse_WhenEmailIsEmpty()
    {
        // Arrange
        var adminEmails = new[] { "admin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_ShouldReturnFalse_WhenEmailIsWhitespace()
    {
        // Arrange
        var adminEmails = new[] { "admin@example.com" };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_ShouldHandleMultipleAdminEmails()
    {
        // Arrange
        var adminEmails = new[]
        {
            "admin1@example.com",
            "admin2@example.com",
            "admin3@example.com",
            "superadmin@example.com"
        };
        SetupAdminEmailConfiguration(adminEmails);
        var service = CreateAuthService();

        // Act & Assert
        foreach (var email in adminEmails)
        {
            var result = await service.IsAdminAsync(email);
            result.Should().BeTrue($"{email} should be recognized as admin");
        }

        var nonAdminResult = await service.IsAdminAsync("user@example.com");
        nonAdminResult.Should().BeFalse("non-admin email should not be recognized");
    }

    [Fact]
    public async Task IsAdminAsync_ShouldReturnFalse_WhenNoAdminEmailsConfigured()
    {
        // Arrange
        SetupAdminEmailConfiguration(Array.Empty<string>());
        var service = CreateAuthService();

        // Act
        var result = await service.IsAdminAsync("admin@example.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LogAdminActionAsync Tests

    [Fact]
    public async Task LogAdminActionAsync_ShouldCreateAdminLog_WithAllRequiredFields()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "Login";
        var ipAddress = "192.168.1.1";
        var service = CreateAuthService();
        AdminLog? capturedLog = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedLog = log)
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action, ipAddress);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AdminLog>()), Times.Once);
        capturedLog.Should().NotBeNull();
        capturedLog!.Email.Should().Be(email);
        capturedLog.Action.Should().Be(action);
        capturedLog.IpAddress.Should().Be(ipAddress);
        capturedLog.Id.Should().NotBe(Guid.Empty);
        capturedLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAdminActionAsync_ShouldCreateAdminLog_WithNullIpAddress()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "Logout";
        var service = CreateAuthService();
        AdminLog? capturedLog = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedLog = log)
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action, null);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AdminLog>()), Times.Once);
        capturedLog.Should().NotBeNull();
        capturedLog!.Email.Should().Be(email);
        capturedLog.Action.Should().Be(action);
        capturedLog.IpAddress.Should().BeNull();
    }

    [Fact]
    public async Task LogAdminActionAsync_ShouldCreateAdminLog_WithoutIpAddressParameter()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "CreatePost";
        var service = CreateAuthService();
        AdminLog? capturedLog = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedLog = log)
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AdminLog>()), Times.Once);
        capturedLog.Should().NotBeNull();
        capturedLog!.Email.Should().Be(email);
        capturedLog.Action.Should().Be(action);
        capturedLog.IpAddress.Should().BeNull();
    }

    [Fact]
    public async Task LogAdminActionAsync_ShouldHandleValidIpv6Address()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "DeletePost";
        var ipv6Address = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        var service = CreateAuthService();
        AdminLog? capturedLog = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedLog = log)
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action, ipv6Address);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AdminLog>()), Times.Once);
        capturedLog.Should().NotBeNull();
        capturedLog!.IpAddress.Should().Be(ipv6Address);
    }

    [Fact]
    public async Task LogAdminActionAsync_ShouldSetTimestamp_ToUtcNow()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "UpdatePost";
        var service = CreateAuthService();
        var beforeTimestamp = DateTime.UtcNow;
        AdminLog? capturedLog = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedLog = log)
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action);
        var afterTimestamp = DateTime.UtcNow;

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.Timestamp.Should().BeOnOrAfter(beforeTimestamp);
        capturedLog.Timestamp.Should().BeOnOrBefore(afterTimestamp);
        capturedLog.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task LogAdminActionAsync_ShouldGenerateUniqueId_ForEachLog()
    {
        // Arrange
        var email = "admin@example.com";
        var action = "Login";
        var service = CreateAuthService();
        var capturedIds = new List<Guid>();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminLog>()))
            .Callback<AdminLog>(log => capturedIds.Add(log.Id))
            .ReturnsAsync((AdminLog log) => log);

        // Act
        await service.LogAdminActionAsync(email, action);
        await service.LogAdminActionAsync(email, action);
        await service.LogAdminActionAsync(email, action);

        // Assert
        capturedIds.Should().HaveCount(3);
        capturedIds.Should().OnlyHaveUniqueItems();
        capturedIds.Should().NotContain(Guid.Empty);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立 AuthService 實例 (需要在實際實作後調整)
    /// </summary>
    private IAuthService CreateAuthService()
    {
        return new AuthService(_mockRepository.Object, _adminEmailsConfig);
    }

    /// <summary>
    /// 設置管理員 Email 設定
    /// </summary>
    private void SetupAdminEmailConfiguration(string[] adminEmails)
    {
        // AuthService 期望逗號分隔的字符串
        _adminEmailsConfig = string.Join(",", adminEmails);
    }

    #endregion
}
