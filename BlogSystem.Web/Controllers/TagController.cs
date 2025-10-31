using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ITagService _tagService;
        private readonly ILogger<TagController> _logger;

        public TagController(IBlogPostService blogPostService, ITagService tagService, ILogger<TagController> logger)
        {
            _blogPostService = blogPostService;
            _tagService = tagService;
            _logger = logger;
        }

        /// <summary>
        /// 標籤頁面 - 顯示特定標籤下的文章列表
        /// 路由: /Tag/{slug}
        /// User Story 4 Acceptance Scenario 5: 訪客在文章頁面點擊標籤，顯示具有相同標籤的文章
        /// </summary>
        /// <param name="id">標籤 slug</param>
        /// <param name="page">頁碼 (預設 1)</param>
        [HttpGet]
        public async Task<IActionResult> Index(string id, int page = 1)
        {
            _logger.LogInformation("TagController.Index called with id={Id}, page={Page}", id, page);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("TagController.Index: id is null or whitespace");
                return NotFound();
            }

            // 根據 slug 查找標籤
            _logger.LogInformation("TagController.Index: Fetching all tags");
            var tags = await _tagService.GetAllTagsAsync();
            _logger.LogInformation("TagController.Index: Found {Count} tags", tags.Count());

            var tag = tags.FirstOrDefault(t => t.Slug.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
            {
                _logger.LogWarning("TagController.Index: Tag with slug '{Slug}' not found", id);
                return NotFound();
            }

            _logger.LogInformation("TagController.Index: Found tag '{Name}' with id={TagId}", tag.Name, tag.Id);

            const int pageSize = 10;

            // 取得該標籤下的文章列表
            var result = await _blogPostService.GetPostsByTagAsync(tag.Id, page, pageSize);

            // 標籤不存在
            if (result == null)
            {
                return NotFound($"標籤 '{tag.Name}' 不存在");
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

            // 將標籤資訊傳遞給 View
            ViewData["TagName"] = tag.Name;
            ViewData["TagSlug"] = tag.Slug;

            return View(pagedResult);
        }
    }
}
