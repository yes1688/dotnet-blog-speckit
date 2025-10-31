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
    /// 搜尋服務實現
    /// User Story 5: 訪客能夠搜尋文章
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly IRepository<BlogPost> _repository;
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 10;
        private const int MinPage = 1;

        public SearchService(IRepository<BlogPost> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 根據關鍵字搜尋已發布的文章
        /// </summary>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>搜尋結果和總筆數</returns>
        public async Task<(IEnumerable<BlogPost> Results, int TotalCount)> SearchAsync(
            string keyword,
            int page = 1,
            int pageSize = 10)
        {
            // 驗證關鍵字
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return (Enumerable.Empty<BlogPost>(), 0);
            }

            // 標準化分頁參數
            page = Math.Max(page, MinPage);
            pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

            // 搜尋已發布的文章，匹配標題、內容或摘要
            var allPosts = await _repository.FindAsync(p =>
                p.Status == PostStatus.Published &&
                (p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 p.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 (p.Summary != null && p.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            );

            // 計算總筆數
            var totalCount = allPosts.Count();

            // 排序：優先標題匹配，再按發布日期降序排列
            var sortedPosts = allPosts
                .OrderByDescending(p => p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(p => p.PublishedAt)
                .ToList();

            // 應用分頁
            var pagedPosts = sortedPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedPosts, totalCount);
        }
    }
}
