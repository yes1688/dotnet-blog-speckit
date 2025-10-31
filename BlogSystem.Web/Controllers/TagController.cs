using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 標籤控制器 (User Story 4)
    /// 訪客透過標籤瀏覽相關文章
    /// </summary>
    public class TagController : Controller
    {
        private readonly IBlogPostService _blogPostService;

        public TagController(IBlogPostService blogPostService)
        {
            _blogPostService = blogPostService;
        }

        /// <summary>
        /// 標籤頁面 - 顯示特定標籤下的文章列表
        /// 路由: /Tag/Index?tagId={tagId}&page={page}
        /// User Story 4 Acceptance Scenario 5: 訪客在文章頁面點擊標籤，顯示具有相同標籤的文章
        /// </summary>
        /// <param name="tagId">標籤 ID</param>
        /// <param name="page">頁碼 (預設 1)</param>
        [HttpGet]
        public async Task<IActionResult> Index(Guid? tagId, int page = 1)
        {
            // 驗證 tagId 是否提供
            if (!tagId.HasValue || tagId.Value == Guid.Empty)
            {
                return BadRequest("標籤 ID 未提供或無效");
            }

            const int pageSize = 10;

            // 取得該標籤下的文章列表
            var result = await _blogPostService.GetPostsByTagAsync(tagId.Value, page, pageSize);

            // 標籤不存在
            if (result == null)
            {
                return NotFound($"標籤 ID: {tagId} 不存在");
            }

            var (posts, totalCount) = result.Value;

            // 轉換為 ViewModel
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

            // 將標籤 ID 傳遞給 View
            ViewData["TagId"] = tagId.Value;

            return View(pagedResult);
        }
    }
}
