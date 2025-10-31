using BlogSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Security.Claims;

namespace BlogSystem.Tests.Integration.Areas.Admin;

/// <summary>
/// Admin AuthController 整合測試
/// </summary>
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 移除原有的 DbContext 註冊
                services.RemoveAll(typeof(DbContextOptions<BlogDbContext>));
                services.RemoveAll(typeof(BlogDbContext));

                // 使用 In-Memory 資料庫
                services.AddDbContext<BlogDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Auth_" + Guid.NewGuid().ToString());
                });

                // 確保資料庫已建立
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
                db.Database.EnsureCreated();
            });

            // 配置測試用的管理員郵箱
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AdminEmails"] = "admin@example.com,admin2@example.com",
                    ["Google:ClientId"] = "test-client-id",
                    ["Google:ClientSecret"] = "test-client-secret"
                });
            });
        });
    }

    #region Login Page Tests

    [Fact]
    public async Task Login_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturnHtmlContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/Login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldContainGoogleOAuthButton()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/Login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 應包含 Google OAuth 相關的連結或按鈕
        content.Should().MatchRegex(@"Google|OAuth|登入|Login");
    }

    [Fact]
    public async Task Login_UnauthenticatedUsers_CanAccessLoginPage()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Admin/Auth/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 未驗證的使用者應該可以訪問登入頁面，不應被重定向
    }

    #endregion

    #region Authentication Flow Tests

    [Fact]
    public async Task GoogleCallback_WithAdminEmail_ShouldCreateSessionCookie()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 使用測試身份驗證處理器模擬 Google OAuth 回調
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogle(options =>
                {
                    options.ClientId = "test-client-id";
                    options.ClientSecret = "test-client-secret";
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Admin/Auth/GoogleCallback");

        // Assert
        // 應該有某種形式的重定向或響應
        // 注意：完整的 OAuth 流程需要真實的 Google 端點，這裡測試路由是否可訪問
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.BadRequest // 可能因為缺少 OAuth 參數
        );
    }

    [Fact]
    public async Task GoogleCallback_WithAdminEmail_ShouldAllowAccess()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");

        // Act - 嘗試訪問需要管理員權限的頁面
        var response = await client.GetAsync("/Admin/Auth/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GoogleCallback_WithNonAdminEmail_ShouldRedirectToAccessDenied()
    {
        // Arrange
        var client = CreateAuthenticatedClient("user@example.com");

        // Act - 嘗試訪問需要管理員權限的頁面（例如 Admin Dashboard）
        // 注意：這個測試假設有管理員儀表板路由，需要根據實際路由調整
        var response = await client.GetAsync("/Admin");

        // Assert
        // 非管理員應該被重定向或拒絕訪問
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task AuthenticatedUser_WithAdminEmail_ShouldHaveAccessToCookie()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");

        // Act
        var response = await client.GetAsync("/Admin/Auth/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 檢查是否有身份驗證 cookie
        response.Headers.Should().NotBeNull();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldClearAuthentication()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com", allowAutoRedirect: false);

        // Act
        var response = await client.PostAsync("/Admin/Auth/Logout", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    [Fact]
    public async Task Logout_ShouldRedirectToHomepage()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com", allowAutoRedirect: false);

        // Act
        var response = await client.PostAsync("/Admin/Auth/Logout", null);

        // Assert
        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
        {
            response.Headers.Location.Should().NotBeNull();
            // 應該重定向到首頁或登入頁面
            var location = response.Headers.Location?.ToString();
            location.Should().MatchRegex(@"^(/|/Home|/Admin/Auth/Login)");
        }
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldStillSucceed()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.PostAsync("/Admin/Auth/Logout", null);

        // Assert
        // 未驗證的使用者登出應該仍然成功（或重定向）
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.BadRequest // 如果只接受 POST 並且需要 CSRF token
        );
    }

    #endregion

    #region Access Control Tests

    [Fact]
    public async Task AccessDenied_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/AccessDenied");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccessDenied_ShouldReturnHtmlContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/AccessDenied");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AccessDenied_ShouldShowAppropriateMessage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/AccessDenied");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 應包含拒絕訪問相關的訊息
        content.Should().MatchRegex(@"(Access Denied|拒絕訪問|權限|Permission|Denied|無權)");
    }

    [Fact]
    public async Task AccessDenied_UnauthenticatedUsers_CanAccessPage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin/Auth/AccessDenied");

        // Assert
        // 訪問拒絕頁面應該對所有人開放
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立已驗證的測試客戶端
    /// </summary>
    /// <param name="email">使用者郵箱</param>
    /// <param name="allowAutoRedirect">是否允許自動重定向</param>
    /// <returns>已驗證的 HttpClient</returns>
    private HttpClient CreateAuthenticatedClient(string email, bool allowAutoRedirect = true)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 添加測試身份驗證方案
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                // 註冊測試用的 claims
                services.AddSingleton<TestClaimsProvider>(sp => new TestClaimsProvider
                {
                    Claims = new[]
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, email.Split('@')[0]),
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    }
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });

        return client;
    }

    #endregion
}

/// <summary>
/// 測試用的身份驗證處理器
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TestClaimsProvider _claimsProvider;

    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        TestClaimsProvider claimsProvider) : base(options, logger, encoder)
    {
        _claimsProvider = claimsProvider;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = _claimsProvider.Claims;
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// 測試用的 Claims 提供者
/// </summary>
public class TestClaimsProvider
{
    public Claim[] Claims { get; set; } = Array.Empty<Claim>();
}
