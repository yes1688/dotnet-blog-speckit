using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
