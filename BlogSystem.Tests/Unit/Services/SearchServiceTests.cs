using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// SearchService 單元測試
/// User Story 5: 訪客能夠搜尋文章
///
/// 測試搜尋邏輯和商業規則：
/// - 標題、內容、摘要關鍵字搜尋
/// - 只返回已發布的文章
/// - 大小寫不敏感搜尋
/// - 分頁支援
/// - 排序邏輯
/// </summary>
public class SearchServiceTests
{
    private readonly Mock<IRepository<BlogPost>> _mockRepository;
    private readonly ISearchService _service;

    public SearchServiceTests()
    {
        _mockRepository = new Mock<IRepository<BlogPost>>();
        _service = new SearchService(_mockRepository.Object);
    }

    #region SearchAsync - 基本搜尋功能

    [Fact]
    public async Task SearchAsync_ShouldReturnArticles_WhenKeywordMatchesTitleSubstring()
    {
        // Arrange
        var keyword = "Entity Framework";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Entity Framework Tutorial", PostStatus.Published, "ef-tutorial", "Learn EF"),
            CreateBlogPost("Advanced Entity Framework Tips", PostStatus.Published, "ef-tips", "EF advanced"),
            CreateBlogPost("LINQ Basics", PostStatus.Published, "linq-basics", "LINQ guide"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
        results.Should().AllSatisfy(p => p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnArticles_WhenKeywordMatchesContentSubstring()
    {
        // Arrange
        var keyword = "Async/Await";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("C# Async Patterns", PostStatus.Published, "c-async", "In this guide, we explore Async/Await patterns"),
            CreateBlogPost("Database Optimization", PostStatus.Published, "db-opt", "Use Async/Await for I/O operations"),
            CreateBlogPost("REST API Design", PostStatus.Published, "rest-api", "Design patterns for web services"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
        results.Should().AllSatisfy(p => p.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnArticles_WhenKeywordMatchesSummarySubstring()
    {
        // Arrange
        var keyword = "Docker";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Containerization Guide", PostStatus.Published, "docker-guide", "Using Docker containers", "Docker is a containerization platform"),
            CreateBlogPost("Kubernetes Basics", PostStatus.Published, "k8s-basics", "Container orchestration", "Kubernetes manages Docker containers"),
            CreateBlogPost("CI/CD Pipeline", PostStatus.Published, "cicd-pipeline", "Continuous integration setup", "Deploy to production servers"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
        results.Should().AllSatisfy(p => p.Summary != null && p.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region SearchAsync - 狀態過濾

    [Fact]
    public async Task SearchAsync_ShouldOnlyReturnPublishedArticles()
    {
        // Arrange
        var keyword = "C#";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("C# Guide", PostStatus.Published, "csharp-guide", "C# programming guide"),
            CreateBlogPost("C# Advanced", PostStatus.Draft, "csharp-advanced", "C# advanced topics"),
            CreateBlogPost("C# Patterns", PostStatus.Published, "csharp-patterns", "Design patterns in C#"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
        results.Should().OnlyContain(p => p.Status == PostStatus.Published);
    }

    #endregion

    #region SearchAsync - 大小寫不敏感

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitive_WhenSearchingTitle()
    {
        // Arrange
        var keyword = "typescript";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("TypeScript Tutorial", PostStatus.Published, "ts-tutorial", "Learn TypeScript"),
            CreateBlogPost("TypeScript Best Practices", PostStatus.Published, "ts-best", "Best practices"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitive_WhenSearchingContent()
    {
        // Arrange
        var keyword = "REACT";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Frontend Framework", PostStatus.Published, "frontend", "React is a JavaScript library"),
            CreateBlogPost("State Management", PostStatus.Published, "state-mgmt", "Managing React state"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
    }

    #endregion

    #region SearchAsync - 分頁

    [Fact]
    public async Task SearchAsync_ShouldSupportPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(25, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 1, pageSize: 10);

        // Assert
        results.Should().HaveCount(10);
        totalCount.Should().Be(25);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnSecondPage_WhenPageIs2()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(25, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 2, pageSize: 10);

        // Assert
        results.Should().HaveCount(10);
        totalCount.Should().Be(25);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnRemainingItems_WhenPageExceedsLastFullPage()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(25, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 3, pageSize: 10);

        // Assert
        results.Should().HaveCount(5);
        totalCount.Should().Be(25);
    }

    [Fact]
    public async Task SearchAsync_ShouldNormalizePage_WhenPageIsLessThan1()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(10, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 0, pageSize: 10);

        // Assert
        results.Should().HaveCount(10);
        totalCount.Should().Be(10);
    }

    [Fact]
    public async Task SearchAsync_ShouldNormalizePageSize_WhenPageSizeIsLessThan1()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(10, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 1, pageSize: 0);

        // Assert
        results.Should().HaveCount(10);
        totalCount.Should().Be(10);
    }

    [Fact]
    public async Task SearchAsync_ShouldLimitPageSize_WhenPageSizeExceedsMaximum()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(150, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 1, pageSize: 200);

        // Assert
        results.Should().HaveCount(100);
        totalCount.Should().Be(150);
    }

    #endregion

    #region SearchAsync - 空值和無效輸入

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyResult_WhenKeywordIsEmpty()
    {
        // Arrange
        var keyword = "";
        var posts = CreateTestPosts(5, "C#");

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyResult_WhenKeywordIsNull()
    {
        // Arrange
        var keyword = (string)null!;

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyResult_WhenKeywordIsWhitespace()
    {
        // Arrange
        var keyword = "   ";

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoMatchingResults()
    {
        // Arrange
        var keyword = "Nonexistent Topic";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("C# Guide", PostStatus.Published, "c-guide", "C# programming"),
            CreateBlogPost("JavaScript Tips", PostStatus.Published, "js-tips", "JS programming"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    #endregion

    #region SearchAsync - 排序

    [Fact]
    public async Task SearchAsync_ShouldSortByPublishedDate_InDescendingOrder()
    {
        // Arrange
        var keyword = "Article";
        var baseDate = DateTime.UtcNow;
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Article One", PostStatus.Published, "article-1", "Article content", publishedAt: baseDate.AddDays(-2)),
            CreateBlogPost("Article Two", PostStatus.Published, "article-2", "Article content", publishedAt: baseDate),
            CreateBlogPost("Article Three", PostStatus.Published, "article-3", "Article content", publishedAt: baseDate.AddDays(-1)),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        var resultList = results.ToList();
        resultList.Should().HaveCount(3);
        resultList[0].Id.Should().Be(posts[1].Id); // Most recent
        resultList[2].Id.Should().Be(posts[0].Id); // Oldest
    }

    [Fact]
    public async Task SearchAsync_ShouldPrioritizeTitleMatches_OverContentMatches()
    {
        // Arrange
        var keyword = "Docker";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Kubernetes Guide", PostStatus.Published, "k8s", "Docker containers are used in Kubernetes"),
            CreateBlogPost("Docker Tutorial", PostStatus.Published, "docker", "Docker basics"),
            CreateBlogPost("Container Orchestration", PostStatus.Published, "container", "Docker containers for orchestration"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        var resultList = results.ToList();
        resultList.Should().HaveCount(3);
        // Title match should come first
        resultList.First().Title.Should().Contain(keyword);
    }

    #endregion

    #region SearchAsync - 特殊字符處理

    [Fact]
    public async Task SearchAsync_ShouldHandleSqlSpecialCharacters_WhenSearchingKeyword()
    {
        // Arrange
        var keyword = "'; DROP TABLE--";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("SQL Security", PostStatus.Published, "sql-sec", "Protect against SQL injection"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldHandleWildcardCharacters_InKeyword()
    {
        // Arrange
        var keyword = "%test%";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Unit Testing", PostStatus.Published, "unit-test", "Testing best practices"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        // Wildcard characters should be treated as literals or escaped
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldHandleUnicodeCharacters_InKeyword()
    {
        // Arrange
        var keyword = "資料";
        var posts = new List<BlogPost>
        {
            CreateBlogPost("資料庫優化", PostStatus.Published, "db-opt", "如何優化資料庫查詢"),
            CreateBlogPost("資料結構", PostStatus.Published, "data-struct", "資料結構與演算法"),
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword);

        // Assert
        results.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    #endregion

    #region SearchAsync - 返回值驗證

    [Fact]
    public async Task SearchAsync_ShouldReturnTotalCount_IncludingNonPagedResults()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(35, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 1, pageSize: 10);

        // Assert
        totalCount.Should().Be(35);
        results.Should().HaveCount(10);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyResults_WhenPageExceedsTotalPages()
    {
        // Arrange
        var keyword = "API";
        var posts = CreateTestPosts(15, keyword);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.AsQueryable());

        // Act
        var (results, totalCount) = await _service.SearchAsync(keyword, page: 10, pageSize: 10);

        // Assert
        results.Should().BeEmpty();
        totalCount.Should().Be(15);
    }

    #endregion

    #region Helper Methods

    private List<BlogPost> CreateTestPosts(int count, string keyword)
    {
        var posts = new List<BlogPost>();
        for (int i = 0; i < count; i++)
        {
            posts.Add(CreateBlogPost(
                $"{keyword} Article {i + 1}",
                PostStatus.Published,
                $"article-{i + 1}",
                $"This is {keyword} content for article {i + 1}",
                publishedAt: DateTime.UtcNow.AddDays(-i)
            ));
        }
        return posts;
    }

    private BlogPost CreateBlogPost(
        string title,
        PostStatus status,
        string slug,
        string content,
        string? summary = null,
        DateTime? publishedAt = null)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = content,
            Summary = summary ?? $"Summary of {title}",
            Status = status,
            PublishedAt = publishedAt ?? (status == PostStatus.Published ? DateTime.UtcNow : null),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            Tags = new List<Tag>()
        };
    }

    #endregion
}
