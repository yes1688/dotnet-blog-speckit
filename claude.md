# dotnet blog

## 專案概述

一個使用 ASP.NET Core 8.0 與 PostgreSQL 建置的功能完整個人部落格系統。採用 Clean Architecture 設計，支援 Markdown、圖片上傳、全文檢索、分類標籤管理等功能。

**當前狀態**: Phase 8 完成，所有核心功能已實作並測試通過。

## 開發原則

- **溝通語言**：過程中互動全程使用繁體中文
- **Linus Torvalds**: "Talk is cheap. Show me the code."
- 實用主義 > 完美設計
- 先跑通，再優化
- **禁止優雅降級** - 問題必須徹底根治
- **企業領先** - 修復頑固錯誤專家
- 優先編輯現有檔案，避免新建
- 遵循專案現有程式碼風格
- 錯誤處理必須完整，不允許忽略

## 技術架構

### 技術棧

- **後端**: ASP.NET Core 8.0
- **資料庫**: PostgreSQL 16+ (支援 uuid-ossp, pg_trgm 擴充)
- **ORM**: Entity Framework Core 8.0
- **前端**: Razor Pages + Bootstrap 5
- **Markdown 解析**: Markdig (支援 GFM, 表格, 語法高亮)
- **容器化**: Docker + Docker Compose

### 專案結構

```
BlogSystem.Core/           # 核心層 (實體、介面、服務邏輯)
BlogSystem.Infrastructure/ # 基礎設施層 (資料存取、Repository)
BlogSystem.Web/            # 表現層 (Controllers, Views, Middleware)
```

### 關鍵設計決策

1. **Clean Architecture**: 三層架構確保關注點分離
   - Core: 不依賴任何外部框架，純粹的業務邏輯
   - Infrastructure: 實作資料存取，依賴 Core
   - Web: 使用者介面層，依賴 Core 與 Infrastructure

2. **Repository Pattern**:
   - 泛型 Repository<T> 處理通用 CRUD
   - 特定 Repository (如 BlogPostRepository) 處理複雜查詢

3. **服務層設計**:
   - IBlogPostService: 文章管理邏輯
   - IAuthService: 身份驗證與授權
   - IImageService: 圖片上傳與驗證
   - ICategoryService / ITagService: 分類標籤管理
   - ISearchService: 全文檢索功能

4. **身份驗證策略**:
   - 主要: Cookie Authentication (7 天有效期)
   - 可選: Google OAuth 2.0 (環境變數配置)
   - 授權: AdminOnly Policy (基於 email 白名單)

5. **錯誤處理**:
   - GlobalErrorHandlingMiddleware: 全域異常捕獲
   - 自訂 Error.cshtml 與 NotFound.cshtml
   - 結構化日誌記錄 (含 RequestId 追蹤)

## 已實作功能

### Phase 1-3: 核心架構與資料層
- ✅ Entity Framework Core 設定
- ✅ PostgreSQL 資料庫配置
- ✅ 實體定義 (BlogPost, Category, Tag, AdminLog)
- ✅ Repository Pattern 實作
- ✅ 資料庫遷移與種子資料

### Phase 4-5: 前台功能
- ✅ 首頁文章列表 (分頁)
- ✅ 文章詳細頁面 (Markdown 渲染)
- ✅ 分類篩選
- ✅ 標籤篩選
- ✅ 全文檢索 (標題、內容、摘要)
- ✅ 瀏覽次數統計

### Phase 6-7: 管理後台
- ✅ Google OAuth 2.0 登入 (可選)
- ✅ Email 白名單授權
- ✅ 儀表板 (統計資訊)
- ✅ 文章管理 (CRUD, 草稿/發布)
- ✅ 分類管理 (CRUD)
- ✅ 標籤管理 (CRUD)
- ✅ 圖片上傳 (5MB 限制, 格式驗證)
- ✅ 操作日誌記錄

### Phase 8: 優化與部署
- ✅ 全域錯誤處理 (Error.cshtml, NotFound.cshtml, GlobalErrorHandlingMiddleware)
- ✅ Anti-Forgery Token 自動驗證
- ✅ 健康檢查端點 (/health)
- ✅ Response Compression (Gzip)
- ✅ 靜態檔案快取 (30 天)
- ✅ Docker Compose 配置 (Web + PostgreSQL)
- ✅ 資料庫初始化腳本 (init-db.sql)
- ✅ README.md 完整文件

## 資料庫 Schema

### 主要資料表

- **BlogPosts**: 文章主表
  - 欄位: Id, Title, Slug, Content, Summary, CoverImageUrl, PublishedAt, CreatedAt, UpdatedAt, Status, ViewCount, CategoryId
  - 索引: Slug (唯一), CategoryId, Status, PublishedAt

- **Categories**: 分類表
  - 欄位: Id, Name, Slug, Description, DisplayOrder
  - 索引: Slug (唯一)

