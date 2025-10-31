using BlogSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 標籤服務介面
    /// </summary>
    public interface ITagService
    {
        /// <summary>
        /// 取得所有標籤 (按名稱排序)
        /// </summary>
        /// <returns>標籤清單</returns>
        Task<IEnumerable<Tag>> GetAllTagsAsync();

        /// <summary>
        /// 根據 ID 取得標籤
        /// </summary>
        /// <param name="id">標籤 ID</param>
        /// <returns>標籤實體,若不存在則返回 null</returns>
        Task<Tag?> GetTagByIdAsync(Guid id);

        /// <summary>
        /// 建立新標籤
        /// </summary>
        /// <param name="name">標籤名稱</param>
        /// <returns>新建立的標籤實體</returns>
        /// <exception cref="ArgumentException">當名稱為空或重複時拋出</exception>
        Task<Tag> CreateTagAsync(string name);

        /// <summary>
        /// 更新標籤
        /// </summary>
        /// <param name="id">標籤 ID</param>
        /// <param name="name">新的標籤名稱</param>
        /// <returns>更新後的標籤實體</returns>
        /// <exception cref="KeyNotFoundException">當標籤不存在時拋出</exception>
        /// <exception cref="ArgumentException">當名稱為空或重複時拋出</exception>
        Task<Tag> UpdateTagAsync(Guid id, string name);

        /// <summary>
        /// 刪除標籤
        /// </summary>
        /// <param name="id">標籤 ID</param>
        /// <exception cref="KeyNotFoundException">當標籤不存在時拋出</exception>
        Task DeleteTagAsync(Guid id);

        /// <summary>
        /// 根據標籤 ID 取得相關文章 (只返回已發布的)
        /// </summary>
        /// <param name="tagId">標籤 ID</param>
        /// <param name="page">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>分頁結果</returns>
        Task<(IEnumerable<BlogPost> Posts, int TotalCount)> GetPostsByTagAsync(Guid tagId, int page = 1, int pageSize = 10);

        /// <summary>
        /// 解析逗號分隔的標籤字串為標籤清單
        /// </summary>
        /// <param name="tagString">逗號分隔的標籤字串 (例如: "C#, .NET, ASP.NET")</param>
        /// <returns>標籤名稱清單 (去除重複和前後空白)</returns>
        IEnumerable<string> ParseTagsFromString(string tagString);
    }
}
