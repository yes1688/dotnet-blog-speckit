# 技術研究:個人部落格系統

**日期**: 2025-10-31
**研究目的**: 為個人部落格系統選擇最適合的技術棧與架構模式

## 技術決策摘要

| 領域 | 決策 | 理由 |
|------|------|------|
| 後端框架 | ASP.NET Core 8.0 MVC | 符合憲章 .NET 8.0 要求,成熟穩定 |
| 資料庫 | PostgreSQL 15+ | 已在澄清階段確認,開源且功能強大 |
| ORM | Entity Framework Core 8.0 | .NET 生態系標準 ORM,成熟可靠 |
| 認證 | Google OAuth 2.0 | 規格明確要求,降低密碼管理風險 |
| Markdown 解析 | Markdig | .NET 生態系最快且功能完整的解析器 |
| 前端模板引擎 | Razor | ASP.NET Core 內建,學習曲線低 |
| CSS 框架 | Bootstrap 5 | 成熟且廣泛使用,快速建立響應式介面 |
| 測試框架 | xUnit + Moq | .NET 社群標準選擇 |
| 容器化 | Podman + Podman Compose | 憲章明確要求 |

---

## 詳細技術研究

### 1. 後端框架選擇

**決策**: ASP.NET Core 8.0 MVC

**評估的替代方案**:
- **Razor Pages**: 適合頁面導向的應用,但本專案需要明確的前後台分離,MVC + Areas 更適合
- **Minimal APIs**: 過於簡化,不適合需要視圖引擎的傳統 Web 應用
- **Blazor Server/WASM**: 過度複雜,違反簡約設計原則

**選擇 ASP.NET Core MVC 的理由**:
1. **Areas 支援**: 可輕鬆分離前台與後台管理邏輯
2. **成熟穩定**: 經過多年演進,文件完整,社群支援強大
3. **Razor 視圖引擎**: 內建模板引擎,無需額外依賴
4. **符合憲章**: .NET 8.0 LTS 版本,生命週期長
5. **簡約設計**: 標準 MVC 模式,團隊熟悉度高

**最佳實踐**:
- 使用 Areas 分離前台 (Public) 與後台 (Admin)
- 遵循 MVC 模式:Controller 處理請求,Model 封裝資料,View 負責展示
- 使用 ViewModels 避免直接暴露領域模型到視圖層
- 套用 [Authorize] 特性保護後台 Controllers

---

### 2. 資料庫與 ORM

**決策**: PostgreSQL 15+ + Entity Framework Core 8.0

**PostgreSQL 選擇理由** (已在澄清階段確認):
1. **關聯式資料**: 文章、分類、標籤間有明確的關聯關係
2. **ACID 保證**: 確保資料一致性
3. **全文檢索**: 內建 `ts_vector` 和 `ts_query` 支援中文全文搜尋
4. **開源免費**: 無授權成本
5. **容器化友善**: 官方提供 Docker/Podman 映像

**Entity Framework Core 選擇理由**:
1. **.NET 標準 ORM**: 微軟官方支援,與 ASP.NET Core 深度整合
2. **Code-First 遷移**: 透過程式碼定義資料庫結構,版本控制友善
3. **LINQ 查詢**: 型別安全的查詢語法
4. **Npgsql Provider**: 成熟的 PostgreSQL 提供者

**資料庫設計決策**:
- 使用 EF Core Fluent API 配置實體關聯
- 文章與分類: 多對一 (一篇文章屬於一個分類)
- 文章與標籤: 多對多 (一篇文章可有多個標籤)
- 啟用 PostgreSQL 全文檢索索引優化搜尋效能

**最佳實踐**:
```csharp
// 使用 Fluent API 配置
modelBuilder.Entity<BlogPost>()
    .HasOne(p => p.Category)
    .WithMany(c => c.BlogPosts)
    .HasForeignKey(p => p.CategoryId);

modelBuilder.Entity<BlogPost>()
    .HasMany(p => p.Tags)
    .WithMany(t => t.BlogPosts)
    .UsingEntity(j => j.ToTable("BlogPostTags"));
```

