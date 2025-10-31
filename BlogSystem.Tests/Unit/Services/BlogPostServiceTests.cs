using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using BlogSystem.Core.Services;
using FluentAssertions;
using Moq;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// BlogPostService 單元測試
/// </summary>
public class BlogPostServiceTests
{
    private readonly Mock<IRepository<BlogPost>> _mockRepository;
    private readonly BlogPostService _service;

    public BlogPostServiceTests()
    {
        _mockRepository = new Mock<IRepository<BlogPost>>();
        _service = new BlogPostService(_mockRepository.Object);
    }

    #region GetPublishedPostsAsync Tests

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldReturnPublishedPosts_WithPagination()
    {
        // Arrange
        var posts = CreateTestPosts(15);
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync(page: 1, pageSize: 10);

        // Assert
        resultPosts.Should().NotBeNull();
        resultPosts.Should().HaveCount(10);
        totalCount.Should().Be(15);
        resultPosts.First().PublishedAt.Should().BeAfter(resultPosts.Last().PublishedAt!.Value);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldReturnSecondPage_WhenPageIs2()
    {
        // Arrange
        var posts = CreateTestPosts(15);
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync(page: 2, pageSize: 10);

        // Assert
        resultPosts.Should().HaveCount(5);
        totalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldNormalizePage_WhenPageIsLessThan1()
    {
        // Arrange
        var posts = CreateTestPosts(5);
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync(page: 0, pageSize: 10);

        // Assert
        resultPosts.Should().HaveCount(5);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldNormalizePageSize_WhenPageSizeIsLessThan1()
    {
        // Arrange
        var posts = CreateTestPosts(15);
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync(page: 1, pageSize: 0);

        // Assert
        resultPosts.Should().HaveCount(10);
        totalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldLimitPageSize_WhenPageSizeExceeds100()
    {
        // Arrange
        var posts = CreateTestPosts(150);
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync(page: 1, pageSize: 150);

        // Assert
        resultPosts.Should().HaveCount(100);
        totalCount.Should().Be(150);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ShouldExcludeDraftPosts()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("Post 1", PostStatus.Published),
            CreateBlogPost("Post 2", PostStatus.Draft),
            CreateBlogPost("Post 3", PostStatus.Published)
        };
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).AsQueryable());

        // Act
        var (resultPosts, totalCount) = await _service.GetPublishedPostsAsync();

        // Assert
        resultPosts.Should().HaveCount(2);
        totalCount.Should().Be(2);
        resultPosts.Should().OnlyContain(p => p.Status == PostStatus.Published);
    }

    #endregion

    #region GetPostBySlugAsync Tests

    [Fact]
    public async Task GetPostBySlugAsync_ShouldReturnPost_WhenSlugExists()
    {
        // Arrange
        var slug = "test-article";
        var fullSlug = "2025/10/test-article";
        var post = CreateBlogPost("Test Article", PostStatus.Published, fullSlug);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(new[] { post }.AsQueryable());

        // Act
        var result = await _service.GetPostBySlugAsync(2025, 10, slug);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(fullSlug);
        result.Title.Should().Be("Test Article");
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldReturnNull_WhenSlugDoesNotExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var result = await _service.GetPostBySlugAsync(2025, 10, "non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldReturnNull_WhenSlugIsEmpty()
    {
        // Act
        var result = await _service.GetPostBySlugAsync(2025, 10, "");

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldReturnNull_WhenSlugIsWhitespace()
    {
        // Act
        var result = await _service.GetPostBySlugAsync(2025, 10, "   ");

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldFormatSlugCorrectly_WithMonthPadding()
    {
        // Arrange
        var slug = "test-article";
        var expectedFullSlug = "2025/05/test-article"; // Month should be padded with zero
        var post = CreateBlogPost("Test Article", PostStatus.Published, expectedFullSlug);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(new[] { post }.AsQueryable());

        // Act
        var result = await _service.GetPostBySlugAsync(2025, 5, slug);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(expectedFullSlug);
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldOnlyReturnPublishedPosts()
    {
        // Arrange
        var slug = "test-article";
        var fullSlug = "2025/10/test-article";
        var draftPost = CreateBlogPost("Test Article", PostStatus.Draft, fullSlug);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<BlogPost>().AsQueryable());

        // Act
        var result = await _service.GetPostBySlugAsync(2025, 10, slug);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IncrementViewCountAsync Tests

    [Fact]
    public async Task IncrementViewCountAsync_ShouldIncreaseViewCount_WhenPostExists()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateBlogPost("Test Post", PostStatus.Published);
        post.ViewCount = 5;

        _mockRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        await _service.IncrementViewCountAsync(postId);

        // Assert
        post.ViewCount.Should().Be(6);
        _mockRepository.Verify(r => r.UpdateAsync(post), Times.Once);
    }

    [Fact]
    public async Task IncrementViewCountAsync_ShouldNotThrow_WhenPostDoesNotExist()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync((BlogPost?)null);

        // Act
        var act = async () => await _service.IncrementViewCountAsync(postId);

        // Assert
        await act.Should().NotThrowAsync();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<BlogPost>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private List<BlogPost> CreateTestPosts(int count)
    {
        var posts = new List<BlogPost>();
        for (int i = 0; i < count; i++)
        {
            posts.Add(CreateBlogPost($"Post {i + 1}", PostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-i)));
        }
        return posts;
    }

    private BlogPost CreateBlogPost(
        string title,
        PostStatus status,
        string? slug = null,
        DateTime? publishedAt = null)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug ?? $"2025/10/{title.ToLower().Replace(" ", "-")}",
            Content = $"Content for {title}",
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
