using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// 管理儀表板控制器
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : Controller
    {
        /// <summary>
        /// 管理儀表板首頁
        /// 顯示系統統計資訊、最近文章等摘要資訊
        /// </summary>
        /// <returns>儀表板視圖</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
