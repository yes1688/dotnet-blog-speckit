using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 圖片服務介面
    /// 處理圖片上傳、驗證、刪除
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// 上傳圖片並儲存到伺服器
        /// </summary>
        /// <param name="imageFile">上傳的圖片檔案</param>
        /// <returns>圖片的相對路徑 (如: /uploads/abc123.jpg)</returns>
        /// <exception cref="System.ArgumentNullException">檔案為 null</exception>
        /// <exception cref="System.ArgumentException">檔案無效或超過大小限制</exception>
        Task<string> UploadImageAsync(IFormFile imageFile);

        /// <summary>
        /// 驗證圖片檔案格式和大小
        /// </summary>
        /// <param name="imageFile">要驗證的圖片檔案</param>
        /// <returns>如果驗證通過返回 true</returns>
        bool ValidateImage(IFormFile imageFile);

        /// <summary>
        /// 刪除指定路徑的圖片
        /// </summary>
        /// <param name="imagePath">圖片相對路徑 (如: /uploads/abc123.jpg)</param>
        Task DeleteImageAsync(string? imagePath);
    }
}
