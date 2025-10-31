using System;

namespace BlogSystem.Core.Entities
{
    /// <summary>
    /// 管理員操作記錄實體
    /// </summary>
    public class AdminLog
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 管理員 email (最大 100 字元)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 操作類型 (Login, Logout, CreatePost, DeletePost 等)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 操作詳情 (JSON 格式)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// 操作時間 (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// IP 位址 (最大 45 字元,支援 IPv6)
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
