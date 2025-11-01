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
        /// 發起 Google OAuth 登入流程
        /// </summary>
        /// <returns>重定向至 Google 登入頁面</returns>
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };
            return Challenge(properties, "Google");
        }

        /// <summary>
        /// 處理 Google OAuth 回調
        /// </summary>
        /// <returns>重定向至管理後台或錯誤頁面</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            // 驗證使用者身份
            var authenticateResult = await HttpContext.AuthenticateAsync("Google");

            if (!authenticateResult.Succeeded)
            {
                return RedirectToAction("AccessDenied");
            }

            // 取得使用者 Email
            var userEmail = authenticateResult.Principal?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("AccessDenied");
            }

            // 記錄登入操作
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogAdminActionAsync(userEmail, "Login", ipAddress);

            // 使用 Cookie 登入
            await HttpContext.SignInAsync("Cookies", authenticateResult.Principal);

            // 重定向至管理後台
            return Redirect("/Admin/Dashboard");
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
