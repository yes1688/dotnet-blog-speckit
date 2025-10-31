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
/// TagController 整合測試 (User Story 4 - 分類與標籤管理)
///
/// 測試訪客透過標籤瀏覽文章的功能
/// </summary>
public class TagControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TagControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Tag_" + Guid.NewGuid().ToString());
                });

                // 確保資料庫已建立
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    #region Index Action Tests

    /// <summary>
    /// 測試: 根據標籤 ID 過濾文章
    /// User Story 4 Acceptance Scenario 5: 訪客在文章頁面點擊標籤，顯示具有相同標籤的其他文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldFilterPostsByTagId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "ASP.NET Core", "C#" });
        var tag1Id = tagIds[0];

        await SeedPublishedPostWithTags("文章1", "2025/10/post1", new[] { tag1Id });
        await SeedPublishedPostWithTags("文章2", "2025/10/post2", new[] { tag1Id });
        await SeedPublishedPostWithTags("文章3", "2025/10/post3", new[] { tagIds[1] }); // 不同標籤

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tag1Id}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("文章1");
        content.Should().Contain("文章2");
        content.Should().NotContain("文章3");
    }

    /// <summary>
    /// 測試: 只顯示已發布的文章
    /// User Story 1 Acceptance Scenario 1: 訪客進入首頁，顯示最新發布的文章列表
    /// </summary>
    [Fact]
    public async Task Index_ShouldOnlyShowPublishedPosts()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "Docker" });
        var tagId = tagIds[0];

        await SeedPublishedPostWithTags("已發布文章", "2025/10/published", new[] { tagId });
        await SeedDraftPostWithTags("草稿文章", "2025/10/draft", new[] { tagId });

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("已發布文章");
        content.Should().NotContain("草稿文章");
    }

    /// <summary>
    /// 測試: 多對多關聯 - 一篇文章可有多個標籤
    /// User Story 4 Acceptance Scenario 3: 管理員編輯文章時新增標籤，標籤與文章關聯並可重複使用
    /// </summary>
    [Fact]
    public async Task Index_ShouldHandleMultipleTags_OnSinglePost()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "ASP.NET Core", "C#", "測試" });

        // 建立一篇文章有多個標籤
        await SeedPublishedPostWithTags("多標籤文章", "2025/10/multi-tags", tagIds.ToArray());

        // Act - 搜索第一個標籤
        var response = await client.GetAsync($"/Tag/Index?tagId={tagIds[0]}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - 應該找到包含所有標籤的文章
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("多標籤文章");
    }

    /// <summary>
    /// 測試: 分頁功能 (每頁 10 篇)
    /// User Story 1 Acceptance Scenario 6: 超過 10 篇文章時，每頁顯示 10 篇，提供分頁導航
    /// </summary>
    [Fact]
    public async Task Index_ShouldSupportPagination_WithTenItemsPerPage()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "Pagination" });
        var tagId = tagIds[0];

        // 建立 15 篇文章
        for (int i = 1; i <= 15; i++)
        {
            await SeedPublishedPostWithTags($"文章{i}", $"2025/10/post{i}", new[] { tagId });
        }

        // Act - 第一頁
        var response1 = await client.GetAsync($"/Tag/Index?tagId={tagId}&page=1");
        var content1 = await response1.Content.ReadAsStringAsync();

        // Act - 第二頁
        var response2 = await client.GetAsync($"/Tag/Index?tagId={tagId}&page=2");
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 第一頁應該有文章 1-10
        content1.Should().Contain("文章1");
        content1.Should().Contain("文章10");

        // 第二頁應該有文章 11-15
        content2.Should().Contain("文章11");
        content2.Should().Contain("文章15");
    }

    /// <summary>
    /// 測試: 標籤不存在時返回 404
    /// User Story 4 應該驗證的邊界情況
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn404_WhenTagDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentTagId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={nonExistentTagId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 測試: 排序 - 最新文章在前
    /// User Story 1 Acceptance Scenario 4: 部落格有多篇文章，文章按發布日期由新到舊排序
    /// </summary>
    [Fact]
    public async Task Index_ShouldOrderByPublishDateDescending()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "Sorting" });
        var tagId = tagIds[0];

        var now = DateTime.UtcNow;

        // 建立 3 篇文章，發布時間不同
        await SeedPublishedPostWithTags("舊文章", "2025/10/old", new[] { tagId }, now.AddDays(-2));
        await SeedPublishedPostWithTags("新文章", "2025/10/new", new[] { tagId }, now);
        await SeedPublishedPostWithTags("中文章", "2025/10/middle", new[] { tagId }, now.AddDays(-1));

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - 應按發布日期由新到舊排序
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newIndex = content.IndexOf("新文章");
        var middleIndex = content.IndexOf("中文章");
        var oldIndex = content.IndexOf("舊文章");

        newIndex.Should().BeLessThan(middleIndex);
        middleIndex.Should().BeLessThan(oldIndex);
    }

    /// <summary>
    /// 測試: 標籤為空時返回 400
    /// 邊界情況測試
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn400_WhenTagIdIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Tag/Index");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Post Filtering Tests

    /// <summary>
    /// 測試: 同標籤下的多篇文章
    /// User Story 4 Acceptance Scenario 5: 點擊標籤後顯示具有相同標籤的其他文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldDisplayAllPostsUnderSameTag()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "WebDevelopment" });
        var tagId = tagIds[0];

        await SeedPublishedPostWithTags("ASP.NET 基礎", "2025/10/aspnet-basics", new[] { tagId });
        await SeedPublishedPostWithTags("ASP.NET 進階", "2025/10/aspnet-advanced", new[] { tagId });
        await SeedPublishedPostWithTags("ASP.NET 最佳實踐", "2025/10/aspnet-best-practices", new[] { tagId });

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("ASP.NET 基礎");
        content.Should().Contain("ASP.NET 進階");
        content.Should().Contain("ASP.NET 最佳實踐");
    }

    /// <summary>
    /// 測試: 草稿文章不應顯示
    /// User Story 1 Acceptance Scenario 1: 顯示已發布的文章列表（暗示草稿文章不顯示）
    /// </summary>
    [Fact]
    public async Task Index_ShouldExcludeDraftPosts()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "DraftTest" });
        var tagId = tagIds[0];

        await SeedPublishedPostWithTags("已發布1", "2025/10/published1", new[] { tagId });
        await SeedPublishedPostWithTags("已發布2", "2025/10/published2", new[] { tagId });
        await SeedDraftPostWithTags("草稿", "2025/10/draft", new[] { tagId });

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("已發布1");
        content.Should().Contain("已發布2");
        content.Should().NotContain("草稿");
    }

    /// <summary>
    /// 測試: 其他標籤的文章不應顯示
    /// User Story 4 Acceptance Scenario 5: 點擊標籤時只顯示具有相同標籤的文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldNotIncludePostsWithDifferentTags()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "Tag1", "Tag2", "Tag3" });

        // 文章分別關聯不同標籤
        await SeedPublishedPostWithTags("文章A", "2025/10/post-a", new[] { tagIds[0] });
        await SeedPublishedPostWithTags("文章B", "2025/10/post-b", new[] { tagIds[1] });
        await SeedPublishedPostWithTags("文章C", "2025/10/post-c", new[] { tagIds[2] });

        // Act - 僅搜尋 Tag1
        var response = await client.GetAsync($"/Tag/Index?tagId={tagIds[0]}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("文章A");
        content.Should().NotContain("文章B");
        content.Should().NotContain("文章C");
    }

    /// <summary>
    /// 測試: 一篇文章有多個標籤的場景
    /// User Story 4 Acceptance Scenario 3: 編輯文章時新增多個標籤
    /// </summary>
    [Fact]
    public async Task Index_ShouldFindPostWithMultipleTags_ByAnyTag()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "C#", "LINQ", "Entity Framework" });

        // 建立一篇文章有三個標籤
        var postId = await SeedPublishedPostWithTags("LINQ 技巧", "2025/10/linq-tips", tagIds.ToArray());

        // Act - 分別用三個標籤搜尋
        var response1 = await client.GetAsync($"/Tag/Index?tagId={tagIds[0]}");
        var response2 = await client.GetAsync($"/Tag/Index?tagId={tagIds[1]}");
        var response3 = await client.GetAsync($"/Tag/Index?tagId={tagIds[2]}");

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        var content3 = await response3.Content.ReadAsStringAsync();

        // Assert - 三個搜尋結果都應該包含該文章
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        content1.Should().Contain("LINQ 技巧");
        content2.Should().Contain("LINQ 技巧");
        content3.Should().Contain("LINQ 技巧");
    }

    #endregion

    #region Edge Cases and Error Handling

    /// <summary>
    /// 測試: 標籤存在但無文章時返回 200 (空列表)
    /// 邊界情況測試
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturnEmptyList_WhenTagHasNoPosts()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "EmptyTag" });
        var tagId = tagIds[0];

        // 不建立任何文章

        // Act
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 應該顯示「沒有文章」或類似訊息
    }

    /// <summary>
    /// 測試: 無效的 GUID 格式
    /// 邊界情況測試
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturn400_WhenTagIdIsInvalidGuid()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Tag/Index?tagId=invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 測試: 分頁超出範圍
    /// 邊界情況測試
    /// </summary>
    [Fact]
    public async Task Index_ShouldHandleInvalidPage_Gracefully()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tagIds = await SeedTags(new[] { "PageTest" });
        var tagId = tagIds[0];

        await SeedPublishedPostWithTags("文章1", "2025/10/post1", new[] { tagId });

        // Act - 請求不存在的頁面
        var response = await client.GetAsync($"/Tag/Index?tagId={tagId}&page=999");

        // Assert - 應該返回 OK 但內容為空，或返回 400/404
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立一個已發布的文章並關聯標籤
    /// </summary>
    private async Task<Guid> SeedPublishedPostWithTags(string title, string slug, Guid[] tagIds, DateTime? publishedAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var tags = await db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();

        var now = DateTime.UtcNow;
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = $"{title} 的內容",
            Summary = $"{title} 的摘要",
            Status = PostStatus.Published,
            PublishedAt = publishedAt ?? now,
            CreatedAt = now,
            UpdatedAt = now,
            ViewCount = 0,
            Tags = tags
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    /// <summary>
    /// 建立一個草稿文章並關聯標籤
    /// </summary>
    private async Task<Guid> SeedDraftPostWithTags(string title, string slug, Guid[] tagIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var tags = await db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();

        var now = DateTime.UtcNow;
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = $"{title} 的內容",
            Summary = $"{title} 的摘要",
            Status = PostStatus.Draft,
            PublishedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            ViewCount = 0,
            Tags = tags
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();

        return post.Id;
    }

    /// <summary>
    /// 建立標籤
    /// </summary>
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

    #endregion
}
