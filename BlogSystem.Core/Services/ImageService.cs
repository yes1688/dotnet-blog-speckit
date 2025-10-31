using BlogSystem.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlogSystem.Core.Services
{
    /// <summary>
    /// 圖片服務實作
    /// 處理圖片上傳、驗證、刪除
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const string UploadFolder = "uploads";

        public ImageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 上傳圖片並儲存到伺服器
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            // 驗證輸入
            if (imageFile == null)
            {
                throw new ArgumentNullException(nameof(imageFile));
            }

            if (imageFile.Length == 0)
            {
                throw new ArgumentException("檔案大小不能為零", nameof(imageFile));
            }

            // 驗證圖片
            if (!ValidateImage(imageFile))
            {
                throw new ArgumentException("無效的圖片檔案", nameof(imageFile));
            }

            // 生成唯一檔名
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // 確保上傳目錄存在
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, UploadFolder);
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // 儲存檔案
            var filePath = Path.Combine(uploadsPath, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // 返回相對路徑
            return $"/{UploadFolder}/{uniqueFileName}";
        }

        /// <summary>
        /// 驗證圖片檔案格式和大小
        /// </summary>
        public bool ValidateImage(IFormFile imageFile)
        {
            // 檢查檔案是否為空
            if (imageFile == null || imageFile.Length == 0)
            {
                return false;
            }

            // 檢查檔案大小
            if (imageFile.Length > MaxFileSize)
            {
                return false;
            }

            // 檢查副檔名
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension))
            {
                return false;
            }

            return AllowedExtensions.Contains(fileExtension);
        }

        /// <summary>
        /// 刪除指定路徑的圖片
        /// </summary>
        public Task DeleteImageAsync(string? imagePath)
        {
            // 如果路徑為空，直接返回
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return Task.CompletedTask;
            }

            try
            {
                // 轉換相對路徑為絕對路徑
                // 移除開頭的 "/"
                var relativePath = imagePath.TrimStart('/');
                var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                // 如果檔案存在則刪除
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
            catch
            {
                // 安靜失敗 - 刪除失敗不應該中斷業務流程
                // 在實際應用中可以記錄日誌
            }

            return Task.CompletedTask;
        }
    }
}
