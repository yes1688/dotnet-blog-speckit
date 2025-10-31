namespace BlogSystem.Core.Enums
{
    /// <summary>
    /// 文章狀態列舉
    /// </summary>
    public enum PostStatus
    {
        /// <summary>
        /// 草稿 (不對外顯示)
        /// </summary>
        Draft = 0,

        /// <summary>
        /// 已發布 (對外顯示)
        /// </summary>
        Published = 1
    }
}
