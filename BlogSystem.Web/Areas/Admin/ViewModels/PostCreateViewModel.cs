using System;
using System.ComponentModel.DataAnnotations;

namespace BlogSystem.Web.Areas.Admin.ViewModels
{
    /// <summary>
    /// 新增文章的表單視圖模型
    /// </summary>
    public class PostCreateViewModel
    {
        /// <summary>
        /// 文章標題
        /// </summary>
        [Required(ErrorMessage = "文章標題為必填")]
        [MaxLength(200, ErrorMessage = "文章標題不可超過 200 字元")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 文章內容 (Markdown 格式)
        /// </summary>
        [Required(ErrorMessage = "文章內容為必填")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 文章摘要
        /// </summary>
        [Required(ErrorMessage = "文章摘要為必填")]
        [MaxLength(500, ErrorMessage = "文章摘要不可超過 500 字元")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// URL Slug (可為空，系統將自動生成)
        /// </summary>
        [MaxLength(200, ErrorMessage = "Slug 不可超過 200 字元")]
        public string? Slug { get; set; }

        /// <summary>
        /// 分類 ID
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// 標籤 (逗號分隔)
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// 封面圖片 URL
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// 是否立即發布 (預設為 false)
        /// </summary>
        public bool IsPublished { get; set; } = false;
    }
}