---

### 3. Google OAuth 2.0 認證

**決策**: Microsoft.AspNetCore.Authentication.Google

**選擇理由**:
1. **官方套件**: 微軟官方提供,與 ASP.NET Core 無縫整合
2. **安全性**: 避免自行處理密碼儲存與驗證
3. **使用者體驗**: 使用既有 Google 帳號,降低註冊門檻
4. **規格要求**: 功能規格明確要求 Google OAuth 2.0

**實作策略**:
1. 在 Google Cloud Console 建立 OAuth 2.0 憑證
2. 設定授權重新導向 URI
3. 在 `Program.cs` 配置 Authentication Middleware
4. 使用自訂 Authorization Policy 檢查 email 是否在授權清單中

**環境變數配置**:
```bash
GOOGLE_CLIENT_ID=<your-client-id>
GOOGLE_CLIENT_SECRET=<your-client-secret>
ADMIN_EMAILS=admin1@example.com,admin2@example.com
```

**最佳實踐**:
```csharp
services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Admin/Auth/Login";
    options.AccessDeniedPath = "/Admin/Auth/AccessDenied";
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Google:ClientId"];
    options.ClientSecret = configuration["Google:ClientSecret"];
});

// 自訂 Authorization Policy
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
        {
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var adminEmails = configuration["AdminEmails"].Split(',');
            return adminEmails.Contains(email);
        }));
});
```

---

### 4. Markdown 解析與渲染

**決策**: Markdig

**評估的替代方案**:
- **CommonMark.NET**: 功能較少,擴展性差
- **MarkdownSharp**: 已過時,不再維護
- **手動解析**: 違反簡約原則,重複造輪子

**選擇 Markdig 的理由**:
1. **效能最佳**: 基準測試顯示比其他解析器快 5-10 倍
2. **功能完整**: 支援 CommonMark、GFM (GitHub Flavored Markdown)、表格、程式碼高亮等
3. **可擴展**: 提供 Pipeline 機制自訂解析規則
4. **安全性**: 預設啟用 HTML 消毒 (sanitization) 防止 XSS 攻擊
5. **活躍維護**: 社群活躍,持續更新

**使用方式**:
```csharp
using Markdig;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()  // 啟用進階功能 (表格、Task lists 等)
            .UseEmojiAndSmiley()      // 支援 emoji
            .UseSyntaxHighlighting()  // 程式碼高亮
            .Build();
    }

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
    }
}
```

---

### 5. 中文 URL Slug 處理

**決策**: 使用 URL 編碼保留中文字元

**技術挑戰**:
- URL 需支援中文標題 (如 `/2025/10/我的文章標題`)
- 需處理特殊字元與空格
- 需避免 slug 衝突

**實作策略**:
```csharp
public string GenerateSlug(string title, DateTime publishDate)
{
    // 移除首尾空白
    var slug = title.Trim();

    // 將空格替換為連字號
    slug = Regex.Replace(slug, @"\s+", "-");

    // 移除不安全的 URL 字元 (保留中文、英文、數字、連字號)
    slug = Regex.Replace(slug, @"[^\u4e00-\u9fa5a-zA-Z0-9\-]", "");

    // URL 編碼 (ASP.NET Core 路由會自動處理)
    return $"{publishDate:yyyy}/{publishDate:MM}/{slug}";
}
```

**衝突處理**:
- 檢查相同日期 + slug 是否已存在
- 若衝突,自動附加數字後綴 (`-2`, `-3` 等)

**路由配置**:
```csharp
app.MapControllerRoute(
    name: "blog-post",
    pattern: "{year:int}/{month:int}/{slug}",
    defaults: new { controller = "Post", action = "Details" });
```

