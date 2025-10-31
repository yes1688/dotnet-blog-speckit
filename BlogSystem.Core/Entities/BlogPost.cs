using System;
using System.Collections.Generic;
using BlogSystem.Core.Enums;

namespace BlogSystem.Core.Entities
{
    /// <summary>
    /// 部落格文章實體
    /// </summary>
    public class BlogPost
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 文章標題 (最大 200 字元,支援中文)
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// URL 友善標題 (格式: {year}/{month}/{url-safe-title})
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 文章內容 (Markdown 格式)
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 文章摘要 (最大 500 字元)
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// 封面圖片 URL
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// 發布日期時間 (UTC)
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// 建立日期時間 (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最後更新時間 (UTC)
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 文章狀態 (Draft=0, Published=1)
        /// </summary>
        public PostStatus Status { get; set; }

        /// <summary>
        /// 瀏覽次數
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// 分類 ID (FK to Category)
        /// </summary>
        public Guid? CategoryId { get; set; }

        // 導航屬性
        public Category? Category { get; set; }
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
