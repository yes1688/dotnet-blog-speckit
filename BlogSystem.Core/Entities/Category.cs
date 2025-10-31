using System;
using System.Collections.Generic;

namespace BlogSystem.Core.Entities
{
    /// <summary>
    /// 文章分類實體
    /// </summary>
    public class Category
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 分類名稱 (最大 50 字元,唯一)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL 友善名稱 (最大 100 字元,唯一)
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 分類描述 (最大 200 字元)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 顯示順序 (較小值優先顯示)
        /// </summary>
        public int DisplayOrder { get; set; }

        // 導航屬性
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }
}
