using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 搜尋控制器
    /// User Story 5: 訪客能夠透過關鍵字搜尋文章標題與內容
    /// </summary>
    public class SearchController : Controller
    {
        private readonly IBlogPostService _blogPostService;

        public SearchController(IBlogPostService blogPostService)
        {
            _blogPostService = blogPostService;
        }

        /// <summary>
        /// 搜尋文章索引頁面
        /// GET: /Search
        /// GET: /Search?keyword=...&page=...
        /// </summary>
        public async Task<IActionResult> Index(string? keyword, int page = 1)
        {
            // 設定頁面大小為 10
            const int pageSize = 10;

            // 獲取搜尋結果
            var (posts, totalCount) = await _blogPostService.SearchPostsAsync(keyword, page, pageSize);

            // 計算分頁資訊
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (page > totalPages && totalPages > 0)
            {
                page = totalPages;
            }

            // 轉換為 ViewModel
            var postViewModels = posts.Select(p => new PostListViewModel
            {
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? (p.Content.Length > 500 ? p.Content.Substring(0, 500) + "..." : p.Content),
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                CategoryName = p.Category?.Name,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                ViewCount = p.ViewCount
            }).ToList();

            // 建立搜尋結果 ViewModel
            var viewModel = new SearchResultViewModel
            {
                Keyword = keyword,
                Posts = postViewModels,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalResults = totalCount,
                PageSize = pageSize
            };

            return View(viewModel);
        }
    }
}
