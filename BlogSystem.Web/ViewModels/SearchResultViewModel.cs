using System;
using System.Collections.Generic;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 搜尋結果視圖模型
    /// 用於展示搜尋結果及分頁資訊
    /// </summary>
    public class SearchResultViewModel
    {
        /// <summary>
        /// 搜尋關鍵字
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 搜尋結果文章列表
        /// </summary>
        public IEnumerable<PostListViewModel> Posts { get; set; } = new List<PostListViewModel>();

        /// <summary>
        /// 當前頁碼 (從 1 開始)
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 搜尋結果總數
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// 每頁顯示的文章數
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 是否有上一頁
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否有下一頁
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 是否有搜尋結果
        /// </summary>
        public bool HasResults => TotalResults > 0;
    }
}
