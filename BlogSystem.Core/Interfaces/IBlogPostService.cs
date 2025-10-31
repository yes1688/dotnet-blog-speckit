using BlogSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 部落格文章服務介面
    /// </summary>
    public interface IBlogPostService
    {
        /// <summary>
        /// 取得已發布文章列表 (分頁)
        /// </summary>
        /// <param name="page">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>分頁結果</returns>
        Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPublishedPostsAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// 根據 slug 取得單篇文章
        /// </summary>
        /// <param name="year">發布年份</param>
        /// <param name="month">發布月份</param>
        /// <param name="slug">文章 slug</param>
        /// <returns>文章實體,若不存在則返回 null</returns>
        Task<BlogPost?> GetPostBySlugAsync(int year, int month, string slug);

        /// <summary>
        /// 增加文章瀏覽次數
        /// </summary>
        Task IncrementViewCountAsync(Guid postId);
    }
}
