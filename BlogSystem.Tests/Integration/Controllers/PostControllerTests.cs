using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace BlogSystem.Tests.Integration.Controllers;

/// <summary>
/// PostController 整合測試
/// </summary>
public class PostControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PostControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Post_" + Guid.NewGuid().ToString());
                });

                // 確保資料庫已建立
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task Details_ShouldReturnSuccessStatusCode_WhenPostExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var postId = await SeedTestPost("測試文章", "2025/10/test-post");

        // Act
        var response = await client.GetAsync("/2025/10/test-post");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Details_ShouldReturnHtmlContent_WithPostDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestPost("我的測試文章", "2025/10/my-test-post", "# 測試內容\n\n這是 **Markdown** 格式的文章。");

        // Act
        var response = await client.GetAsync("/2025/10/my-test-post");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("我的測試文章");
        content.Should().Contain("測試內容"); // Markdown 標題應該被渲染
        content.Should().Contain("<strong>Markdown</strong>"); // 粗體應該被渲染為 HTML
    }

    [Fact]
    public async Task Details_ShouldReturn404_WhenPostDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/2025/10/non-existent-post");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Details_ShouldSupportChineseUrlSlug()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestPost("歡迎來到我的部落格", "2025/10/歡迎來到我的部落格");

        // Act
        var response = await client.GetAsync("/2025/10/歡迎來到我的部落格");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("歡迎來到我的部落格");
    }

    [Fact]
    public async Task Details_ShouldIncrementViewCount_OnEachVisit()
    {
        // Arrange
        var client = _factory.CreateClient();
        var postId = await SeedTestPost("查看計數測試", "2025/10/view-count-test");

        // Act - 訪問三次
        await client.GetAsync("/2025/10/view-count-test");
        await client.GetAsync("/2025/10/view-count-test");
        await client.GetAsync("/2025/10/view-count-test");

        // Assert - 檢查資料庫中的查看次數
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.BlogPosts.FindAsync(postId);

        post.Should().NotBeNull();
        post!.ViewCount.Should().Be(3);
    }

    [Fact]
    public async Task Details_ShouldNotShowDraftPosts()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedDraftPost("草稿文章", "2025/10/draft-post");

        // Act
        var response = await client.GetAsync("/2025/10/draft-post");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Details_ShouldDisplayCategory_WhenPostHasCategory()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("技術");
        await SeedTestPostWithCategory("技術文章", "2025/10/tech-post", categoryId);

        // Act
        var response = await client.GetAsync("/2025/10/tech-post");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("技術");
    }

    [Fact]
    public async Task Details_ShouldDisplayTags_WhenPostHasTags()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "ASP.NET Core", "C#", "測試" });
        await SeedTestPostWithTags("標籤測試文章", "2025/10/tags-test", tagIds);

        // Act
        var response = await client.GetAsync("/2025/10/tags-test");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("ASP.NET Core");
        content.Should().Contain("C#");
        content.Should().Contain("測試");
    }

    [Fact]
    public async Task Details_ShouldRenderMarkdown_WithCodeBlocks()
    {
        // Arrange
        var client = _factory.CreateClient();
        var markdownContent = @"# 程式碼範例

```csharp
public class Hello
{
    public void SayHello() => Console.WriteLine(""Hello!"");
}
```";
        await SeedTestPost("程式碼文章", "2025/10/code-post", markdownContent);

        // Act
        var response = await client.GetAsync("/2025/10/code-post");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("程式碼範例");
        content.Should().Contain("<code"); // 應該包含程式碼區塊
        content.Should().Contain("public class Hello");
    }

    [Fact]
    public async Task Details_ShouldMatchYearAndMonth_InSlug()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestPost("2024年文章", "2024/12/old-post");
        await SeedTestPost("2025年文章", "2025/10/new-post");

        // Act
        var response2024 = await client.GetAsync("/2024/12/old-post");
        var response2025 = await client.GetAsync("/2025/10/new-post");
        var responseWrong = await client.GetAsync("/2024/10/new-post"); // 錯誤的年份

        // Assert
        response2024.StatusCode.Should().Be(HttpStatusCode.OK);
        response2025.StatusCode.Should().Be(HttpStatusCode.OK);
        responseWrong.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #region Helper Methods

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

    private async Task<Guid> SeedDraftPost(string title, string slug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = "草稿內容",
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

    private async Task<Guid> SeedCategory(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower(),
            DisplayOrder = 0
        };

        await db.Categories.AddAsync(category);
        await db.SaveChangesAsync();

        return category.Id;
    }

    private async Task<List<Guid>> SeedTags(string[] tagNames)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var tagIds = new List<Guid>();
        foreach (var name in tagNames)
        {
            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = name.ToLower().Replace(" ", "-"),
                UsageCount = 0
            };
            await db.Tags.AddAsync(tag);
            tagIds.Add(tag.Id);
        }

        await db.SaveChangesAsync();
        return tagIds;
    }

    private async Task<Guid> SeedTestPostWithCategory(string title, string slug, Guid categoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = "測試內容",
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            CategoryId = categoryId,
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    private async Task<Guid> SeedTestPostWithTags(string title, string slug, List<Guid> tagIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var tags = await db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = "測試內容",
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            Tags = tags
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    #endregion
}
