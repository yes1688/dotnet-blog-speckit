using System;
using System.Collections.Generic;

namespace BlogSystem.Web.ViewModels
{
    /// <summary>
    /// 文章詳細頁視圖模型
    /// </summary>
    public class PostDetailViewModel
    {
        /// <summary>
        /// 文章標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 文章內容 (已轉換為 HTML 的 Markdown)
        /// </summary>
        public string ContentHtml { get; set; } = string.Empty;

        /// <summary>
        /// 發布日期
        /// </summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// 分類名稱
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// 分類 Slug
        /// </summary>
        public string? CategorySlug { get; set; }

        /// <summary>
        /// 標籤列表
        /// </summary>
        public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();

        /// <summary>
        /// 瀏覽次數
        /// </summary>
        public int ViewCount { get; set; }
    }

    /// <summary>
    /// 標籤視圖模型
    /// </summary>
    public class TagViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
