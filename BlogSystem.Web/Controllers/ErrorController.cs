using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 錯誤處理控制器
    /// 處理 404 和其他 HTTP 錯誤狀態碼
    /// </summary>
    public class ErrorController : Controller
    {
        /// <summary>
        /// 404 Not Found 頁面
        /// GET: /Error/NotFound
        /// </summary>
        [HttpGet]
        public IActionResult NotFound(int? statusCode = null)
        {
            // 設定狀態碼為 404
            Response.StatusCode = 404;

            return View();
        }
    }
}
