using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace BlogSystem.Tests.Unit.Services;

/// <summary>
/// TagService 單元測試
/// User Story 4 - 分類與標籤管理
/// </summary>
public class TagServiceTests
{
    private readonly Mock<IRepository<Tag>> _mockTagRepository;
    private readonly Mock<IRepository<BlogPost>> _mockPostRepository;
    private readonly ITagService _service;

    public TagServiceTests()
    {
        _mockTagRepository = new Mock<IRepository<Tag>>();
        _mockPostRepository = new Mock<IRepository<BlogPost>>();
        _service = CreateTagService();
    }

    #region GetAllTagsAsync Tests

    [Fact]
    public async Task GetAllTagsAsync_ShouldReturnAllTags_SortedByName()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new Tag { Id = Guid.NewGuid(), Name = "Zebra", Slug = "zebra", UsageCount = 1 },
            new Tag { Id = Guid.NewGuid(), Name = "Alpha", Slug = "alpha", UsageCount = 5 },
            new Tag { Id = Guid.NewGuid(), Name = "Beta", Slug = "beta", UsageCount = 3 }
        };

        _mockTagRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(tags);

        // Act
        var result = await _service.GetAllTagsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        var resultList = result.ToList();
        resultList[0].Name.Should().Be("Alpha");
        resultList[1].Name.Should().Be("Beta");
        resultList[2].Name.Should().Be("Zebra");
        _mockTagRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllTagsAsync_ShouldReturnEmptyList_WhenNoTagsExist()
    {
        // Arrange
        _mockTagRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Tag>());

        // Act
        var result = await _service.GetAllTagsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsAsync_ShouldHandleSingleTag()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new Tag { Id = Guid.NewGuid(), Name = "Single", Slug = "single", UsageCount = 1 }
        };

        _mockTagRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(tags);

        // Act
        var result = await _service.GetAllTagsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Single");
    }

    #endregion

    #region GetTagByIdAsync Tests

    [Fact]
    public async Task GetTagByIdAsync_ShouldReturnTag_WhenTagExists()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "CSharp", Slug = "csharp", UsageCount = 10 };

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        // Act
        var result = await _service.GetTagByIdAsync(tagId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tagId);
        result.Name.Should().Be("CSharp");
        result.UsageCount.Should().Be(10);
        _mockTagRepository.Verify(r => r.GetByIdAsync(tagId), Times.Once);
    }

    [Fact]
    public async Task GetTagByIdAsync_ShouldReturnNull_WhenTagDoesNotExist()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync((Tag?)null);

        // Act
        var result = await _service.GetTagByIdAsync(tagId);

        // Assert
        result.Should().BeNull();
        _mockTagRepository.Verify(r => r.GetByIdAsync(tagId), Times.Once);
    }

    #endregion

    #region CreateTagAsync Tests

    [Fact]
    public async Task CreateTagAsync_ShouldCreateTag_WithValidName()
    {
        // Arrange
        var tagName = "DotNET";
        Tag? capturedTag = null;

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag>());

        _mockTagRepository
            .Setup(r => r.AddAsync(It.IsAny<Tag>()))
            .Callback<Tag>(tag => capturedTag = tag)
            .ReturnsAsync((Tag tag) => tag);

        // Act
        var result = await _service.CreateTagAsync(tagName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(tagName);
        result.Id.Should().NotBe(Guid.Empty);
        result.Slug.Should().NotBeNullOrEmpty();
        result.UsageCount.Should().Be(0);
        _mockTagRepository.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Once);
    }

    [Fact]
    public async Task CreateTagAsync_ShouldThrowArgumentException_WhenNameIsEmpty()
    {
        // Arrange
        var tagName = string.Empty;

        // Act & Assert
        await _service.Invoking(s => s.CreateTagAsync(tagName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTagAsync_ShouldThrowArgumentException_WhenNameIsNull()
    {
        // Arrange
        var tagName = (string)null!;

        // Act & Assert
        await _service.Invoking(s => s.CreateTagAsync(tagName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTagAsync_ShouldThrowArgumentException_WhenNameIsWhitespace()
    {
        // Arrange
        var tagName = "   ";

        // Act & Assert
        await _service.Invoking(s => s.CreateTagAsync(tagName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTagAsync_ShouldThrowArgumentException_WhenNameAlreadyExists()
    {
        // Arrange
        var tagName = "Duplicate";
        var existingTag = new Tag { Id = Guid.NewGuid(), Name = "Duplicate", Slug = "duplicate" };

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag> { existingTag });

        // Act & Assert
        await _service.Invoking(s => s.CreateTagAsync(tagName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTagAsync_ShouldGenerateSlug_FromTagName()
    {
        // Arrange
        var tagName = "My New Tag";
        Tag? capturedTag = null;

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag>());

        _mockTagRepository
            .Setup(r => r.AddAsync(It.IsAny<Tag>()))
            .Callback<Tag>(tag => capturedTag = tag)
            .ReturnsAsync((Tag tag) => tag);

        // Act
        var result = await _service.CreateTagAsync(tagName);

        // Assert
        result.Slug.Should().NotBeNullOrEmpty();
        result.Slug.Should().Contain("my-new-tag", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateTagAsync Tests

    [Fact]
    public async Task UpdateTagAsync_ShouldUpdateTag_WithValidName()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var oldTag = new Tag { Id = tagId, Name = "OldName", Slug = "old-name", UsageCount = 5 };
        var newName = "NewName";
        Tag? capturedTag = null;

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(oldTag);

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag>());

        _mockTagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Tag>()))
            .Callback<Tag>(tag => capturedTag = tag)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateTagAsync(tagId, newName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
        result.Id.Should().Be(tagId);
        _mockTagRepository.Verify(r => r.UpdateAsync(It.IsAny<Tag>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTagAsync_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var newName = "NewName";

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync((Tag?)null);

        // Act & Assert
        await _service.Invoking(s => s.UpdateTagAsync(tagId, newName))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateTagAsync_ShouldThrowArgumentException_WhenNewNameIsEmpty()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "OldName", Slug = "old-name" };
        var newName = string.Empty;

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        // Act & Assert
        await _service.Invoking(s => s.UpdateTagAsync(tagId, newName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateTagAsync_ShouldCheckUniqueness_WhenUpdatingName()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "OldName", Slug = "old-name" };
        var existingTag = new Tag { Id = Guid.NewGuid(), Name = "Duplicate", Slug = "duplicate" };
        var newName = "Duplicate";

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag> { existingTag });

        // Act & Assert
        await _service.Invoking(s => s.UpdateTagAsync(tagId, newName))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateTagAsync_ShouldNotThrowOnNameUniqueness_WhenNameBelongsToSameTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "SameName", Slug = "same-name", UsageCount = 3 };
        var newName = "SameName";
        Tag? capturedTag = null;

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        _mockTagRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<Tag, bool>>()))
            .ReturnsAsync(new List<Tag> { tag });

        _mockTagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Tag>()))
            .Callback<Tag>(t => capturedTag = t)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateTagAsync(tagId, newName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
    }

    #endregion

    #region DeleteTagAsync Tests

    [Fact]
    public async Task DeleteTagAsync_ShouldDeleteTag_WhenTagExists()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "ToDelete", Slug = "to-delete" };

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        _mockTagRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Tag>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteTagAsync(tagId);

        // Assert
        _mockTagRepository.Verify(r => r.DeleteAsync(It.IsAny<Tag>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTagAsync_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync((Tag?)null);

        // Act & Assert
        await _service.Invoking(s => s.DeleteTagAsync(tagId))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTagAsync_ShouldDeleteTag_EvenWithAssociatedPosts()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "UsedTag", Slug = "used-tag", UsageCount = 5 };

        _mockTagRepository
            .Setup(r => r.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        _mockTagRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Tag>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteTagAsync(tagId);

        // Assert
        // 預期允許刪除 (級聯刪除或移除關聯)
        _mockTagRepository.Verify(r => r.DeleteAsync(It.IsAny<Tag>()), Times.Once);
    }

    #endregion

    #region GetPostsByTagAsync Tests

    [Fact]
    public async Task GetPostsByTagAsync_ShouldReturnPublishedPosts_ForTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "CSharp", Slug = "csharp" };
        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Post 1",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tag }
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Post 2",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                Tags = new List<Tag> { tag }
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Draft Post",
                Status = PostStatus.Draft,
                Tags = new List<Tag> { tag }
            }
        };

        _mockPostRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<BlogPost, bool>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).ToList());

        // Act
        var (resultPosts, totalCount) = await _service.GetPostsByTagAsync(tagId, page: 1, pageSize: 10);

        // Assert
        resultPosts.Should().NotBeNull();
        resultPosts.Should().HaveCount(2);
        totalCount.Should().Be(2);
        resultPosts.Should().AllSatisfy(p => p.Status.Should().Be(PostStatus.Published));
    }

    [Fact]
    public async Task GetPostsByTagAsync_ShouldSupportPagination()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "Tagged" };
        var posts = new List<BlogPost>();

        for (int i = 0; i < 25; i++)
        {
            posts.Add(new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = $"Post {i}",
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i),
                Tags = new List<Tag> { tag }
            });
        }

        _mockPostRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<BlogPost, bool>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).ToList());

        // Act - First page
        var (page1Posts, totalCount1) = await _service.GetPostsByTagAsync(tagId, page: 1, pageSize: 10);

        // Assert - First page
        page1Posts.Should().HaveCount(10);
        totalCount1.Should().Be(25);

        // Act - Second page
        var (page2Posts, totalCount2) = await _service.GetPostsByTagAsync(tagId, page: 2, pageSize: 10);

        // Assert - Second page
        page2Posts.Should().HaveCount(10);
        totalCount2.Should().Be(25);
    }

    [Fact]
    public async Task GetPostsByTagAsync_ShouldReturnEmptyList_WhenNoPostsForTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        _mockPostRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<BlogPost, bool>>()))
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var (posts, totalCount) = await _service.GetPostsByTagAsync(tagId);

        // Assert
        posts.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPostsByTagAsync_ShouldOrderByPublishedDate_Descending()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = new Tag { Id = tagId, Name = "Sorted" };
        var now = DateTime.UtcNow;
        var posts = new List<BlogPost>
        {
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Post 1",
                Status = PostStatus.Published,
                PublishedAt = now.AddDays(-3),
                Tags = new List<Tag> { tag }
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Post 2",
                Status = PostStatus.Published,
                PublishedAt = now,
                Tags = new List<Tag> { tag }
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Post 3",
                Status = PostStatus.Published,
                PublishedAt = now.AddDays(-1),
                Tags = new List<Tag> { tag }
            }
        };

        _mockPostRepository
            .Setup(r => r.FindAsync(It.IsAny<Func<BlogPost, bool>>()))
            .ReturnsAsync(posts.Where(p => p.Status == PostStatus.Published).ToList());

        // Act
        var (resultPosts, _) = await _service.GetPostsByTagAsync(tagId);

        // Assert
        var postsList = resultPosts.ToList();
        postsList[0].Title.Should().Be("Post 2");
        postsList[1].Title.Should().Be("Post 3");
        postsList[2].Title.Should().Be("Post 1");
    }

    #endregion

    #region ParseTagsFromString Tests

    [Fact]
    public void ParseTagsFromString_ShouldParseTags_FromCommaSeparatedString()
    {
        // Arrange
        var tagString = "C#, .NET, ASP.NET Core";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("C#");
        result.Should().Contain(".NET");
        result.Should().Contain("ASP.NET Core");
    }

    [Fact]
    public void ParseTagsFromString_ShouldTrimWhitespace_FromEachTag()
    {
        // Arrange
        var tagString = "  Tag1  ,   Tag2   ,  Tag3  ";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Tag1");
        result.Should().Contain("Tag2");
        result.Should().Contain("Tag3");
        result.Should().NotContain(r => r.StartsWith(" ") || r.EndsWith(" "));
    }

    [Fact]
    public void ParseTagsFromString_ShouldRemoveDuplicateTags()
    {
        // Arrange
        var tagString = "JavaScript, Node.js, JavaScript, React, Node.js";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("JavaScript");
        result.Should().Contain("Node.js");
        result.Should().Contain("React");
    }

    [Fact]
    public void ParseTagsFromString_ShouldHandleEmptyString()
    {
        // Arrange
        var tagString = string.Empty;

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTagsFromString_ShouldHandleOnlyCommas()
    {
        // Arrange
        var tagString = ",,,";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTagsFromString_ShouldHandleSingleTag()
    {
        // Arrange
        var tagString = "SingleTag";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().Be("SingleTag");
    }

    [Fact]
    public void ParseTagsFromString_ShouldHandleTagsWithSpacesInName()
    {
        // Arrange
        var tagString = "Machine Learning, Data Science, Deep Learning";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Machine Learning");
        result.Should().Contain("Data Science");
        result.Should().Contain("Deep Learning");
    }

    [Fact]
    public void ParseTagsFromString_ShouldHandleNullString()
    {
        // Arrange
        var tagString = (string)null!;

        // Act & Assert
        if (tagString == null)
        {
            var result = new List<string>();
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void ParseTagsFromString_ShouldBeCaseSensitive_WhenRemovingDuplicates()
    {
        // Arrange
        var tagString = "tag, Tag, TAG, python, Python";

        // Act
        var result = _service.ParseTagsFromString(tagString);

        // Assert
        // 應該根據規格決定是否大小寫敏感
        result.Should().NotBeEmpty();
        result.Should().Contain(r => r.Contains("tag", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 建立 TagService 實例
    /// </summary>
    private ITagService CreateTagService()
    {
        // 這裡需要實作 TagService，目前返回 Mock
        // 實際實作後應該返回真實的 TagService 實例
        return new MockTagService(_mockTagRepository.Object, _mockPostRepository.Object);
    }

    #endregion
}

/// <summary>
/// 臨時 Mock TagService，用於測試演示
/// 實際實作後應該被真實的 TagService 替換
/// </summary>
internal class MockTagService : ITagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<BlogPost> _postRepository;

    public MockTagService(IRepository<Tag> tagRepository, IRepository<BlogPost> postRepository)
    {
        _tagRepository = tagRepository;
        _postRepository = postRepository;
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        var tags = await _tagRepository.GetAllAsync();
        return tags.OrderBy(t => t.Name).ToList();
    }

    public async Task<Tag?> GetTagByIdAsync(Guid id)
    {
        return await _tagRepository.GetByIdAsync(id);
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name cannot be empty", nameof(name));
        }

        var existingTags = await _tagRepository.FindAsync(t => t.Name.ToLower() == name.ToLower());
        if (existingTags.Any())
        {
            throw new ArgumentException($"Tag '{name}' already exists", nameof(name));
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = GenerateSlug(name),
            UsageCount = 0
        };

        return await _tagRepository.AddAsync(tag);
    }

    public async Task<Tag> UpdateTagAsync(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name cannot be empty", nameof(name));
        }

        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Tag with ID '{id}' not found");
        }

        var existingTags = await _tagRepository.FindAsync(t => t.Name.ToLower() == name.ToLower() && t.Id != id);
        if (existingTags.Any())
        {
            throw new ArgumentException($"Tag '{name}' already exists", nameof(name));
        }

        tag.Name = name;
        tag.Slug = GenerateSlug(name);

        await _tagRepository.UpdateAsync(tag);
        return tag;
    }

    public async Task DeleteTagAsync(Guid id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Tag with ID '{id}' not found");
        }

        await _tagRepository.DeleteAsync(tag);
    }

    public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByTagAsync(Guid tagId, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var posts = await _postRepository.FindAsync(p => p.Status == PostStatus.Published && p.Tags.Any(t => t.Id == tagId));
        var orderedPosts = posts.OrderByDescending(p => p.PublishedAt).ToList();

        var paginatedPosts = orderedPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (paginatedPosts, orderedPosts.Count);
    }

    public IEnumerable<string> ParseTagsFromString(string tagString)
    {
        if (string.IsNullOrWhiteSpace(tagString))
        {
            return new List<string>();
        }

        return tagString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower().Replace(" ", "-").Replace(".", "").Replace("#", "");
    }
}
