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

        /// <summary>
        /// 根據標題生成唯一的 slug
        /// </summary>
        /// <param name="title">文章標題</param>
        /// <param name="publishDate">發布日期（用於生成路徑前綴）</param>
        /// <returns>唯一的 slug（格式：year/month/title-slug）</returns>
        Task<string> GenerateSlugAsync(string title, DateTime? publishDate = null);

        /// <summary>
        /// 發布文章（將狀態從 Draft 改為 Published）
        /// </summary>
        Task PublishPostAsync(Guid postId);

        /// <summary>
        /// 取消發布文章（將狀態從 Published 改為 Draft）
        /// </summary>
        Task UnpublishPostAsync(Guid postId);

        /// <summary>
        /// 根據分類 ID 取得該分類下的已發布文章列表 (分頁)
        /// </summary>
        /// <param name="categoryId">分類 ID</param>
        /// <param name="page">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>分頁結果，若分類不存在返回空列表</returns>
        Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10);

        /// <summary>
        /// 根據標籤 ID 取得該標籤下的已發布文章列表 (分頁)
        /// User Story 4: 訪客能夠透過標籤瀏覽相關文章
        /// </summary>
        /// <param name="tagId">標籤 ID</param>
        /// <param name="page">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>分頁結果，若標籤不存在返回 null；若無文章返回空列表</returns>
        Task<(IEnumerable<BlogPost> Posts, int TotalCount)?> GetPostsByTagAsync(Guid tagId, int page = 1, int pageSize = 10);
    }
}
