using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// CategoryService 單元測試
/// 根據 User Story 4 - 分類與標籤管理進行測試
/// </summary>
public class CategoryServiceTests
{
    private readonly Mock<IRepository<Category>> _mockRepository;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<Category>>();
        _service = new CategoryService(_mockRepository.Object);
    }

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories_OrderedByName()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Technology", Slug = "technology", DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), Name = "Lifestyle", Slug = "lifestyle", DisplayOrder = 3 },
            new() { Id = Guid.NewGuid(), Name = "Business", Slug = "business", DisplayOrder = 1 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories.OrderBy(c => c.Name).ToList());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.First().Name.Should().Be("Business");
        result.Last().Name.Should().Be("Technology");
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetCategoryByIdAsync Tests

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            Description = "Technology related posts",
            DisplayOrder = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(categoryId);
        result.Name.Should().Be("Technology");
        result.Slug.Should().Be("technology");
        _mockRepository.Verify(r => r.GetByIdAsync(categoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(categoryId), Times.Once);
    }

    #endregion

    #region CreateCategoryAsync Tests

    [Fact]
    public async Task CreateCategoryAsync_ShouldCreateCategory_WithValidData()
    {
        // Arrange
        var categoryName = "Technology";
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Slug = "technology",
            Description = "Technology posts",
            DisplayOrder = 1
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Category>().AsQueryable());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync(category);

        // Act
        var result = await _service.CreateCategoryAsync(categoryName, "Technology posts");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(categoryName);
        result.Slug.Should().Be("technology");
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldThrowException_WhenNameIsEmpty()
    {
        // Arrange
        var emptyName = string.Empty;

        // Act
        var act = async () => await _service.CreateCategoryAsync(emptyName, "Description");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldThrowException_WhenNameIsWhitespace()
    {
        // Arrange
        var whitespaceName = "   ";

        // Act
        var act = async () => await _service.CreateCategoryAsync(whitespaceName, "Description");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldThrowException_WhenNameAlreadyExists()
    {
        // Arrange
        var categoryName = "Technology";
        var existingCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Slug = "technology"
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(new[] { existingCategory }.AsQueryable());

        // Act
        var act = async () => await _service.CreateCategoryAsync(categoryName, "Description");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category name already exists*");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldTrimNameAndConvertToSlug()
    {
        // Arrange
        var categoryName = "  Advanced Technology  ";
        var trimmedName = "Advanced Technology";
        var expectedSlug = "advanced-technology";

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Category>().AsQueryable());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Category>()))
            .Returns(Task.FromResult(new Category
            {
                Id = Guid.NewGuid(),
                Name = trimmedName,
                Slug = expectedSlug,
                DisplayOrder = 1
            }));

        // Act
        var result = await _service.CreateCategoryAsync(categoryName, "Description");

        // Assert
        result.Name.Should().Be(trimmedName);
        result.Slug.Should().Be(expectedSlug);
    }

    #endregion

    #region UpdateCategoryAsync Tests

    [Fact]
    public async Task UpdateCategoryAsync_ShouldUpdateCategory_WithValidData()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Old Name",
            Slug = "old-name",
            Description = "Old description",
            DisplayOrder = 1
        };

        var updatedCategory = new Category
        {
            Id = categoryId,
            Name = "New Name",
            Slug = "new-name",
            Description = "New description",
            DisplayOrder = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Category>().AsQueryable());

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCategoryAsync(categoryId, "New Name", "New description");

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(categoryId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldThrowException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var act = async () => await _service.UpdateCategoryAsync(categoryId, "New Name", "New description");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category not found*");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldThrowException_WhenNewNameAlreadyExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var anotherCategoryId = Guid.NewGuid();

        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Old Name",
            Slug = "old-name"
        };

        var duplicateCategory = new Category
        {
            Id = anotherCategoryId,
            Name = "New Name",
            Slug = "new-name"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(new[] { duplicateCategory }.AsQueryable());

        // Act
        var act = async () => await _service.UpdateCategoryAsync(categoryId, "New Name", "Description");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category name already exists*");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldAllowSameName_WhenUpdatingExistingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            Description = "Old description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(new[] { existingCategory }.AsQueryable()); // Same category returned

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCategoryAsync(categoryId, "Technology", "New description");

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_ShouldDeleteCategory_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = new List<BlogPost>()
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _mockRepository
            .Setup(r => r.DeleteAsync(category))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteCategoryAsync(categoryId);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(categoryId), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(category), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldThrowException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var act = async () => await _service.DeleteCategoryAsync(categoryId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category not found*");
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldThrowException_WhenCategoryHasAssociatedPosts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var blogPost = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Test Post",
            Slug = "test-post",
            Content = "Content",
            Status = PostStatus.Published
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = new List<BlogPost> { blogPost }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var act = async () => await _service.DeleteCategoryAsync(categoryId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot delete category with associated posts*");
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldAllowDeletion_WhenCategoryHasNoAssociatedPosts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = new List<BlogPost>()
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _mockRepository
            .Setup(r => r.DeleteAsync(category))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteCategoryAsync(categoryId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(category), Times.Once);
    }

    #endregion

    #region GetPostsByCategoryAsync Tests

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldReturnPublishedPosts_OrderedByPublishedDate()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var posts = new List<BlogPost>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Post 1",
                Slug = "post-1",
                Content = "Content 1",
                Status = PostStatus.Published,
                PublishedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Post 2",
                Slug = "post-2",
                Content = "Content 2",
                Status = PostStatus.Published,
                PublishedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Post 3",
                Slug = "post-3",
                Content = "Content 3",
                Status = PostStatus.Draft,
                PublishedAt = null
            }
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = posts
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var (resultPosts, totalCount) = await _service.GetPostsByCategoryAsync(categoryId, page: 1, pageSize: 10);

        // Assert
        resultPosts.Should().NotBeNull();
        resultPosts.Should().HaveCount(2);
        totalCount.Should().Be(2);
        resultPosts.First().Title.Should().Be("Post 2"); // Most recent first
        resultPosts.Last().Title.Should().Be("Post 1");
        resultPosts.Should().OnlyContain(p => p.Status == PostStatus.Published);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldExcludeDraftPosts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var posts = new List<BlogPost>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Published Post",
                Slug = "published-post",
                Content = "Content",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Draft Post",
                Slug = "draft-post",
                Content = "Content",
                Status = PostStatus.Draft,
                PublishedAt = null
            }
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = posts
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var (resultPosts, totalCount) = await _service.GetPostsByCategoryAsync(categoryId);

        // Assert
        resultPosts.Should().HaveCount(1);
        totalCount.Should().Be(1);
        resultPosts.First().Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldSupportPagination()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var posts = new List<BlogPost>();
        for (int i = 0; i < 15; i++)
        {
            posts.Add(new()
            {
                Id = Guid.NewGuid(),
                Title = $"Post {i + 1}",
                Slug = $"post-{i + 1}",
                Content = "Content",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = posts
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act - Get first page
        var (firstPagePosts, totalCount) = await _service.GetPostsByCategoryAsync(categoryId, page: 1, pageSize: 10);

        // Assert
        firstPagePosts.Should().HaveCount(10);
        totalCount.Should().Be(15);

        // Act - Get second page
        var (secondPagePosts, _) = await _service.GetPostsByCategoryAsync(categoryId, page: 2, pageSize: 10);

        // Assert
        secondPagePosts.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldReturnEmptyList_WhenCategoryHasNoPosts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = new List<BlogPost>()
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var (resultPosts, totalCount) = await _service.GetPostsByCategoryAsync(categoryId);

        // Assert
        resultPosts.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldReturnEmptyList_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var (resultPosts, totalCount) = await _service.GetPostsByCategoryAsync(categoryId);

        // Assert
        resultPosts.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldNormalizePage_WhenPageIsLessThan1()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var posts = new List<BlogPost>();
        for (int i = 0; i < 5; i++)
        {
            posts.Add(new()
            {
                Id = Guid.NewGuid(),
                Title = $"Post {i + 1}",
                Slug = $"post-{i + 1}",
                Content = "Content",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow
            });
        }

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = posts
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var (resultPosts, _) = await _service.GetPostsByCategoryAsync(categoryId, page: 0, pageSize: 10);

        // Assert
        resultPosts.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ShouldNormalizePageSize_WhenPageSizeIsLessThan1()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var posts = new List<BlogPost>();
        for (int i = 0; i < 15; i++)
        {
            posts.Add(new()
            {
                Id = Guid.NewGuid(),
                Title = $"Post {i + 1}",
                Slug = $"post-{i + 1}",
                Content = "Content",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow
            });
        }

        var category = new Category
        {
            Id = categoryId,
            Name = "Technology",
            Slug = "technology",
            BlogPosts = posts
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var (resultPosts, _) = await _service.GetPostsByCategoryAsync(categoryId, page: 1, pageSize: 0);

        // Assert
        resultPosts.Should().HaveCount(10);
    }

    #endregion

    #region Helper Methods

    private CategoryService CreateCategoryService()
    {
        return new CategoryService(_mockRepository.Object);
    }

    #endregion
}
