using BlogSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogSystem.Infrastructure.Data
{
    /// <summary>
    /// 資料庫種子資料初始化器
    /// 用於建立示範資料以便測試和展示
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// 初始化資料庫並植入種子資料
        /// </summary>
        public static async Task InitializeAsync(BlogDbContext context, ILogger logger)
        {
            try
            {
                // 確保資料庫已建立
                await context.Database.EnsureCreatedAsync();

                // 套用未執行的遷移
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                }

                // 檢查是否已有資料
                if (await context.Categories.AnyAsync())
                {
                    logger.LogInformation("Database already seeded. Skipping seed data initialization.");
                    return;
                }

                logger.LogInformation("Seeding database with initial data...");

                // 建立分類
                var categories = new[]
                {
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "技術筆記",
                        Slug = "tech",
                        Description = "程式開發、系統架構與技術研究",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "生活隨筆",
                        Slug = "life",
                        Description = "日常生活、旅遊與心情分享",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "學習資源",
                        Slug = "learning",
                        Description = "書籍推薦、線上課程與學習心得",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Created {Count} categories", categories.Length);

                // 建立標籤
                var tags = new[]
                {
                    new Tag { Id = Guid.NewGuid(), Name = "ASP.NET Core", Slug = "aspnet-core", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "C#", Slug = "csharp", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "PostgreSQL", Slug = "postgresql", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "Docker", Slug = "docker", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "架構設計", Slug = "architecture", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "效能優化", Slug = "performance", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "閱讀", Slug = "reading", CreatedAt = DateTime.UtcNow },
                    new Tag { Id = Guid.NewGuid(), Name = "旅行", Slug = "travel", CreatedAt = DateTime.UtcNow }
                };

                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();
                logger.LogInformation("Created {Count} tags", tags.Length);

                // 建立範例文章
                var techCategory = categories[0];
                var lifeCategory = categories[1];

                var posts = new[]
                {
                    new BlogPost
                    {
                        Id = Guid.NewGuid(),
                        Title = "使用 ASP.NET Core 8.0 建立部落格系統",
                        Slug = "building-blog-with-aspnet-core-8",
                        Summary = "分享使用 ASP.NET Core 8.0 與 PostgreSQL 建立完整部落格系統的經驗",
                        Content = @"# 前言

最近使用 ASP.NET Core 8.0 建立了一個完整的部落格系統，這篇文章將分享開發過程中的一些心得。

## 技術選型

- **後端框架**: ASP.NET Core 8.0
- **資料庫**: PostgreSQL
- **ORM**: Entity Framework Core
- **前端**: Razor Pages + Bootstrap

## 專案架構

採用了 Clean Architecture 的設計理念：

1. **Core Layer**: 包含實體、介面定義
2. **Infrastructure Layer**: 實作資料存取、外部服務
3. **Web Layer**: MVC 控制器、視圖

## 重點功能

### Markdown 支援
使用 Markdig 套件處理 Markdown 格式，支援語法高亮、表格等進階功能。

### 圖片上傳
實作了本地檔案上傳功能，包含檔案大小限制、格式驗證。

### 搜尋功能
使用 Entity Framework Core 的 LINQ 查詢實作全文檢索。

## 總結

ASP.NET Core 8.0 提供了強大的功能和優秀的效能，非常適合建立這類內容管理系統。",
                        CategoryId = techCategory.Id,
                        Status = PostStatus.Published,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        ViewCount = 150
                    },
                    new BlogPost
                    {
                        Id = Guid.NewGuid(),
                        Title = "PostgreSQL 效能調校實戰",
                        Slug = "postgresql-performance-tuning",
                        Summary = "PostgreSQL 資料庫效能優化的實用技巧與最佳實踐",
                        Content = @"# PostgreSQL 效能調校

PostgreSQL 是一個功能強大的開源資料庫，但要發揮最佳效能需要適當的調校。

## 索引優化

### 選擇正確的索引類型
- B-Tree: 適合大多數情況
- Hash: 等值查詢
- GIN/GiST: 全文檢索

### 索引維護
定期執行 VACUUM 和 ANALYZE 指令。

## 查詢優化

使用 EXPLAIN ANALYZE 分析查詢計畫。

## 連線池配置

使用 pgBouncer 管理資料庫連線。",
                        CategoryId = techCategory.Id,
                        Status = PostStatus.Published,
                        PublishedAt = DateTime.UtcNow.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        ViewCount = 95
                    },
                    new BlogPost
                    {
                        Id = Guid.NewGuid(),
                        Title = "2024 年度閱讀清單",
                        Slug = "2024-reading-list",
                        Summary = "今年讀過的好書推薦",
                        Content = @"# 2024 年度閱讀清單

分享今年讀過的幾本好書。

## 技術類

1. **Clean Architecture** - Robert C. Martin
2. **Designing Data-Intensive Applications** - Martin Kleppmann

## 非技術類

1. **原子習慣** - James Clear
2. **深度工作力** - Cal Newport

這些書都給了我很多啟發！",
                        CategoryId = lifeCategory.Id,
                        Status = PostStatus.Published,
                        PublishedAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        ViewCount = 42
                    }
                };

                await context.BlogPosts.AddRangeAsync(posts);
                await context.SaveChangesAsync();
                logger.LogInformation("Created {Count} blog posts", posts.Length);

                // 為文章加上標籤
                posts[0].Tags.Add(tags[0]); // ASP.NET Core
                posts[0].Tags.Add(tags[1]); // C#
                posts[0].Tags.Add(tags[4]); // 架構設計

                posts[1].Tags.Add(tags[2]); // PostgreSQL
                posts[1].Tags.Add(tags[5]); // 效能優化

                posts[2].Tags.Add(tags[6]); // 閱讀

                await context.SaveChangesAsync();
                logger.LogInformation("Assigned tags to posts");

                logger.LogInformation("Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
