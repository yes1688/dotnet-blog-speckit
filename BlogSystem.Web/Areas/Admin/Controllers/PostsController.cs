using BlogSystem.Core.Entities;
using BlogSystem.Core.Enums;
using BlogSystem.Core.Interfaces;
using BlogSystem.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
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
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;

        public PostsController(
            IBlogPostService blogPostService,
            IRepository<BlogPost> postRepository,
            IAuthService authService,
            ICategoryService categoryService,
            ITagService tagService)
        {
            _blogPostService = blogPostService;
            _postRepository = postRepository;
            _authService = authService;
            _categoryService = categoryService;
            _tagService = tagService;
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
        public async Task<IActionResult> Create()
        {
            try
            {
                // 設定分類下拉清單
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");

                return View(new PostCreateViewModel());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"載入分類失敗：{ex.Message}";
                return View(new PostCreateViewModel());
            }
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
                    ViewCount = 0,
                    Tags = new List<Tag>()
                };

                // 處理標籤
                if (!string.IsNullOrWhiteSpace(model.Tags))
                {
                    var tagNames = _tagService.ParseTagsFromString(model.Tags);
                    foreach (var tagName in tagNames)
                    {
                        try
                        {
                            var existingTag = await _tagService.GetTagByIdAsync(Guid.Empty);
                            // 查詢現有標籤（使用 GetAllTagsAsync 取得所有標籤並比較名稱）
                            var allTags = await _tagService.GetAllTagsAsync();
                            var existingTagByName = allTags.FirstOrDefault(t => t.Name == tagName);

                            if (existingTagByName != null)
                            {
                                post.Tags.Add(existingTagByName);
                            }
                            else
                            {
                                var newTag = await _tagService.CreateTagAsync(tagName);
                                post.Tags.Add(newTag);
                            }
                        }
                        catch (ArgumentException)
                        {
                            // 標籤名稱重複，尋找現有的標籤
                            var allTags = await _tagService.GetAllTagsAsync();
                            var existingTag = allTags.FirstOrDefault(t => t.Name == tagName);
                            if (existingTag != null && !post.Tags.Any(t => t.Id == existingTag.Id))
                            {
                                post.Tags.Add(existingTag);
                            }
                        }
                    }
                }

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
            try
            {
                var post = await _postRepository.GetByIdAsync(id);
                if (post == null)
                {
                    return NotFound();
                }

                // 設定分類下拉清單
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", post.CategoryId);

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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"載入文章失敗：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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

                // 處理標籤：清除舊的標籤並設定新的
                post.Tags = new List<Tag>();
                if (!string.IsNullOrWhiteSpace(model.Tags))
                {
                    var tagNames = _tagService.ParseTagsFromString(model.Tags);
                    foreach (var tagName in tagNames)
                    {
                        try
                        {
                            // 查詢現有標籤
                            var allTags = await _tagService.GetAllTagsAsync();
                            var existingTagByName = allTags.FirstOrDefault(t => t.Name == tagName);

                            if (existingTagByName != null)
                            {
                                if (!post.Tags.Any(t => t.Id == existingTagByName.Id))
                                {
                                    post.Tags.Add(existingTagByName);
                                }
                            }
                            else
                            {
                                var newTag = await _tagService.CreateTagAsync(tagName);
                                post.Tags.Add(newTag);
                            }
                        }
                        catch (ArgumentException)
                        {
                            // 標籤名稱重複，尋找現有的標籤
                            var allTags = await _tagService.GetAllTagsAsync();
                            var existingTag = allTags.FirstOrDefault(t => t.Name == tagName);
                            if (existingTag != null && !post.Tags.Any(t => t.Id == existingTag.Id))
                            {
                                post.Tags.Add(existingTag);
                            }
                        }
                    }
                }

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
