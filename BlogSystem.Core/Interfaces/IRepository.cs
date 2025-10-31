using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlogSystem.Core.Interfaces
{
    /// <summary>
    /// 通用 Repository 介面
    /// </summary>
    /// <typeparam name="T">實體類型</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// 根據 ID 取得單一實體
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// 取得所有實體
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根據條件查詢實體
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 新增實體
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// 更新實體
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// 刪除實體
        /// </summary>
        Task DeleteAsync(T entity);

        /// <summary>
        /// 根據 ID 刪除實體
        /// </summary>
        Task DeleteByIdAsync(Guid id);

        /// <summary>
        /// 檢查實體是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 計算符合條件的實體數量
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
