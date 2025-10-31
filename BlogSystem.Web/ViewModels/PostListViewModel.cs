using System;
using System.Collections.Generic;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 文章列表視圖模型
    /// </summary>
    public class PostListViewModel
    {
        /// <summary>
        /// 文章標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 文章 slug (完整路徑: /year/month/slug)
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 文章摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 發布日期
        /// </summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// 分類名稱
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// 標籤列表
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 瀏覽次數
        /// </summary>
        public int ViewCount { get; set; }
    }
}
