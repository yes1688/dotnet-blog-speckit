using System;
using System.Collections.Generic;
using BlogSystem.Core.Entities;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 分類文章列表視圖模型
    /// 用於展示特定分類下的文章列表及分頁資訊
    /// </summary>
    public class CategoryPostsViewModel
    {
        /// <summary>
        /// 分類實體
        /// </summary>
        public Category Category { get; set; } = null!;

        /// <summary>
        /// 該分類下的文章列表
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
        /// 該分類下的總文章數
        /// </summary>
        public int TotalPosts { get; set; }

        /// <summary>
        /// 是否有上一頁
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否有下一頁
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 每頁顯示的文章數
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
