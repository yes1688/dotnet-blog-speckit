using System;
using System.ComponentModel.DataAnnotations;
using BlogSystem.Core.Enums;

namespace BlogSystem.Web.Areas.Admin.ViewModels
{
    /// <summary>
    /// 管理員編輯文章的表單 ViewModel
    /// </summary>
    public class PostEditViewModel
    {
        /// <summary>
        /// 文章 ID
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 文章標題
        /// </summary>
        [Required(ErrorMessage = "請輸入文章標題")]
        [MaxLength(200, ErrorMessage = "標題長度不能超過 200 字元")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Markdown 內容
        /// </summary>
        [Required(ErrorMessage = "請輸入文章內容")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 摘要
        /// </summary>
        [Required(ErrorMessage = "請輸入文章摘要")]
        [MaxLength(500, ErrorMessage = "摘要長度不能超過 500 字元")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// URL slug
        /// </summary>
        [Required(ErrorMessage = "請輸入 URL slug")]
        [MaxLength(200, ErrorMessage = "Slug 長度不能超過 200 字元")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 分類 ID
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// 標籤（逗號分隔）
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// 封面圖片 URL
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// 文章狀態（Draft/Published）
        /// </summary>
        public PostStatus Status { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 瀏覽次數
        /// </summary>
        public int ViewCount { get; set; }
    }
}
