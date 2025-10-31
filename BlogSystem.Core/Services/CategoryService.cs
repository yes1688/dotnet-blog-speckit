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
    /// 分類服務實作
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoryService(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        /// <summary>
        /// 取得所有分類 (按名稱排序)
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.OrderBy(c => c.Name).ToList();
        }

        /// <summary>
        /// 根據 ID 取得分類
        /// </summary>
        public async Task<Category?> GetCategoryByIdAsync(Guid id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 建立新分類
        /// </summary>
        public async Task<Category> CreateCategoryAsync(string name, string? description = null)
        {
            // 驗證名稱
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name cannot be empty.", nameof(name));
            }

            var trimmedName = name.Trim();

            // 檢查名稱唯一性
            var existingCategories = await _categoryRepository.FindAsync(c =>
                c.Name.ToLower() == trimmedName.ToLower());

            if (existingCategories.Any())
            {
                throw new InvalidOperationException("Category name already exists.");
            }

            // 建立新分類
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = trimmedName,
                Slug = GenerateSlug(trimmedName),
                Description = description,
                DisplayOrder = 0
            };

            return await _categoryRepository.AddAsync(category);
        }

        /// <summary>
        /// 更新分類
        /// </summary>
        public async Task UpdateCategoryAsync(Guid id, string name, string? description = null)
        {
            // 查找現有分類
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                throw new InvalidOperationException("Category not found.");
            }

            var trimmedName = name.Trim();

            // 檢查名稱唯一性 (排除當前分類)
            var existingCategories = await _categoryRepository.FindAsync(c =>
                c.Name.ToLower() == trimmedName.ToLower() && c.Id != id);

            if (existingCategories.Any())
            {
                throw new InvalidOperationException("Category name already exists.");
            }

            // 更新分類屬性
            category.Name = trimmedName;
            category.Slug = GenerateSlug(trimmedName);
            if (description != null)
            {
                category.Description = description;
            }

            await _categoryRepository.UpdateAsync(category);
        }

        /// <summary>
        /// 刪除分類
        /// </summary>
        public async Task DeleteCategoryAsync(Guid id)
        {
            // 查找現有分類
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                throw new InvalidOperationException("Category not found.");
            }

            // 檢查是否有關聯的文章
            if (category.BlogPosts != null && category.BlogPosts.Any())
            {
                throw new InvalidOperationException("Cannot delete category with associated posts.");
            }

            await _categoryRepository.DeleteAsync(category);
        }

        /// <summary>
        /// 根據分類 ID 取得相關文章 (只返回已發布的)
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByCategoryAsync(
            Guid categoryId, int page = 1, int pageSize = 10)
        {
            // 正規化分頁參數
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // 查找分類
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
            {
                return (Enumerable.Empty<BlogPost>(), 0);
            }

            // 篩選已發布的文章並按發布日期降序排列
            var publishedPosts = category.BlogPosts
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
        /// 根據名稱產生 Slug
        /// </summary>
        private string GenerateSlug(string name)
        {
            // 轉換為小寫
            var slug = name.ToLower();

            // 移除非字母、數字和連字符的字符
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // 用連字符替換空格和多個連字符
            slug = Regex.Replace(slug, @"[\s-]+", "-");

            // 移除前後連字符
            slug = slug.Trim('-');

            return slug;
        }
    }
}
