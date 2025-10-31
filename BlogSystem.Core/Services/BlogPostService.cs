using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlogSystem.Core.Services
{
    /// <summary>
    /// 部落格文章服務實作
    /// </summary>
    public class BlogPostService : IBlogPostService
    {
        private readonly IRepository<BlogPost> _postRepository;

        public BlogPostService(IRepository<BlogPost> postRepository)
        {
            _postRepository = postRepository;
        }

        /// <summary>
        /// 取得已發布文章列表 (分頁)
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPublishedPostsAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 最大限制

            // 取得已發布的文章,包含關聯的分類和標籤
            var query = await _postRepository.FindAsync(p => p.Status == PostStatus.Published);

            var posts = query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = query.Count(p => p.Status == PostStatus.Published);

            return (posts, totalCount);
        }

        /// <summary>
        /// 根據 slug 取得單篇文章
        /// </summary>
        public async Task<BlogPost?> GetPostBySlugAsync(int year, int month, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            // Slug 格式: {year}/{month}/{title-slug}
            var fullSlug = $"{year}/{month:D2}/{slug}";

            var posts = await _postRepository.FindAsync(p =>
                p.Slug == fullSlug &&
                p.Status == PostStatus.Published);

            return posts.FirstOrDefault();
        }

        /// <summary>
        /// 增加文章瀏覽次數
        /// </summary>
        public async Task IncrementViewCountAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post != null)
            {
                post.ViewCount++;
                await _postRepository.UpdateAsync(post);
            }
        }

        /// <summary>
        /// 根據標題生成唯一的 slug
        /// </summary>
        public async Task<string> GenerateSlugAsync(string title, DateTime? publishDate = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("標題不能為空", nameof(title));
            }

            // 使用當前時間或指定的發布時間
            var date = publishDate ?? DateTime.UtcNow;
            var year = date.Year;
            var month = date.Month;

            // 生成 slug（支援中文）
            var baseSlug = SlugifyTitle(title);
            var fullSlug = $"{year}/{month:D2}/{baseSlug}";

            // 檢查是否存在衝突
            var allPosts = await _postRepository.GetAllAsync();
            var existingSlugs = allPosts.Select(p => p.Slug).ToList();

            if (!existingSlugs.Contains(fullSlug))
            {
                return fullSlug;
            }

            // 處理衝突 - 添加數字後綴
            var counter = 2;
            while (existingSlugs.Contains($"{year}/{month:D2}/{baseSlug}-{counter}"))
            {
                counter++;
            }

            return $"{year}/{month:D2}/{baseSlug}-{counter}";
        }

        /// <summary>
        /// 發布文章
        /// </summary>
        public async Task PublishPostAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new InvalidOperationException($"找不到 ID 為 {postId} 的文章");
            }

            if (post.Status == PostStatus.Published)
            {
                return; // 已經是發布狀態
            }

            post.Status = PostStatus.Published;
            post.PublishedAt = DateTime.UtcNow;

            await _postRepository.UpdateAsync(post);
        }

        /// <summary>
        /// 取消發布文章
        /// </summary>
        public async Task UnpublishPostAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new InvalidOperationException($"找不到 ID 為 {postId} 的文章");
            }

            if (post.Status == PostStatus.Draft)
            {
                return; // 已經是草稿狀態
            }

            post.Status = PostStatus.Draft;
            // PublishedAt 保持不變，保留歷史記錄

            await _postRepository.UpdateAsync(post);
        }

        /// <summary>
        /// 根據分類 ID 取得該分類下的已發布文章列表 (分頁)
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 最大限制

            // 根據分類 ID 和已發布狀態取得文章
            var query = await _postRepository.FindAsync(p =>
                p.CategoryId == categoryId &&
                p.Status == PostStatus.Published);

            var posts = query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = query.Count();

            return (posts, totalCount);
        }

        /// <summary>
        /// 根據標籤 ID 取得該標籤下的已發布文章列表 (分頁)
        /// User Story 4: 訪客能夠透過標籤瀏覽相關文章
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)?> GetPostsByTagAsync(Guid tagId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 最大限制

            // 先檢查標籤是否存在
            var allPosts = await _postRepository.GetAllAsync();
            var tagExists = allPosts.Any(p => p.Tags.Any(t => t.Id == tagId));

            if (!tagExists)
            {
                return null; // 標籤不存在
            }

            // 根據標籤 ID 取得該標籤下的已發布文章（透過 Tags 多對多關聯）
            var query = await _postRepository.FindAsync(p =>
                p.Status == PostStatus.Published &&
                p.Tags.Any(t => t.Id == tagId));

            var posts = query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = query.Count();

            return (posts, totalCount);
        }

        /// <summary>
        /// 根據關鍵字搜尋已發布的文章 (分頁)
        /// User Story 5: 訪客能夠透過關鍵字搜尋文章標題與內容
        /// </summary>
        public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> SearchPostsAsync(string? keyword, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 最大限制

            // 處理關鍵字長度限制 - 截短過長的關鍵字
            var searchKeyword = keyword?.Trim() ?? string.Empty;
            if (searchKeyword.Length > 500)
            {
                searchKeyword = searchKeyword.Substring(0, 500);
            }

            // 如果關鍵字為空，返回空結果
            if (string.IsNullOrWhiteSpace(searchKeyword))
            {
                return (Enumerable.Empty<BlogPost>(), 0);
            }

            // 進行大小寫不敏感的搜尋（搜尋標題、內容、摘要）
            var searchKeywordLower = searchKeyword.ToLowerInvariant();

            var query = await _postRepository.FindAsync(p =>
                p.Status == PostStatus.Published &&
                (
                    p.Title.ToLower().Contains(searchKeywordLower) ||
                    p.Content.ToLower().Contains(searchKeywordLower) ||
                    (p.Summary != null && p.Summary.ToLower().Contains(searchKeywordLower))
                ));

            var posts = query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = query.Count();

            return (posts, totalCount);
        }

        /// <summary>
        /// 將標題轉換為 URL 友好的 slug（支援中文）
        /// </summary>
        private string SlugifyTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "untitled";
            }

            // 移除前後空白
            var slug = title.Trim();

            // 轉換為小寫（僅影響英文字元）
            slug = slug.ToLowerInvariant();

            // 替換空白字元為連字號
            slug = Regex.Replace(slug, @"\s+", "-");

            // 移除特殊符號（保留中文、英文、數字、連字號）
            slug = Regex.Replace(slug, @"[^\u4e00-\u9fa5a-z0-9\-]", "");

            // 移除連續的連字號
            slug = Regex.Replace(slug, @"-{2,}", "-");

            // 移除開頭和結尾的連字號
            slug = slug.Trim('-');

            // 如果 slug 為空，使用預設值
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = "untitled";
            }

            return slug;
        }
    }
}
