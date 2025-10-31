using BlogSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BlogSystem.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// 管理員身份驗證控制器
    /// 處理 Google OAuth 2.0 登入/登出及存取控制
    /// </summary>
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 顯示登入頁面
        /// </summary>
        /// <returns>登入視圖</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// 處理 Google OAuth 回調
        /// 目前為佔位實作，稍後將整合真正的 OAuth 流程
        /// </summary>
        /// <returns>回調處理視圖</returns>
        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            // TODO: 實作真正的 Google OAuth 2.0 回調處理
            // 當實作完成後，在成功驗證時記錄登入日誌：
            // var userEmail = User.Identity?.Name;
            // if (!string.IsNullOrEmpty(userEmail))
            // {
            //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            //     await _authService.LogAdminActionAsync(userEmail, "Login", ipAddress);
            // }

            return View();
        }

        /// <summary>
        /// 登出並清除身份驗證
        /// </summary>
        /// <returns>重定向至首頁</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // 記錄登出操作
            var userEmail = User.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _authService.LogAdminActionAsync(userEmail, "Logout", ipAddress);
            }

            // 清除身份驗證
            await HttpContext.SignOutAsync();

            // 重定向至首頁
            return Redirect("/");
        }

        /// <summary>
        /// 顯示存取被拒頁面
        /// </summary>
        /// <returns>存取被拒視圖</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
