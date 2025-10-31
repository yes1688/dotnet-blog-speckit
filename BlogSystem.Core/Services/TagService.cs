using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlogSystem.Core.Services
{
    /// <summary>
    /// 標籤服務實作
    /// </summary>
    public class TagService : ITagService
    {
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<BlogPost> _blogPostRepository;

        public TagService(IRepository<Tag> tagRepository, IRepository<BlogPost> blogPostRepository)
        {
            _tagRepository = tagRepository;
            _blogPostRepository = blogPostRepository;
        }

        /// <summary>
        /// 取得所有標籤 (按名稱排序)
        /// </summary>
        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            var tags = await _tagRepository.GetAllAsync();
            return tags.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// 根據 ID 取得標籤
        /// </summary>
        public async Task<Tag?> GetTagByIdAsync(Guid id)
        {
            return await _tagRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 建立新標籤
        /// </summary>
        public async Task<Tag> CreateTagAsync(string name)
        {
            // 驗證名稱
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tag name cannot be empty.", nameof(name));
            }

            var trimmedName = name.Trim();

            // 檢查名稱唯一性
            var existingTags = await _tagRepository.FindAsync(t =>
                t.Name.ToLower() == trimmedName.ToLower());

            if (existingTags.Any())
            {
                throw new ArgumentException("Tag name already exists.", nameof(name));
            }

            // 建立新標籤
            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                Name = trimmedName,
                Slug = GenerateSlug(trimmedName),
                UsageCount = 0
            };

            return await _tagRepository.AddAsync(tag);
        }

        /// <summary>
        /// 更新標籤
        /// </summary>
        public async Task<Tag> UpdateTagAsync(Guid id, string newName)
        {
            // 查找現有標籤
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException("Tag not found.");
            }

            var trimmedName = newName.Trim();

            // 驗證名稱
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                throw new ArgumentException("Tag name cannot be empty.", nameof(newName));
            }

            // 檢查名稱唯一性 (排除當前標籤)
            var existingTags = await _tagRepository.FindAsync(t =>
                t.Name.ToLower() == trimmedName.ToLower() && t.Id != id);

            if (existingTags.Any())
            {
                throw new ArgumentException("Tag name already exists.", nameof(newName));
            }

            // 更新標籤屬性
            tag.Name = trimmedName;
            tag.Slug = GenerateSlug(trimmedName);

            await _tagRepository.UpdateAsync(tag);

            return tag;
        }

        /// <summary>
        /// 刪除標籤
        /// </summary>
        public async Task DeleteTagAsync(Guid id)
        {
            // 查找現有標籤
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException("Tag not found.");
            }

            await _tagRepository.DeleteAsync(tag);
        }

        /// <summary>
        /// 根據標籤 ID 取得相關文章 (只返回已發布的)
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByTagAsync(
            Guid tagId, int page = 1, int pageSize = 10)
        {
            // 正規化分頁參數
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // 查找標籤
            var tag = await _tagRepository.GetByIdAsync(tagId);
            if (tag == null)
            {
                return (Enumerable.Empty<BlogPost>(), 0);
            }

            // 篩選已發布的文章並按發布日期降序排列
            var publishedPosts = tag.BlogPosts
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.PublishedAt)
                .ToList();

            var totalCount = publishedPosts.Count;

            // 應用分頁
            var pagedPosts = publishedPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedPosts, totalCount);
        }

        /// <summary>
        /// 解析逗號分隔的標籤字串為標籤名稱清單
        /// </summary>
        public IEnumerable<string> ParseTagsFromString(string tagString)
        {
            if (string.IsNullOrWhiteSpace(tagString))
            {
                return Enumerable.Empty<string>();
            }

            // 按逗號分割、去除前後空白、移除空值、去除重複
            var tags = tagString
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return tags;
        }

        /// <summary>
        /// 根據名稱產生 Slug (支援中文)
        /// </summary>
        private string GenerateSlug(string name)
        {
            // 轉換為小寫
            var slug = name.ToLower();

            // 移除非字母、數字、中文字符和連字符的字符
            // 保留中文字符 (\u4e00-\u9fff)
            slug = Regex.Replace(slug, @"[^\u4e00-\u9fffa-z0-9\s-]", "");

            // 用連字符替換空格和多個連字符
            slug = Regex.Replace(slug, @"[\s-]+", "-");

            // 移除前後連字符
            slug = slug.Trim('-');

            return slug;
        }
    }
}
