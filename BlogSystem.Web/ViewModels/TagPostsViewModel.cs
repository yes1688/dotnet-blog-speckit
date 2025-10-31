using System;
using System.Collections.Generic;
using BlogSystem.Core.Entities;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 標籤文章列表視圖模型
    /// </summary>
    public class TagPostsViewModel
    {
        /// <summary>
        /// 標籤實體
        /// </summary>
        public Tag Tag { get; set; } = new();

        /// <summary>
        /// 標籤下的文章列表
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
        /// 標籤下的文章總數
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
    }
}