- **Tags**: 標籤表
  - 欄位: Id, Name, Slug, UsageCount
  - 索引: Slug (唯一)

- **BlogPostTags**: 多對多關聯表
  - 複合主鍵: (BlogPostId, TagId)

- **AdminLogs**: 管理員操作日誌
  - 欄位: Id, Email, Action, Timestamp, IpAddress, Details

## 檔案位置參考

### 重要配置檔

- **Program.cs** (BlogSystem.Web/Program.cs:1): 應用程式進入點與 DI 配置
- **appsettings.json**: 資料庫連線字串、管理員 Email 清單
- **docker-compose.yml**: 容器編排配置
- **docker/init-db.sql**: PostgreSQL 初始化腳本

### 核心服務

- **BlogPostService** (BlogSystem.Core/Services/BlogPostService.cs:1)
- **AuthService** (BlogSystem.Core/Services/AuthService.cs:1)
- **ImageService** (BlogSystem.Core/Services/ImageService.cs:1)
- **SearchService** (BlogSystem.Core/Services/SearchService.cs:1)

### 中介軟體

- **GlobalErrorHandlingMiddleware** (BlogSystem.Web/Middleware/GlobalErrorHandlingMiddleware.cs:1)

### 關鍵控制器

- **Admin/PostsController** (BlogSystem.Web/Areas/Admin/Controllers/PostsController.cs:1): 文章管理
- **Admin/CategoriesController** (BlogSystem.Web/Areas/Admin/Controllers/CategoriesController.cs:1): 分類管理
- **Admin/AuthController** (BlogSystem.Web/Areas/Admin/Controllers/AuthController.cs:1): 身份驗證

## 已知問題與限制

### 已解決
- ✅ Category/Tag 路由 404 問題 (commit: 6e0d6b4)
- ✅ DbInitializer 編譯錯誤 (移除不存在的 CreatedAt 屬性)
- ✅ AddNpgSql 缺少套件 (已安裝 AspNetCore.HealthChecks.Npgsql 9.0.0)

### 當前限制
- 無整合測試專案 (未來可新增 BlogSystem.Tests)
- 圖片儲存僅支援本地檔案系統 (未來可整合雲端儲存)
- 無快取機制 (可考慮加入 Redis 或 MemoryCache)
- 無 CDN 整合
- 無留言功能

## 部署說明

### Docker Compose (推薦)

```bash
# 啟動服務
docker-compose up -d

# 查看日誌
docker-compose logs -f web

# 停止服務
docker-compose down
```

### 本地開發

```bash
# 還原套件
dotnet restore

# 執行遷移
cd BlogSystem.Web
dotnet ef database update

# 啟動應用程式
dotnet run
```

### 環境變數

必要配置:
- `ConnectionStrings__DefaultConnection`: PostgreSQL 連線字串
- `AdminEmails`: 管理員 email 清單 (逗號分隔)

可選配置:
- `Google__ClientId`: Google OAuth Client ID
- `Google__ClientSecret`: Google OAuth Client Secret

## 效能指標

- **Response Compression**: 已啟用 Gzip (HTML, CSS, JS, JSON, SVG)
- **靜態檔案快取**: 30 天瀏覽器快取
- **資料庫查詢**: 所有查詢使用非同步方法
- **索引優化**: 主要查詢欄位已建立索引

## 安全性措施

- ✅ CSRF 防護 (Auto Validate Anti-Forgery Token)
- ✅ SQL 注入防護 (參數化查詢)
- ✅ XSS 防護 (Razor 自動編碼)
- ✅ 檔案上傳驗證 (類型、大小、擴展名)
- ✅ 身份驗證與授權 (Cookie + OAuth)
- ✅ HTTPS 重定向 (生產環境)

## 未來改進方向

1. **測試覆蓋率**:
   - 單元測試 (xUnit + Moq)
   - 整合測試 (WebApplicationFactory)
   - E2E 測試 (Playwright)

2. **效能優化**:
   - Redis 快取層
   - 圖片 CDN 整合
   - 查詢結果快取
   - 資料庫連線池調校

3. **功能擴充**:
   - 留言系統
   - RSS Feed
   - 文章草稿自動儲存
   - Markdown 即時預覽
   - SEO 優化 (sitemap, meta tags)
   - 多語言支援

4. **監控與日誌**:
   - Serilog 結構化日誌
   - Application Insights 整合
   - Prometheus + Grafana 監控

5. **CI/CD**:
   - GitHub Actions 自動化測試
   - 自動部署到雲端平台
   - 容器映像掃描

## 版本歷史

- **v1.0** (2025-10-31): 初版發布
  - 完成所有核心功能
  - Docker 部署支援
  - 完整文件

## 聯絡資訊

如有問題或建議，請參考 README.md 或提交 GitHub Issue。
