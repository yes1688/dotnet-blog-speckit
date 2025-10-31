using BlogSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BlogSystem.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// 檔案上傳控制器
    /// 處理 AJAX 圖片上傳請求
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UploadController : Controller
    {
        private readonly IImageService _imageService;
        private readonly IAuthService _authService;

        public UploadController(IImageService imageService, IAuthService authService)
        {
            _imageService = imageService;
            _authService = authService;
        }

        /// <summary>
        /// AJAX 圖片上傳端點
        /// </summary>
        /// <param name="file">上傳的圖片檔案</param>
        /// <returns>JSON 結果包含圖片 URL 或錯誤訊息</returns>
        [HttpPost]
        public async Task<IActionResult> Image(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "請選擇要上傳的圖片檔案"
                });
            }

            try
            {
                // 驗證圖片
                if (!_imageService.ValidateImage(file))
                {
                    return Json(new
                    {
                        success = false,
                        message = "無效的圖片檔案。支援格式：JPG、PNG、GIF，最大 5MB"
                    });
                }

                // 上傳圖片
                var imageUrl = await _imageService.UploadImageAsync(file);

                // 記錄管理員操作
                var email = User.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _authService.LogAdminActionAsync(email, "UploadImage", ipAddress);
                }

                return Json(new
                {
                    success = true,
                    url = imageUrl,
                    message = "圖片上傳成功"
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
                    message = $"上傳失敗：{ex.Message}"
                });
            }
        }
    }
}
