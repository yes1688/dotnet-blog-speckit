# 資料模型:個人部落格系統

**日期**: 2025-10-31
**資料庫**: PostgreSQL 15+
**ORM**: Entity Framework Core 8.0

## 實體關聯圖 (ERD)

```
┌─────────────┐         ┌──────────────┐
│  Category   │1       *│  BlogPost    │
│─────────────│◄────────│──────────────│
│ Id (PK)     │         │ Id (PK)      │
│ Name        │         │ Title        │
│ Slug        │         │ Slug         │
│ Description │         │ Content      │
└─────────────┘         │ Summary      │
                        │ PublishedAt  │
                        │ Status       │
                        │ CategoryId(FK│
                        └──────────────┘
                               *│
                                │
                                │*
                        ┌──────────────┐
                        │BlogPostTags  │(Join Table)
                        │──────────────│
                        │BlogPostId(FK)│
                        │TagId (FK)    │
                        └──────────────┘
                               *│
                                │
                                │1
                        ┌──────────────┐
                        │  Tag         │
                        │──────────────│
                        │ Id (PK)      │
                        │ Name         │
                        │ Slug         │
                        └──────────────┘

┌──────────────┐
│  AdminLog    │
│──────────────│
│ Id (PK)      │
│ Email        │
│ Action       │
│ Timestamp    │
└──────────────┘
```

## 實體定義

### 1. BlogPost (部落格文章)

**用途**: 儲存部落格文章的完整資訊

| 欄位名稱 | 型別 | 必填 | 說明 | 限制 |
|---------|------|------|------|------|
| Id | Guid | ✓ | 主鍵 | PK, 自動產生 |
| Title | string | ✓ | 文章標題 | 最大 200 字元,支援中文 |
| Slug | string | ✓ | URL 友善標題 | 最大 300 字元,唯一索引 |
| Content | string | ✓ | 文章內容 (Markdown) | 無限制 |
| Summary | string | | 文章摘要 | 最大 500 字元 |
| PublishedAt | DateTime | | 發布日期時間 | UTC 時間 |
| CreatedAt | DateTime | ✓ | 建立日期時間 | UTC 時間,自動設定 |
| UpdatedAt | DateTime | ✓ | 最後更新時間 | UTC 時間,自動更新 |
| Status | enum | ✓ | 文章狀態 | Draft=0, Published=1 |
| ViewCount | int | ✓ | 瀏覽次數 | 預設 0 |
| CategoryId | Guid | | 分類 ID | FK to Category |
| Category | Category | | 導航屬性 | |
| Tags | ICollection<Tag> | | 標籤集合 | 多對多關聯 |

**索引**:
- `IX_BlogPost_Slug` (唯一): 加速 URL 查詢
- `IX_BlogPost_Status_PublishedAt`: 加速已發布文章列表查詢
- `IX_BlogPost_CategoryId`: 加速分類篩選

**驗證規則**:
- Title 不可為空或僅空白
- Slug 格式: `{year}/{month}/{url-safe-title}`
- Published 狀態的文章必須有 PublishedAt
- Summary 若為空,自動從 Content 前 500 字元產生

---

### 2. Category (分類)

**用途**: 文章分類,每篇文章屬於一個分類

| 欄位名稱 | 型別 | 必填 | 說明 | 限制 |
|---------|------|------|------|------|
| Id | Guid | ✓ | 主鍵 | PK, 自動產生 |
| Name | string | ✓ | 分類名稱 | 最大 50 字元,唯一 |
| Slug | string | ✓ | URL 友善名稱 | 最大 100 字元,唯一 |
| Description | string | | 分類描述 | 最大 200 字元 |
| DisplayOrder | int | ✓ | 顯示順序 | 預設 0,較小值優先顯示 |
| BlogPosts | ICollection<BlogPost> | | 導航屬性 | |

**索引**:
- `IX_Category_Slug` (唯一): 加速 URL 查詢
- `IX_Category_DisplayOrder`: 排序用

---

### 3. Tag (標籤)

**用途**: 文章標籤,一篇文章可有多個標籤

| 欄位名稱 | 型別 | 必填 | 說明 | 限制 |
|---------|------|------|------|------|
| Id | Guid | ✓ | 主鍵 | PK, 自動產生 |
| Name | string | ✓ | 標籤名稱 | 最大 30 字元,唯一 |
| Slug | string | ✓ | URL 友善名稱 | 最大 50 字元,唯一 |
| UsageCount | int | ✓ | 使用次數 | 預設 0,自動計算 |
| BlogPosts | ICollection<BlogPost> | | 導航屬性 | |

**索引**:
- `IX_Tag_Slug` (唯一): 加速 URL 查詢
- `IX_Tag_UsageCount`: 支援「熱門標籤」查詢

---

### 4. AdminLog (管理員操作記錄)

**用途**: 記錄管理員登入登出與重要操作

