using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 身份驗證服務介面
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 檢查指定的 Email 是否為管理員
        /// </summary>
        /// <param name="email">使用者 Email</param>
        /// <returns>如果是管理員則返回 true</returns>
        Task<bool> IsAdminAsync(string email);

        /// <summary>
        /// 記錄管理員操作日誌
        /// </summary>
        /// <param name="email">管理員 Email</param>
        /// <param name="action">操作類型 (Login, Logout, etc.)</param>
        /// <param name="ipAddress">IP 位址</param>
        Task LogAdminActionAsync(string email, string action, string? ipAddress = null);
    }
}
