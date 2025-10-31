using BlogSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 分類服務介面
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// 取得所有分類 (按名稱排序)
        /// </summary>
        /// <returns>分類清單</returns>
        Task<IEnumerable<Category>> GetAllCategoriesAsync();

        /// <summary>
        /// 根據 ID 取得分類
        /// </summary>
        /// <param name="id">分類 ID</param>
        /// <returns>分類實體,若不存在則返回 null</returns>
        Task<Category?> GetCategoryByIdAsync(Guid id);

        /// <summary>
        /// 建立新分類
        /// </summary>
        /// <param name="name">分類名稱</param>
        /// <param name="description">分類描述 (選擇性)</param>
        /// <returns>新建立的分類實體</returns>
        /// <exception cref="ArgumentException">當名稱為空或重複時拋出</exception>
        Task<Category> CreateCategoryAsync(string name, string? description = null);

        /// <summary>
        /// 更新分類
        /// </summary>
        /// <param name="id">分類 ID</param>
        /// <param name="name">新的分類名稱</param>
        /// <param name="description">新的分類描述</param>
        /// <returns>更新後的分類實體</returns>
        /// <exception cref="InvalidOperationException">當分類不存在或名稱重複時拋出</exception>
        Task UpdateCategoryAsync(Guid id, string name, string? description = null);

        /// <summary>
        /// 刪除分類
        /// </summary>
        /// <param name="id">分類 ID</param>
        /// <exception cref="InvalidOperationException">當分類不存在或有關聯文章時拋出</exception>
        Task DeleteCategoryAsync(Guid id);

        /// <summary>
        /// 根據分類 ID 取得相關文章 (只返回已發布的)
        /// </summary>
        /// <param name="categoryId">分類 ID</param>
        /// <param name="page">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>分頁結果</returns>
        Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10);
    }
}
