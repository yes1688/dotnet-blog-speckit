using System;
using System.Collections.Generic;

namespace BlogSystem.Core.Entities
{
    /// <summary>
    /// 文章標籤實體
    /// </summary>
    public class Tag
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 標籤名稱 (最大 30 字元,唯一)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL 友善名稱 (最大 50 字元,唯一)
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 使用次數 (自動計算)
        /// </summary>
        public int UsageCount { get; set; }

        // 導航屬性
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }
}
