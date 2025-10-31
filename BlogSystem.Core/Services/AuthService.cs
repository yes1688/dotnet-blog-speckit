using BlogSystem.Core.Entities;
using BlogSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Core.Services
{
    /// <summary>
    /// 身份驗證服務實作
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IRepository<AdminLog> _adminLogRepository;
        private readonly string _adminEmailsConfig;

        public AuthService(IRepository<AdminLog> adminLogRepository, string adminEmailsConfig)
        {
            _adminLogRepository = adminLogRepository;
            _adminEmailsConfig = adminEmailsConfig ?? string.Empty;
        }

        /// <summary>
        /// 檢查指定的 Email 是否為管理員
        /// </summary>
        /// <param name="email">使用者 Email</param>
        /// <returns>如果是管理員則返回 true</returns>
        public Task<bool> IsAdminAsync(string email)
        {
            // 驗證輸入
            if (string.IsNullOrWhiteSpace(email))
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(_adminEmailsConfig))
            {
                return Task.FromResult(false);
            }

            // 分割並清理 Email 列表
            var adminEmails = _adminEmailsConfig
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            // 不區分大小寫比較
            var isAdmin = adminEmails.Any(adminEmail =>
                adminEmail.Equals(email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(isAdmin);
        }

        /// <summary>
        /// 記錄管理員操作日誌
        /// </summary>
        /// <param name="email">管理員 Email</param>
        /// <param name="action">操作類型 (Login, Logout, etc.)</param>
        /// <param name="ipAddress">IP 位址</param>
        public async Task LogAdminActionAsync(string email, string action, string? ipAddress = null)
        {
            var adminLog = new AdminLog
            {
                Id = Guid.NewGuid(),
                Email = email ?? string.Empty,
                Action = action ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            await _adminLogRepository.AddAsync(adminLog);
        }
    }
}
