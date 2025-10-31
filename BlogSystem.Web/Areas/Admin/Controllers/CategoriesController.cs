using BlogSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// 管理員分類管理控制器
    /// 處理分類的 CRUD 操作
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;

        public CategoriesController(
            ICategoryService categoryService,
            IAuthService authService)
        {
            _categoryService = categoryService;
            _authService = authService;
        }

        /// <summary>
        /// 分類列表頁
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                var categoryList = categories
                    .OrderBy(c => c.Name)
                    .ToList();

                return View(categoryList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"載入分類列表失敗：{ex.Message}";
                return View(new List<dynamic>());
            }
        }

        /// <summary>
        /// 建立分類 (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return Json(new
                {
                    success = false,
                    message = "分類名稱不能為空"
                });
            }

            try
            {
                var category = await _categoryService.CreateCategoryAsync(
                    request.Name.Trim(),
                    request.Description?.Trim());

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "CreateCategory", ipAddress);
                }

                return Json(new
                {
                    success = true,
                    message = "分類建立成功",
                    category = new
                    {
                        id = category.Id,
                        name = category.Name,
                        description = category.Description,
                        createdAt = category.CreatedAt
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"建立分類失敗：{ex.Message}"
                });
            }
        }

        /// <summary>
        /// 更新分類 (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] EditCategoryRequest request)
        {
            if (request?.Id == Guid.Empty || string.IsNullOrWhiteSpace(request?.Name))
            {
                return Json(new
                {
                    success = false,
                    message = "分類 ID 和名稱不能為空"
                });
            }

            try
            {
                await _categoryService.UpdateCategoryAsync(
                    request.Id,
                    request.Name.Trim(),
                    request.Description?.Trim());

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "EditCategory", ipAddress);
                }

                return Json(new
                {
                    success = true,
                    message = "分類更新成功"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"更新分類失敗：{ex.Message}"
                });
            }
        }

        /// <summary>
        /// 刪除分類
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "無效的分類 ID";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _categoryService.DeleteCategoryAsync(id);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "DeleteCategory", ipAddress);
                }

                TempData["SuccessMessage"] = "分類刪除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除分類失敗：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    /// <summary>
    /// 建立分類請求模型
    /// </summary>
    public class CreateCategoryRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// 編輯分類請求模型
    /// </summary>
    public class EditCategoryRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