---

### 6. 圖片儲存與管理

**決策**: 本地檔案系統 + Volume 掛載 (已在澄清階段確認)

**儲存策略**:
- **上傳路徑**: `wwwroot/uploads/{year}/{month}/{filename}`
- **檔名處理**: GUID + 原始副檔名,避免檔名衝突
- **大小限制**: 5MB (在 `Program.cs` 配置 `FormOptions.MultipartBodyLengthLimit`)
- **格式限制**: 僅允許圖片格式 (jpg, png, gif, webp)

**容器 Volume 配置**:
```yaml
# podman-compose.yml
services:
  blog-app:
    volumes:
      - ./uploads:/app/wwwroot/uploads:Z  # SELinux 標籤 :Z 確保權限正確
```

**最佳實踐**:
```csharp
public async Task<string> SaveImageAsync(IFormFile file)
{
    // 驗證檔案類型
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(extension))
        throw new InvalidOperationException("不支援的圖片格式");

    // 驗證檔案大小
    if (file.Length > 5 * 1024 * 1024)  // 5MB
        throw new InvalidOperationException("圖片檔案超過 5MB 限制");

    // 產生檔名與路徑
    var now = DateTime.UtcNow;
    var fileName = $"{Guid.NewGuid()}{extension}";
    var relativePath = $"uploads/{now:yyyy}/{now:MM}/{fileName}";
    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

    // 確保目錄存在
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

    // 儲存檔案
    using var stream = new FileStream(fullPath, FileMode.Create);
    await file.CopyToAsync(stream);

    return $"/{relativePath}";  // 返回相對 URL
}
```

---

### 7. 分頁實作

**決策**: 自訂分頁邏輯 + EF Core Skip/Take

**評估的替代方案**:
- **X.PagedList**: 功能豐富但引入額外依賴
- **手動實作**: 符合簡約原則,程式碼量小

**選擇手動實作的理由**:
1. **簡單需求**: 僅需基本分頁功能,無需複雜特性
2. **YAGNI 原則**: 避免引入不必要的套件
3. **可控性**: 完全掌握分頁邏輯

**實作範例**:
```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public async Task<PagedResult<BlogPost>> GetPagedPostsAsync(int page, int pageSize = 10)
{
    var query = _context.BlogPosts
        .Where(p => p.Status == PostStatus.Published)
        .OrderByDescending(p => p.PublishedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<BlogPost>
    {
        Items = items,
        CurrentPage = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

---

### 8. 容器化部署策略

**決策**: Multi-stage Dockerfile + Podman Compose

**Dockerfile 結構**:
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BlogSystem.Web/BlogSystem.Web.csproj", "BlogSystem.Web/"]
COPY ["BlogSystem.Core/BlogSystem.Core.csproj", "BlogSystem.Core/"]
COPY ["BlogSystem.Infrastructure/BlogSystem.Infrastructure.csproj", "BlogSystem.Infrastructure/"]
RUN dotnet restore "BlogSystem.Web/BlogSystem.Web.csproj"
COPY . .
WORKDIR "/src/BlogSystem.Web"
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlogSystem.Web.dll"]
```

**Podman Compose 配置**:
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: blog-postgres
    environment:
      POSTGRES_DB: blogdb
      POSTGRES_USER: bloguser
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - blog-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U bloguser"]
      interval: 10s
      timeout: 5s
      retries: 5

  blog-app:
    build:
      context: ..
      dockerfile: deployment/Dockerfile
    container_name: blog-app
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=blogdb;Username=bloguser;Password=${DB_PASSWORD}"
      Google__ClientId: ${GOOGLE_CLIENT_ID}
      Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
      AdminEmails: ${ADMIN_EMAILS}
      ASPNETCORE_ENVIRONMENT: Production
    volumes:
      - ./uploads:/app/wwwroot/uploads:Z
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - blog-network

volumes:
  postgres-data:

