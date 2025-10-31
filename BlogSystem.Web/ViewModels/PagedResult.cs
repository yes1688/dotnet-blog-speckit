using System.Collections.Generic;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 分頁結果輔助類別
    /// </summary>
    /// <typeparam name="T">項目類型</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// 當前頁項目列表
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// 當前頁碼 (從 1 開始)
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 總項目數
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

        /// <summary>
        /// 是否有上一頁
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否有下一頁
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
