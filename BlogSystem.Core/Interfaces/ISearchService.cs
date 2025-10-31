using BlogSystem.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 搜尋服務介面
    /// User Story 5: 訪客能夠搜尋文章
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// 根據關鍵字搜尋已發布的文章
        /// </summary>
        /// <param name="keyword">搜尋關鍵字 (支援標題、內容、摘要)</param>
        /// <param name="page">頁碼 (從 1 開始, 預設為 1)</param>
        /// <param name="pageSize">每頁筆數 (預設為 10)</param>
        /// <returns>搜尋結果和總筆數</returns>
        Task<(IEnumerable<BlogPost> Results, int TotalCount)> SearchAsync(
            string keyword,
            int page = 1,
            int pageSize = 10);
    }
}
