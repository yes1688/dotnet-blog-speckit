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
/// CategoryController 整合測試
/// 測試訪客透過分類瀏覽文章的功能
/// 根據 User Story 4 - 分類與標籤管理
/// </summary>
public class CategoryControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CategoryControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Category_" + Guid.NewGuid().ToString());
                });

                // 確保資料庫已建立
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    #region Test Cases - Index Action

    /// <summary>
    /// 測試案例 1: 根據分類 ID 過濾文章
    /// 驗證只顯示指定分類下的文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturnOnlyArticlesFromSpecificCategory()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("技術");
        var otherCategoryId = await SeedCategory("生活");

        await SeedPublishedPostWithCategory("技術文章1", "2025/10/tech-article-1", categoryId);
        await SeedPublishedPostWithCategory("技術文章2", "2025/10/tech-article-2", categoryId);
        await SeedPublishedPostWithCategory("生活文章1", "2025/10/life-article-1", otherCategoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("技術文章1");
        content.Should().Contain("技術文章2");
        content.Should().NotContain("生活文章1");
    }

    /// <summary>
    /// 測試案例 2: 只顯示已發布的文章
    /// 驗證草稿文章不會在分類頁面顯示
    /// </summary>
    [Fact]
    public async Task Index_ShouldOnlyShowPublishedArticles_NotDrafts()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("技術");

        await SeedPublishedPostWithCategory("已發布文章", "2025/10/published-article", categoryId);
        await SeedDraftPostWithCategory("草稿文章", "2025/10/draft-article", categoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("已發布文章");
        content.Should().NotContain("草稿文章");
    }

    /// <summary>
    /// 測試案例 3: 分頁功能 - 每頁 10 篇
    /// 驗證正確的分頁邏輯
    /// </summary>
    [Fact]
    public async Task Index_ShouldSupportPagination_WithPageSize10()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("技術");

        // 建立 15 篇文章
        for (int i = 1; i <= 15; i++)
        {
            await SeedPublishedPostWithCategory($"技術文章{i}", $"2025/10/tech-article-{i}", categoryId);
        }

        // Act - 第一頁
        var response1 = await client.GetAsync($"/Category/Index?categoryId={categoryId}&page=1");
        var content1 = await response1.Content.ReadAsStringAsync();

        // Act - 第二頁
        var response2 = await client.GetAsync($"/Category/Index?categoryId={categoryId}&page=2");
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert - 第一頁應該有 10 篇文章
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        content1.Should().Contain("技術文章1");
        content1.Should().Contain("技術文章10");

        // Assert - 第二頁應該有 5 篇文章
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        content2.Should().Contain("技術文章11");
        content2.Should().Contain("技術文章15");

        // 第一頁不應該包含第二頁的內容
        content1.Should().NotContain("技術文章11");
        content2.Should().NotContain("技術文章1");
    }

    /// <summary>
    /// 測試案例 4: 分類不存在時返回 404
    /// 驗證無效的分類 ID 返回 404 狀態碼
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentCategoryId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={nonExistentCategoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 測試案例 5: 排序 - 最新文章在前
    /// 驗證文章按發布時間逆序排列
    /// </summary>
    [Fact]
    public async Task Index_ShouldSortArticlesByPublishedDate_NewestFirst()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("技術");

        var baseDate = DateTime.UtcNow;

        // 建立三篇文章，發布時間不同
        var oldPostId = await SeedPublishedPostWithCategoryAndDate("舊文章", "2025/10/old-article", categoryId, baseDate.AddDays(-10));
        var middlePostId = await SeedPublishedPostWithCategoryAndDate("中間文章", "2025/10/middle-article", categoryId, baseDate.AddDays(-5));
        var newPostId = await SeedPublishedPostWithCategoryAndDate("新文章", "2025/10/new-article", categoryId, baseDate);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證排序順序：新文章應該出現在前，舊文章在後
        var newArticleIndex = content.IndexOf("新文章");
        var middleArticleIndex = content.IndexOf("中間文章");
        var oldArticleIndex = content.IndexOf("舊文章");

        newArticleIndex.Should().BeLessThan(middleArticleIndex);
        middleArticleIndex.Should().BeLessThan(oldArticleIndex);
    }

    /// <summary>
    /// 測試案例 6: 無效的分類 ID 格式返回 404
    /// 驗證非 GUID 格式的分類 ID 返回 404
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn404_WhenCategoryIdIsInvalidGuid()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Category/Index?categoryId=invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 測試案例 7: 同分類下的多篇文章
    /// 驗證分類頁面能正確顯示同分類的多篇文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldDisplayMultipleArticlesInSameCategory()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("ASP.NET");

        await SeedPublishedPostWithCategory("ASP.NET Core 入門", "2025/10/aspnet-core-intro", categoryId);
        await SeedPublishedPostWithCategory("ASP.NET Core 進階", "2025/10/aspnet-core-advanced", categoryId);
        await SeedPublishedPostWithCategory("ASP.NET Core 最佳實踐", "2025/10/aspnet-core-best-practices", categoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("ASP.NET Core 入門");
        content.Should().Contain("ASP.NET Core 進階");
        content.Should().Contain("ASP.NET Core 最佳實踐");
    }

    /// <summary>
    /// 測試案例 8: 其他分類的文章不應顯示
    /// 驗證分類隔離 - 不同分類的文章不會混合顯示
    /// </summary>
    [Fact]
    public async Task Index_ShouldNotShowArticlesFromOtherCategories()
    {
        // Arrange
        var client = _factory.CreateClient();
        var techCategoryId = await SeedCategory("技術");
        var lifeCategoryId = await SeedCategory("生活");
        var gamesCategoryId = await SeedCategory("遊戲");

        await SeedPublishedPostWithCategory("C# 教程", "2025/10/csharp-tutorial", techCategoryId);
        await SeedPublishedPostWithCategory("旅遊日記", "2025/10/travel-diary", lifeCategoryId);
        await SeedPublishedPostWithCategory("遊戲評測", "2025/10/game-review", gamesCategoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={techCategoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("C# 教程");
        content.Should().NotContain("旅遊日記");
        content.Should().NotContain("遊戲評測");
    }

    /// <summary>
    /// 測試案例 9: 分類存在但沒有已發布文章時返回 404
    /// 驗證當分類存在但沒有已發布的文章時返回 404
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn404_WhenCategoryExistsButHasNoDraftArticles()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("空分類");

        // 新增只有草稿文章的分類
        await SeedDraftPostWithCategory("草稿1", "2025/10/draft-1", categoryId);
        await SeedDraftPostWithCategory("草稿2", "2025/10/draft-2", categoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 測試案例 10: 分類成功返回時應包含分類資訊
    /// 驗證頁面中顯示正確的分類信息
    /// </summary>
    [Fact]
    public async Task Index_ShouldDisplayCategoryInfo_WhenCategoryExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var categoryId = await SeedCategory("前端開發");

        await SeedPublishedPostWithCategory("React 教程", "2025/10/react-tutorial", categoryId);
        await SeedPublishedPostWithCategory("Vue 教程", "2025/10/vue-tutorial", categoryId);

        // Act
        var response = await client.GetAsync($"/Category/Index?categoryId={categoryId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 驗證返回的內容中包含分類信息
        content.Should().Contain("React 教程");
        content.Should().Contain("Vue 教程");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立測試分類
    /// </summary>
    private async Task<Guid> SeedCategory(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            DisplayOrder = 0
        };

        await db.Categories.AddAsync(category);
        await db.SaveChangesAsync();

        return category.Id;
    }

    /// <summary>
    /// 建立測試已發布文章 (含分類)
    /// </summary>
    private async Task<Guid> SeedPublishedPostWithCategory(string title, string slug, Guid categoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = $"{title}的內容",
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

    /// <summary>
    /// 建立測試已發布文章 (含分類和發布日期)
    /// </summary>
    private async Task<Guid> SeedPublishedPostWithCategoryAndDate(string title, string slug, Guid categoryId, DateTime publishedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = $"{title}的內容",
            Status = PostStatus.Published,
            PublishedAt = publishedAt,
            CreatedAt = publishedAt,
            UpdatedAt = publishedAt,
            ViewCount = 0,
            CategoryId = categoryId,
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    /// <summary>
    /// 建立測試草稿文章 (含分類)
    /// </summary>
    private async Task<Guid> SeedDraftPostWithCategory(string title, string slug, Guid categoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = $"{title}的內容",
            Status = PostStatus.Draft,
            PublishedAt = null,
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

    #endregion
}
