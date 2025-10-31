using System;
using System.Collections.Generic;
using BlogSystem.Core.Enums;

namespace BlogSystem.Web.Areas.Admin.ViewModels
{
    /// <summary>
    /// 管理員文章列表視圖模型
    /// </summary>
    public class PostListViewModel
    {
        /// <summary>
        /// 文章 ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// URL slug
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 文章狀態
        /// </summary>
        public PostStatus Status { get; set; }

        /// <summary>
        /// 分類名稱
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// 標籤名稱列表
        /// </summary>
        public List<string>? TagNames { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 發布時間
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// 瀏覽次數
        /// </summary>
        public int ViewCount { get; set; }
    }
}