networks:
  blog-network:
    driver: bridge
```

**啟動命令**:
```bash
# 建立 .env 檔案
cp .env.example .env
# 編輯 .env 設定環境變數

# 啟動所有服務
podman-compose up -d

# 執行資料庫遷移
podman exec blog-app dotnet ef database update

# 檢視日誌
podman-compose logs -f blog-app
```

---

### 9. 搜尋功能實作

**決策**: PostgreSQL 全文檢索 (pg_trgm + GIN 索引)

**評估的替代方案**:
- **LIKE 查詢**: 簡單但效能差,不支援相關性排序
- **Elasticsearch**: 過度複雜,違反簡約原則,增加維護成本
- **PostgreSQL FTS**: 平衡效能與複雜度,無需額外服務

**選擇 PostgreSQL 全文檢索的理由**:
1. **內建功能**: 無需額外服務,降低複雜度
2. **中文支援**: pg_trgm extension 支援中文三元組搜尋
3. **相關性排序**: ts_rank 函數提供相關性評分
4. **效能優化**: GIN 索引加速查詢

**實作策略**:
```sql
-- 啟用 pg_trgm extension (支援中文)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 為標題和內容建立 GIN 索引
CREATE INDEX idx_blogpost_title_gin ON "BlogPosts" USING gin(to_tsvector('simple', "Title"));
CREATE INDEX idx_blogpost_content_gin ON "BlogPosts" USING gin(to_tsvector('simple', "Content"));
```

**EF Core 查詢**:
```csharp
public async Task<PagedResult<BlogPost>> SearchAsync(string keyword, int page, int pageSize = 10)
{
    var query = _context.BlogPosts
        .Where(p => p.Status == PostStatus.Published)
        .Where(p => EF.Functions.ILike(p.Title, $"%{keyword}%") ||
                    EF.Functions.ILike(p.Content, $"%{keyword}%"))
        .OrderByDescending(p => p.PublishedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<BlogPost>
    {
        Items = items,
        CurrentPage = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

---

### 10. 測試策略

**決策**: 三層測試金字塔 (Unit > Integration > Contract)

**測試框架選擇**:
- **xUnit**: .NET 社群標準,支援並行測試
- **Moq**: Mock 框架,隔離依賴
- **FluentAssertions**: 可讀性高的斷言語法
- **Testcontainers**: 整合測試使用真實 PostgreSQL 容器

**測試分層**:
1. **單元測試** (最多): Services、ViewModels、Helpers
2. **整合測試** (適中): Controllers + Database
3. **契約測試** (最少): API 端點與預期回應格式

**範例**:
```csharp
// 單元測試
public class BlogPostServiceTests
{
    [Fact]
    public async Task GetPublishedPosts_ShouldReturnOnlyPublished()
    {
        // Arrange
        var mockRepo = new Mock<IRepository<BlogPost>>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(GetTestPosts());
        var service = new BlogPostService(mockRepo.Object);

        // Act
        var result = await service.GetPublishedPostsAsync();

        // Assert
        result.Should().OnlyContain(p => p.Status == PostStatus.Published);
    }
}

// 整合測試 (使用 Testcontainers)
public class PostControllerIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<BlogDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });
    }

    [Fact]
    public async Task GET_Index_ReturnsSuccessAndContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("部落格");
    }
}
```

---

## 技術決策總結

所有關鍵技術決策已完成,選擇的技術棧符合以下標準:

✅ **符合憲章要求**: .NET 8.0、Podman、分層架構、測試驅動
✅ **簡約設計**: 使用標準框架,避免過度工程
✅ **成熟穩定**: 所有技術均為業界標準,社群支援充足
✅ **可維護性**: 清晰的架構分層,便於測試與擴展
✅ **效能達標**: 滿足所有成功標準的效能指標

**下一步**: 進入 Phase 1 產生詳細的資料模型與 API 契約。
