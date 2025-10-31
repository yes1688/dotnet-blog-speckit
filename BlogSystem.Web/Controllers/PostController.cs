using BlogSystem.Core.Interfaces;
using BlogSystem.Web.ViewModels;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Controllers
{
    /// <summary>
    /// 文章控制器
    /// </summary>
    public class PostController : Controller
    {
        private readonly IBlogPostService _blogPostService;
        private readonly MarkdownPipeline _markdownPipeline;

        public PostController(IBlogPostService blogPostService, MarkdownPipeline markdownPipeline)
        {
            _blogPostService = blogPostService;
            _markdownPipeline = markdownPipeline;
        }

        /// <summary>
        /// 文章詳細頁
        /// 路由格式: /{year}/{month}/{slug}
        /// </summary>
        public async Task<IActionResult> Details(int year, int month, string slug)
        {
            var post = await _blogPostService.GetPostBySlugAsync(year, month, slug);

            if (post == null)
            {
                return NotFound();
            }

            // 增加瀏覽次數
            await _blogPostService.IncrementViewCountAsync(post.Id);

            // 將 Markdown 轉換為 HTML
            var contentHtml = Markdown.ToHtml(post.Content, _markdownPipeline);

            var viewModel = new PostDetailViewModel
            {
                Title = post.Title,
                ContentHtml = contentHtml,
                PublishedAt = post.PublishedAt ?? post.CreatedAt,
                CategoryName = post.Category?.Name,
                CategorySlug = post.Category?.Slug,
                Tags = post.Tags.Select(t => new TagViewModel
                {
                    Name = t.Name,
                    Slug = t.Slug
                }).ToList(),
                ViewCount = post.ViewCount
            };

            return View(viewModel);
        }
    }
}