| 欄位名稱 | 型別 | 必填 | 說明 | 限制 |
|---------|------|------|------|------|
| Id | Guid | ✓ | 主鍵 | PK, 自動產生 |
| Email | string | ✓ | 管理員 email | 最大 100 字元 |
| Action | string | ✓ | 操作類型 | Login, Logout, CreatePost, DeletePost 等 |
| Details | string | | 操作詳情 (JSON) | |
| Timestamp | DateTime | ✓ | 操作時間 | UTC 時間 |
| IpAddress | string | | IP 位址 | 最大 45 字元 (IPv6) |

**索引**:
- `IX_AdminLog_Email_Timestamp`: 查詢特定管理員操作記錄
- `IX_AdminLog_Timestamp`: 依時間排序

---

## Entity Framework Core 配置

### BlogPostConfiguration.cs

```csharp
public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.HasIndex(p => new { p.Status, p.PublishedAt });

        builder.Property(p => p.Content)
            .IsRequired();

        builder.Property(p => p.Summary)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // 關聯配置
        builder.HasOne(p => p.Category)
            .WithMany(c => c.BlogPosts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Tags)
            .WithMany(t => t.BlogPosts)
            .UsingEntity(j => j.ToTable("BlogPostTags"));
    }
}
```

## 資料庫遷移腳本

### 初始遷移

```bash
dotnet ef migrations add InitialCreate --project BlogSystem.Infrastructure --startup-project BlogSystem.Web
dotnet ef database update --project BlogSystem.Infrastructure --startup-project BlogSystem.Web
```

### 種子資料

```csharp
public static class DbInitializer
{
    public static async Task SeedAsync(BlogDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;  // 已有資料,跳過

        // 建立預設分類
        var categories = new[]
        {
            new Category { Name = "技術", Slug = "tech", DisplayOrder = 1 },
            new Category { Name = "生活", Slug = "life", DisplayOrder = 2 },
            new Category { Name = "隨筆", Slug = "notes", DisplayOrder = 3 }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
```

## 查詢範例

### 1. 取得已發布文章列表 (分頁)

```csharp
var posts = await _context.BlogPosts
    .Where(p => p.Status == PostStatus.Published)
    .Include(p => p.Category)
    .Include(p => p.Tags)
    .OrderByDescending(p => p.PublishedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 2. 依分類取得文章

```csharp
var posts = await _context.BlogPosts
    .Where(p => p.Status == PostStatus.Published && p.Category.Slug == categorySlug)
    .OrderByDescending(p => p.PublishedAt)
    .ToListAsync();
```

### 3. 搜尋文章

```csharp
var posts = await _context.BlogPosts
    .Where(p => p.Status == PostStatus.Published)
    .Where(p => EF.Functions.ILike(p.Title, $"%{keyword}%") ||
                EF.Functions.ILike(p.Content, $"%{keyword}%"))
    .OrderByDescending(p => p.PublishedAt)
    .ToListAsync();
```

### 4. 取得熱門標籤 (Top 10)

```csharp
var popularTags = await _context.Tags
    .OrderByDescending(t => t.UsageCount)
    .Take(10)
    .ToListAsync();
```

## 資料庫效能優化

### 1. 索引策略
- 為常用查詢欄位建立索引 (Status, PublishedAt, Slug)
- 複合索引支援多欄位排序與篩選
- 唯一索引確保資料完整性

### 2. 查詢優化
- 使用 `AsNoTracking()` 提升唯讀查詢效能
- 明確指定需要的欄位 (Select),避免載入整個實體
- 適時使用 Eager Loading (`Include`) 或 Explicit Loading

### 3. 資料庫連線
- 使用連線池 (Connection Pooling),預設已啟用
- 設定合理的 Timeout 值 (預設 30 秒)

## 資料完整性規則

1. **級聯刪除**:
   - 刪除分類時,文章的 CategoryId 設為 NULL (SetNull)
   - 刪除文章時,自動移除相關的 BlogPostTags 記錄 (Cascade)

2. **並發控制**:
   - 使用 EF Core 的 Optimistic Concurrency (RowVersion)
   - 更新時檢查 UpdatedAt 時間戳避免衝突

3. **資料驗證**:
   - 應用層驗證 (DataAnnotations + FluentValidation)
   - 資料庫層約束 (NOT NULL, UNIQUE, CHECK)

## 備份與還原

### 備份腳本 (Podman 環境)

```bash
# 備份資料庫
podman exec blog-postgres pg_dump -U bloguser blogdb > backup_$(date +%Y%m%d).sql

# 還原資料庫
podman exec -i blog-postgres psql -U bloguser blogdb < backup_20251031.sql
```

### 資料保留政策
- 文章: 永久保留 (可軟刪除)
- 管理員日誌: 保留 1 年,之後歸檔或刪除
- 定期備份: 每日自動備份,保留最近 30 天
