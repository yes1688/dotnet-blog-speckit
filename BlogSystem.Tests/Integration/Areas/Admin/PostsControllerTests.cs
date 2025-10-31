using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BlogSystem.Tests.Integration.Areas.Admin;

/// <summary>
/// Admin PostsController 整合測試
/// </summary>
public class PostsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PostsControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_AdminPosts_" + Guid.NewGuid().ToString());
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
                    ["AdminEmails"] = "admin@example.com",
                    ["Google:ClientId"] = "test-client-id",
                    ["Google:ClientSecret"] = "test-client-secret"
                });
            });
        });
    }

    #region Index (列表頁) Tests

    [Fact]
    public async Task Index_WithAuthentication_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");

        // Act
        var response = await client.GetAsync("/Admin/Posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Index_WithAuthentication_ShouldReturnHtmlContent()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");

        // Act
        var response = await client.GetAsync("/Admin/Posts");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Index_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Admin/Posts");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    [Fact]
    public async Task Index_WithAuthentication_ShouldDisplayAllPosts()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        await SeedTestPosts();

        // Act
        var response = await client.GetAsync("/Admin/Posts");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("已發布文章");
        content.Should().Contain("草稿文章");
    }

    #endregion

    #region Create (新增表單) Tests

    [Fact]
    public async Task Create_Get_WithAuthentication_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");

        // Act
        var response = await client.GetAsync("/Admin/Posts/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Get_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Admin/Posts/Create");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    [Fact]
    public async Task Create_Post_WithValidData_ShouldCreatePostAndRedirect()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var formData = new Dictionary<string, string>
        {
            ["Title"] = "新建文章",
            ["Slug"] = "2025/10/new-post",
            ["Content"] = "# 測試內容\n\n這是新建的文章。",
            ["Summary"] = "文章摘要",
            ["Status"] = "1" // Published
        };

        // Act
        var response = await client.PostAsync("/Admin/Posts/Create",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.OK
        );

        // 驗證資料庫中已建立文章
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Title == "新建文章");

        post.Should().NotBeNull();
        post!.Slug.Should().Be("2025/10/new-post");
        post.Content.Should().Contain("測試內容");
    }

    [Fact]
    public async Task Create_Post_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var formData = new Dictionary<string, string>
        {
            ["Title"] = "", // 空標題 - 無效
            ["Slug"] = "2025/10/invalid-post",
            ["Content"] = "內容"
        };

        // Act
        var response = await client.PostAsync("/Admin/Posts/Create",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, // 返回表單並顯示驗證錯誤
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task Create_Post_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var formData = new Dictionary<string, string>
        {
            ["Title"] = "未授權文章",
            ["Slug"] = "2025/10/unauthorized-post",
            ["Content"] = "內容"
        };

        // Act
        var response = await client.PostAsync("/Admin/Posts/Create",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    #endregion

    #region Edit (編輯表單) Tests

    [Fact]
    public async Task Edit_Get_WithValidId_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedTestPost("編輯測試文章", "2025/10/edit-test");

        // Act
        var response = await client.GetAsync($"/Admin/Posts/Edit/{postId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Admin/Posts/Edit/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Edit_Get_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var postId = await SeedTestPost("測試文章", "2025/10/test-post");

        // Act
        var response = await client.GetAsync($"/Admin/Posts/Edit/{postId}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    [Fact]
    public async Task Edit_Post_WithValidData_ShouldUpdatePost()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedTestPost("原始標題", "2025/10/original-slug");

        var formData = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "更新後的標題",
            ["Slug"] = "2025/10/updated-slug",
            ["Content"] = "# 更新後的內容",
            ["Summary"] = "更新後的摘要",
            ["Status"] = "1" // Published
        };

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Edit/{postId}",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.OK
        );

        // 驗證資料庫中的文章已更新
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FindAsync(postId);

        post.Should().NotBeNull();
        post!.Title.Should().Be("更新後的標題");
        post.Slug.Should().Be("2025/10/updated-slug");
        post.Content.Should().Contain("更新後的內容");
    }

    [Fact]
    public async Task Edit_Post_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var nonExistentId = Guid.NewGuid();

        var formData = new Dictionary<string, string>
        {
            ["Id"] = nonExistentId.ToString(),
            ["Title"] = "更新標題",
            ["Slug"] = "2025/10/slug",
            ["Content"] = "內容"
        };

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Edit/{nonExistentId}",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task Edit_Post_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedTestPost("測試文章", "2025/10/test");

        var formData = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "", // 空標題 - 無效
            ["Slug"] = "2025/10/test",
            ["Content"] = "內容"
        };

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Edit/{postId}",
            new FormUrlEncodedContent(formData));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, // 返回表單並顯示驗證錯誤
            HttpStatusCode.BadRequest
        );
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_Post_WithValidId_ShouldDeletePost()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedTestPost("待刪除文章", "2025/10/to-delete");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Delete/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.OK,
            HttpStatusCode.NoContent
        );

        // 驗證資料庫中文章已被刪除
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FindAsync(postId);

        post.Should().BeNull();
    }

    [Fact]
    public async Task Delete_Post_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Delete/{nonExistentId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task Delete_Post_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var postId = await SeedTestPost("測試文章", "2025/10/test");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Delete/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    #endregion

    #region Publish/Unpublish (狀態切換) Tests

    [Fact]
    public async Task Publish_Post_WithDraftPost_ShouldPublishPost()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedDraftPost("草稿文章", "2025/10/draft-post");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Publish/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.OK,
            HttpStatusCode.NoContent
        );

        // 驗證文章狀態已更新為 Published
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FindAsync(postId);

        post.Should().NotBeNull();
        post!.Status.Should().Be(PostStatus.Published);
        post.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_Post_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Publish/{nonExistentId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task Unpublish_Post_WithPublishedPost_ShouldUnpublishPost()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var postId = await SeedTestPost("已發布文章", "2025/10/published-post");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Unpublish/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.OK,
            HttpStatusCode.NoContent
        );

        // 驗證文章狀態已更新為 Draft
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FindAsync(postId);

        post.Should().NotBeNull();
        post!.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public async Task Unpublish_Post_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Unpublish/{nonExistentId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task Publish_Post_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var postId = await SeedDraftPost("草稿", "2025/10/draft");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Publish/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    [Fact]
    public async Task Unpublish_Post_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var postId = await SeedTestPost("已發布", "2025/10/published");

        // Act
        var response = await client.PostAsync($"/Admin/Posts/Unpublish/{postId}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立已驗證的測試客戶端
    /// </summary>
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

    /// <summary>
    /// 種子測試文章（已發布狀態）
    /// </summary>
    private async Task<Guid> SeedTestPost(string title, string slug, string content = "測試內容")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = content,
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    /// <summary>
    /// 種子草稿文章
    /// </summary>
    private async Task<Guid> SeedDraftPost(string title, string slug, string content = "草稿內容")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = content,
            Status = PostStatus.Draft,
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    /// <summary>
    /// 種子測試文章列表
    /// </summary>
    private async Task SeedTestPosts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new[]
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "已發布文章",
                Slug = "2025/10/published-post",
                Content = "這是已發布的文章",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ViewCount = 10,
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "草稿文章",
                Slug = "2025/10/draft-post",
                Content = "這是草稿文章",
                Status = PostStatus.Draft,
                PublishedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ViewCount = 0,
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    #endregion
}
