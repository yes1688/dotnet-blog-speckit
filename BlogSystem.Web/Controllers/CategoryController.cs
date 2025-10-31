using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 分類控制器 - 用於前台訪客瀏覽特定分類下的文章
    /// </summary>
    public class CategoryController : Controller
    {
        private readonly IBlogPostService _blogPostService;

        public CategoryController(IBlogPostService blogPostService)
        {
            _blogPostService = blogPostService;
        }

        /// <summary>
        /// 分類文章列表
        /// 根據分類 ID 顯示該分類下已發布的文章，支援分頁
        /// </summary>
        /// <param name="categoryId">分類 ID</param>
        /// <param name="page">頁碼 (預設 1)</param>
        [HttpGet]
        public async Task<IActionResult> Index(string categoryId, int page = 1)
        {
            // 驗證 categoryId 是否為有效的 GUID
            if (!System.Guid.TryParse(categoryId, out var parsedCategoryId))
            {
                return NotFound();
            }

            const int pageSize = 10;

            var (posts, totalCount) = await _blogPostService.GetPostsByCategoryAsync(parsedCategoryId, page, pageSize);

            if (totalCount == 0 && page == 1)
            {
                // 分類不存在或沒有文章，返回 404
                return NotFound();
            }

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

            // 將分類 ID 傳遞給 View
            ViewData["CategoryId"] = categoryId;

            return View(pagedResult);
        }
    }
}
