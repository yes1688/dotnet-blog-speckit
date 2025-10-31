using BlogSystem.Core.Interfaces;
using BlogSystem.Web.Models;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 首頁控制器
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IBlogPostService _blogPostService;

        public HomeController(IBlogPostService blogPostService)
        {
            _blogPostService = blogPostService;
        }

        /// <summary>
        /// 首頁 - 顯示已發布文章列表
        /// </summary>
        /// <param name="page">頁碼 (預設 1)</param>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;

            var (posts, totalCount) = await _blogPostService.GetPublishedPostsAsync(page, pageSize);

            var viewModels = posts.Select(p => new PostListViewModel
            {
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? (p.Content.Length > 500 ? p.Content.Substring(0, 500) + "..." : p.Content),
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                CategoryName = p.Category?.Name,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                ViewCount = p.ViewCount
            }).ToList();

            var pagedResult = new PagedResult<PostListViewModel>
            {
                Items = viewModels,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(pagedResult);
        }

        /// <summary>
        /// 錯誤頁面 (T114: 全域錯誤處理)
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // 從 HttpContext.Items 中取得 RequestId (由中介軟體設定)
            var requestId = HttpContext.Items["RequestId"] as string;

            // 如果沒有從中介軟體取得，則使用 Activity 或 TraceIdentifier
            if (string.IsNullOrEmpty(requestId))
            {
                requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            }

            var errorViewModel = new ErrorViewModel
            {
                RequestId = requestId
            };

            return View(errorViewModel);
        }
    }
}
