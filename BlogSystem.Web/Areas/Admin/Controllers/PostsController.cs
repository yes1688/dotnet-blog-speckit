using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using BlogSystem.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// 管理員文章管理控制器
    /// 處理文章的 CRUD 操作
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class PostsController : Controller
    {
        private readonly IBlogPostService _blogPostService;
        private readonly IRepository<BlogPost> _postRepository;
        private readonly IAuthService _authService;

        public PostsController(
            IBlogPostService blogPostService,
            IRepository<BlogPost> postRepository,
            IAuthService authService)
        {
            _blogPostService = blogPostService;
            _postRepository = postRepository;
            _authService = authService;
        }

        /// <summary>
        /// 文章列表頁
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allPosts = await _postRepository.GetAllAsync();
            var viewModels = allPosts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostListViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Summary = p.Summary,
                    Slug = p.Slug,
                    Status = p.Status,
                    CategoryName = p.Category?.Name,
                    TagNames = p.Tags?.Select(t => t.Name).ToList(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    PublishedAt = p.PublishedAt,
                    ViewCount = p.ViewCount
                })
                .ToList();

            return View(viewModels);
        }

        /// <summary>
        /// 顯示新增文章表單
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new PostCreateViewModel());
        }

        /// <summary>
        /// 處理新增文章提交
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 生成 slug（如果使用者未提供）
                var slug = string.IsNullOrWhiteSpace(model.Slug)
                    ? await _blogPostService.GenerateSlugAsync(model.Title)
                    : model.Slug;

                var post = new BlogPost
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Content = model.Content,
                    Summary = model.Summary,
                    Slug = slug,
                    CategoryId = model.CategoryId,
                    CoverImageUrl = model.CoverImageUrl,
                    Status = model.IsPublished ? PostStatus.Published : PostStatus.Draft,
                    PublishedAt = model.IsPublished ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ViewCount = 0
                };

                await _postRepository.AddAsync(post);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "CreatePost", ipAddress);
                }

                TempData["SuccessMessage"] = "文章建立成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"建立文章失敗：{ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// 顯示編輯文章表單
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var viewModel = new PostEditViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Summary = post.Summary,
                Slug = post.Slug,
                CategoryId = post.CategoryId,
                Tags = post.Tags != null ? string.Join(", ", post.Tags.Select(t => t.Name)) : null,
                CoverImageUrl = post.CoverImageUrl,
                Status = post.Status,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount
            };

            return View(viewModel);
        }

        /// <summary>
        /// 處理編輯文章提交
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PostEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var post = await _postRepository.GetByIdAsync(id);
                if (post == null)
                {
                    return NotFound();
                }

                post.Title = model.Title;
                post.Content = model.Content;
                post.Summary = model.Summary;
                post.Slug = model.Slug;
                post.CategoryId = model.CategoryId;
                post.CoverImageUrl = model.CoverImageUrl;
                post.Status = model.Status;
                post.UpdatedAt = DateTime.UtcNow;

                await _postRepository.UpdateAsync(post);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "EditPost", ipAddress);
                }

                TempData["SuccessMessage"] = "文章更新成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"更新文章失敗：{ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// 刪除文章
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            try
            {
                await _postRepository.DeleteAsync(post);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "DeletePost", ipAddress);
                }

                TempData["SuccessMessage"] = "文章刪除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除文章失敗：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 發布文章
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(Guid id)
        {
            try
            {
                await _blogPostService.PublishPostAsync(id);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "PublishPost", ipAddress);
                }

                TempData["SuccessMessage"] = "文章發布成功！";
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"發布文章失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 取消發布文章
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(Guid id)
        {
            try
            {
                await _blogPostService.UnpublishPostAsync(id);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "UnpublishPost", ipAddress);
                }

                TempData["SuccessMessage"] = "文章已取消發布！";
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"取消發布失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
