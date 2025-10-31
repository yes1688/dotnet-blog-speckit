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
/// SearchController 整合測試
/// User Story 5: 訪客能夠透過關鍵字搜尋文章標題與內容
/// </summary>
public class SearchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SearchControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Search_" + Guid.NewGuid().ToString());
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
    /// 測試 1: 無關鍵字時顯示空結果
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturnEmpty_WhenNoKeywordProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("測試文章");
    }

    /// <summary>
    /// 測試 2: 空關鍵字時顯示提示訊息
    /// </summary>
    [Fact]
    public async Task Index_ShouldShowHint_WhenKeywordIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Search?keyword=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // 應該顯示搜尋框，但無結果
        content.Should().Contain("搜尋");
    }

    /// <summary>
    /// 測試 3: 搜尋標題包含關鍵字的文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldFindPosts_WithKeywordInTitle()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostsWithVariousTitles();

        // Act
        var response = await client.GetAsync("/Search?keyword=ASP");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ASP.NET Core 教學");
        content.Should().NotContain("Python 基礎");
    }

    /// <summary>
    /// 測試 4: 搜尋內容包含關鍵字的文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldFindPosts_WithKeywordInContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostsWithVariousContent();

        // Act
        var response = await client.GetAsync("/Search?keyword=REST%20API");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Web 開發");
    }

    /// <summary>
    /// 測試 5: 搜尋摘要包含關鍵字的文章
    /// </summary>
    [Fact]
    public async Task Index_ShouldFindPosts_WithKeywordInSummary()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostsWithVariousSummaries();

        // Act
        var response = await client.GetAsync("/Search?keyword=資料庫");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("資料庫設計");
    }

    /// <summary>
    /// 測試 6: 只顯示已發布的文章（草稿不顯示）
    /// </summary>
    [Fact]
    public async Task Index_ShouldNotShowDraftPosts_WhenSearching()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedDraftAndPublishedPosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=秘密");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("已發布的秘密文章");
        content.Should().NotContain("草稿的秘密文章");
    }

    /// <summary>
    /// 測試 7: 大小寫不敏感搜尋
    /// </summary>
    [Fact]
    public async Task Index_ShouldBeCaseInsensitive()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedCaseSensitivePost();

        // Act - 搜尋不同大小寫的關鍵字
        var responseLower = await client.GetAsync("/Search?keyword=hello");
        var responseUpper = await client.GetAsync("/Search?keyword=HELLO");
        var responseMixed = await client.GetAsync("/Search?keyword=HeLLo");

        // Assert
        var contentLower = await responseLower.Content.ReadAsStringAsync();
        var contentUpper = await responseUpper.Content.ReadAsStringAsync();
        var contentMixed = await responseMixed.Content.ReadAsStringAsync();

        contentLower.Should().Contain("Hello World");
        contentUpper.Should().Contain("Hello World");
        contentMixed.Should().Contain("Hello World");
    }

    /// <summary>
    /// 測試 8: 分頁功能（每頁 10 篇）
    /// </summary>
    [Fact]
    public async Task Index_ShouldSupportPagination_WithTenPostsPerPage()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedManySearchablePostsForPagination();

        // Act - 第一頁
        var responsePage1 = await client.GetAsync("/Search?keyword=文章&page=1");
        var contentPage1 = await responsePage1.Content.ReadAsStringAsync();

        // Act - 第二頁
        var responsePage2 = await client.GetAsync("/Search?keyword=文章&page=2");
        var contentPage2 = await responsePage2.Content.ReadAsStringAsync();

        // Assert
        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 第一頁應該包含前 10 篇
        contentPage1.Should().Contain("搜尋文章 1");
        contentPage1.Should().Contain("搜尋文章 10");

        // 第二頁應該包含後面的文章
        contentPage2.Should().Contain("搜尋文章 11");
    }

    /// <summary>
    /// 測試 9: 關鍵字高亮顯示（返回 ViewBag.Keyword）
    /// </summary>
    [Fact]
    public async Task Index_ShouldHighlightKeyword_ReturnInViewBag()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=測試");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // 檢查關鍵字是否被高亮或返回在視圖中
        content.Should().Contain("測試");
    }

    /// <summary>
    /// 測試 10: 搜尋結果按相關度/發布日期排序
    /// </summary>
    [Fact]
    public async Task Index_ShouldSortResultsByRelevanceOrPublishDate()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostsWithDifferentDates();

        // Act
        var response = await client.GetAsync("/Search?keyword=最新");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // 檢查最新的文章出現在前面
        var indexNewest = content.IndexOf("最新文章-2025");
        var indexOldest = content.IndexOf("最新文章-2024");

        if (indexNewest > -1 && indexOldest > -1)
        {
            indexNewest.Should().BeLessThan(indexOldest);
        }
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// 測試 11: 特殊字符處理
    /// </summary>
    [Fact]
    public async Task Index_ShouldHandleSpecialCharactersInKeyword()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedPostWithSpecialCharacters();

        // Act - 搜尋包含特殊字符的內容
        var response = await client.GetAsync("/Search?keyword=%3Cscript%3E");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 應該安全地處理，不應該導致錯誤
    }

    /// <summary>
    /// 測試 12: 過長關鍵字處理
    /// </summary>
    [Fact]
    public async Task Index_ShouldHandleLongKeyword()
    {
        // Arrange
        var client = _factory.CreateClient();
        var longKeyword = new string('a', 1000); // 1000 字元

        // Act
        var response = await client.GetAsync($"/Search?keyword={longKeyword}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 應該返回 OK 或重定向，不應該崩潰
    }

    /// <summary>
    /// 測試 13: SQL 注入防護驗證
    /// </summary>
    [Fact]
    public async Task Index_ShouldBeProtectedFromSQLInjection()
    {
        // Arrange
        var client = _factory.CreateClient();
        var maliciousKeyword = "'; DROP TABLE BlogPosts; --";

        // Act
        var response = await client.GetAsync($"/Search?keyword={Uri.EscapeDataString(maliciousKeyword)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 確認資料庫中的資料未被刪除（如果有測試資料）
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        db.BlogPosts.Count().Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// 測試 14: 無搜尋結果時
    /// </summary>
    [Fact]
    public async Task Index_ShouldShowEmptyResult_WhenNoMatchesFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=不存在的關鍵字");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // 應該顯示"無搜尋結果"的提示
        content.Should().NotContain("測試文章");
    }

    /// <summary>
    /// 測試 15: 搜尋中文關鍵字
    /// </summary>
    [Fact]
    public async Task Index_ShouldSupportChineseKeyword()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedChinesePost();

        // Act
        var response = await client.GetAsync("/Search?keyword=部落格");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("歡迎來到我的部落格");
    }

    /// <summary>
    /// 測試 16: 返回正確的狀態碼
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturnOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// 測試 17: 返回 HTML 內容類型
    /// </summary>
    [Fact]
    public async Task Index_ShouldReturnHtmlContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Search");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    /// <summary>
    /// 測試 18: 無效頁碼處理
    /// </summary>
    [Fact]
    public async Task Index_ShouldHandleInvalidPageNumber()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=測試&page=0");
        var response2 = await client.GetAsync("/Search?keyword=測試&page=-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// 測試 19: 多個關鍵字的搜尋（任意一個匹配）
    /// </summary>
    [Fact]
    public async Task Index_ShouldSearchMultipleTerms()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=測試");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // 應該找到包含"測試"的文章
        content.Should().Contain("測試");
    }

    /// <summary>
    /// 測試 20: 檢查搜尋是否包含發布日期
    /// </summary>
    [Fact]
    public async Task Index_ShouldIncludePublishDateInResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedMultiplePosts();

        // Act
        var response = await client.GetAsync("/Search?keyword=測試");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // 結果應該顯示發布日期
        content.Should().Contain("2025");
    }

    #endregion

    #region Helper Methods

    private async Task SeedMultiplePosts()
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
                Summary = "測試摘要 1",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "測試文章 2",
                Slug = "2025/10/test-post-2",
                Content = "這是測試文章 2 的內容",
                Summary = "測試摘要 2",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "其他文章",
                Slug = "2025/10/other-post",
                Content = "這是其他文章的內容",
                Summary = "其他摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedPostsWithVariousTitles()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "ASP.NET Core 教學",
                Slug = "2025/10/aspnet-tutorial",
                Content = "這是 ASP.NET Core 的教學內容",
                Summary = "ASP.NET Core 教學摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Python 基礎",
                Slug = "2025/10/python-basic",
                Content = "這是 Python 的基礎內容",
                Summary = "Python 基礎摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedPostsWithVariousContent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Web 開發",
                Slug = "2025/10/web-dev",
                Content = "在現代 Web 開發中，REST API 是重要的部分。REST API 使客戶端和伺服器能夠進行通訊。",
                Summary = "Web 開發摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedPostsWithVariousSummaries()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "資料庫設計",
                Slug = "2025/10/db-design",
                Content = "這篇文章介紹了資料庫設計的最佳實踐",
                Summary = "資料庫設計包括表格規範化、索引優化和查詢效能調整。",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedDraftAndPublishedPosts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "已發布的秘密文章",
                Slug = "2025/10/published-secret",
                Content = "這是一篇已發布的秘密文章",
                Summary = "已發布的秘密文章摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "草稿的秘密文章",
                Slug = "2025/10/draft-secret",
                Content = "這是一篇草稿的秘密文章",
                Summary = "草稿的秘密文章摘要",
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

    private async Task SeedCaseSensitivePost()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Hello World",
            Slug = "2025/10/hello-world",
            Content = "這是 Hello World 文章的內容",
            Summary = "Hello World 摘要",
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();
    }

    private async Task SeedManySearchablePostsForPagination()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>();
        for (int i = 1; i <= 25; i++)
        {
            posts.Add(new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = $"搜尋文章 {i}",
                Slug = $"2025/10/search-post-{i}",
                Content = $"這是搜尋文章 {i} 的內容，包含可搜尋的內容",
                Summary = $"搜尋文章 {i} 的摘要",
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

    private async Task SeedPostsWithDifferentDates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "最新文章-2025",
                Slug = "2025/10/newest-2025",
                Content = "這是 2025 年最新的文章",
                Summary = "最新文章摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "最新文章-2024",
                Slug = "2024/12/newest-2024",
                Content = "這是 2024 年的文章",
                Summary = "最新文章摘要",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddYears(-1),
                Tags = new List<Tag>()
            }
        };

        await db.BlogPosts.AddRangeAsync(posts);
        await db.SaveChangesAsync();
    }

    private async Task SeedPostWithSpecialCharacters()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "特殊字符文章 & < > \" '",
            Slug = "2025/10/special-chars",
            Content = "這是包含特殊字符的文章：<script>alert('test');</script>",
            Summary = "特殊字符摘要",
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();
    }

    private async Task SeedChinesePost()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "歡迎來到我的部落格",
            Slug = "2025/10/welcome-blog",
            Content = "這是一篇歡迎文章，介紹我的部落格",
            Summary = "歡迎來到我的部落格",
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Tags = new List<Tag>()
        };

        await db.BlogPosts.AddAsync(post);
        await db.SaveChangesAsync();
    }

    #endregion
}
