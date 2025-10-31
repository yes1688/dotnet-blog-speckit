using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IBlogPostService blogPostService, ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _blogPostService = blogPostService;
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>
        /// 分類文章列表
        /// 根據分類 slug 顯示該分類下已發布的文章，支援分頁
        /// </summary>
        /// <param name="id">分類 slug</param>
        /// <param name="page">頁碼 (預設 1)</param>
        [HttpGet]
        public async Task<IActionResult> Index(string id, int page = 1)
        {
            _logger.LogInformation("CategoryController.Index called with id={Id}, page={Page}", id, page);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("CategoryController.Index: id is null or whitespace");
                return NotFound();
            }

            // 根據 slug 查找分類
            _logger.LogInformation("CategoryController.Index: Fetching all categories");
            var categories = await _categoryService.GetAllCategoriesAsync();
            _logger.LogInformation("CategoryController.Index: Found {Count} categories", categories.Count());

            var category = categories.FirstOrDefault(c => c.Slug.Equals(id, System.StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                _logger.LogWarning("CategoryController.Index: Category with slug '{Slug}' not found", id);
                return NotFound();
            }

            _logger.LogInformation("CategoryController.Index: Found category '{Name}' with id={CategoryId}", category.Name, category.Id);

            const int pageSize = 10;

            var (posts, totalCount) = await _blogPostService.GetPostsByCategoryAsync(category.Id, page, pageSize);

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

            // 將分類資訊傳遞給 View
            ViewData["CategoryName"] = category.Name;
            ViewData["CategorySlug"] = category.Slug;

            return View(pagedResult);
        }
    }
}
