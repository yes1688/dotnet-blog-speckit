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
/// HomeController 整合測試
/// </summary>
public class HomeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomeControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Home_" + Guid.NewGuid().ToString());
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
    public async Task Index_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Index_ShouldReturnHtmlContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().Contain("部落格文章"); // 首頁標題
    }

    [Fact]
    public async Task Index_ShouldDisplayPublishedPosts()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestData();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("測試文章 1");
        content.Should().Contain("測試文章 2");
        content.Should().NotContain("草稿文章"); // 草稿不應顯示
    }

    [Fact]
    public async Task Index_ShouldSupportPagination()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedManyPosts(15);

        // Act - Page 1
        var responsePage1 = await client.GetAsync("/?page=1");
        var contentPage1 = await responsePage1.Content.ReadAsStringAsync();

        // Act - Page 2
        var responsePage2 = await client.GetAsync("/?page=2");
        var contentPage2 = await responsePage2.Content.ReadAsStringAsync();

        // Assert
        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 第一頁應該有 10 篇文章（預設 pageSize）
        contentPage1.Should().Contain("測試文章 1");
        contentPage1.Should().Contain("測試文章 10");

        // 第二頁應該有剩餘的 5 篇文章
        contentPage2.Should().Contain("測試文章 11");
        contentPage2.Should().Contain("測試文章 15");
    }

    [Fact]
    public async Task Index_ShouldDisplayEmptyState_WhenNoPostsExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("部落格文章");
    }

    [Fact]
    public async Task Index_ShouldOrderPostsByPublishedDateDescending()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostsWithDates();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 最新的文章應該在最前面
        var indexOfNewest = content.IndexOf("最新文章");
        var indexOfOldest = content.IndexOf("最舊文章");
        indexOfNewest.Should().BeLessThan(indexOfOldest);
    }

    #region Helper Methods

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "測試文章 1",
                Slug = "2025/10/test-post-1",
                Content = "這是測試文章 1 的內容",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "測試文章 2",
                Slug = "2025/10/test-post-2",
                Content = "這是測試文章 2 的內容",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "草稿文章",
                Slug = "2025/10/draft-post",
                Content = "這是草稿文章的內容",
                Status = PostStatus.Draft,
                PublishedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedManyPosts(int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>();
        for (int i = 1; i <= count; i++)
        {
            posts.Add(new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = $"測試文章 {i}",
                Slug = $"2025/10/test-post-{i}",
                Content = $"這是測試文章 {i} 的內容",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddDays(-i),
                Tags = new List<Tag>()
            });
        }

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedPostsWithDates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "最新文章",
                Slug = "2025/10/newest-post",
                Content = "這是最新的文章",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "最舊文章",
                Slug = "2025/10/oldest-post",
                Content = "這是最舊的文章",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    #endregion
}
