# Blog System - ASP.NET Core 8.0

一個功能完整的個人部落格系統，使用 ASP.NET Core 8.0 與 PostgreSQL 建置。

## 專案特色

- **現代化技術棧**: ASP.NET Core 8.0, Entity Framework Core, PostgreSQL
- **Clean Architecture**: 清晰的分層架構，易於維護和測試
- **Markdown 支援**: 使用 Markdig 解析，支援語法高亮、表格等進階功能
- **身份驗證**: Cookie Authentication + Google OAuth 2.0 (可選)
- **圖片上傳**: 本地檔案上傳，支援格式驗證和大小限制
- **全文檢索**: 基於 PostgreSQL 的搜尋功能
- **分類與標籤**: 完整的內容組織系統
- **響應式設計**: Bootstrap 5 打造的現代化介面
- **Docker 支援**: 提供完整的 Docker Compose 配置

## 系統需求

- .NET 8.0 SDK
- PostgreSQL 16+ (或使用 Docker)
- 5GB 以上可用磁碟空間

## 快速開始

### 使用 Docker Compose (推薦)

1. **複製專案並設定環境變數**:

```bash
git clone <repository-url>
cd dotnet-blog-speckit

# 複製環境變數範例檔案
cp .env.example .env

# 編輯 .env 檔案，設定 Google OAuth (可選)
nano .env
```

2. **啟動服務**:

```bash
docker-compose up -d
```

3. **訪問應用程式**:
   - 前台: http://localhost:8080
   - 健康檢查: http://localhost:8080/health

### 本地開發

1. **安裝 PostgreSQL**:

```bash
# Ubuntu/Debian
sudo apt-get install postgresql-16

# 或使用 Docker
docker run -d \
  --name blog-postgres \
  -e POSTGRES_DB=blogsystem \
  -e POSTGRES_USER=bloguser \
  -e POSTGRES_PASSWORD=blogpass123 \
  -p 5432:5432 \
  postgres:16-alpine
```

2. **設定連線字串**:

編輯 `BlogSystem.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=blogsystem;Username=bloguser;Password=blogpass123"
  },
  "AdminEmails": "your.email@example.com"
}
```

3. **執行資料庫遷移**:

```bash
cd BlogSystem.Web
dotnet ef database update
```

4. **啟動應用程式**:

```bash
dotnet run
```

5. **訪問應用程式**:
   - 前台: https://localhost:5001
   - 管理後台: https://localhost:5001/Admin

## 專案結構

```
dotnet-blog-speckit/
├── BlogSystem.Core/           # 核心層 - 實體、介面、列舉
│   ├── Entities/              # 資料實體 (BlogPost, Category, Tag, etc.)
│   ├── Interfaces/            # 服務介面
│   ├── Enums/                 # 列舉定義
│   └── Services/              # 核心服務邏輯
├── BlogSystem.Infrastructure/ # 基礎設施層 - 資料存取
│   ├── Data/                  # DbContext 與種子資料
│   └── Repositories/          # Repository 實作
├── BlogSystem.Web/            # 表現層 - Web 應用程式
│   ├── Areas/                 # 管理後台區域
│   │   └── Admin/             # 管理員功能
│   ├── Controllers/           # MVC 控制器
│   ├── Views/                 # Razor 視圖
│   ├── ViewModels/            # 視圖模型
│   ├── Middleware/            # 自訂中介軟體
│   └── wwwroot/               # 靜態檔案
├── docker/                    # Docker 相關檔案
│   └── init-db.sql            # 資料庫初始化腳本
├── docker-compose.yml         # Docker Compose 配置
└── Dockerfile                 # 應用程式 Docker 映像
```

## 主要功能

### 前台功能

- 文章瀏覽與分頁
- 依分類/標籤篩選文章
- 文章搜尋 (標題、內容、摘要)
- Markdown 內容渲染
- 響應式設計 (支援手機、平板、桌面)

### 管理後台

需要使用管理員帳號登入 (在 `appsettings.json` 中設定 `AdminEmails`)。

- 文章管理 (建立、編輯、刪除、發布)
- 分類管理
- 標籤管理
- 圖片上傳
- 操作日誌
- 儀表板統計

## 配置說明

### 必要配置

在 `appsettings.json` 或環境變數中設定:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=blogsystem;Username=bloguser;Password=yourpassword"
  },
  "AdminEmails": "admin@example.com,another.admin@example.com"
}
```

### Google OAuth (可選)

如需啟用 Google 登入:

1. 前往 [Google Cloud Console](https://console.cloud.google.com/)
2. 建立 OAuth 2.0 用戶端 ID
3. 設定授權重新導向 URI: `https://yourdomain.com/Admin/Auth/GoogleCallback`
4. 將憑證加入配置:

```json
{
  "Google": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "ClientSecret": "your-client-secret"
  }
}
```

## 資料庫遷移

### 建立新的遷移

```bash
cd BlogSystem.Web
dotnet ef migrations add YourMigrationName
```

### 套用遷移

```bash
dotnet ef database update
```

### 復原遷移

```bash
dotnet ef database update PreviousMigrationName
```

## 效能優化

本系統已實作以下優化:

- **Response Compression**: Gzip 壓縮 (HTML, CSS, JS, JSON, SVG)
- **靜態檔案快取**: 30 天瀏覽器快取
- **資料庫索引**: 針對常用查詢欄位建立索引
- **非同步操作**: 所有 I/O 操作皆使用非同步方法
- **健康檢查**: `/health` 端點監控應用程式與資料庫狀態

## 安全性

- **CSRF 防護**: 所有表單自動驗證 Anti-Forgery Token
- **身份驗證**: Cookie-based Authentication + OAuth 2.0
- **授權策略**: AdminOnly policy 限制後台存取
- **檔案上傳驗證**: 檔案類型與大小限制 (5MB)
- **SQL 注入防護**: 使用參數化查詢 (Entity Framework Core)
- **XSS 防護**: Razor 自動編碼輸出

## 監控與除錯

### 健康檢查

```bash
curl http://localhost:8080/health
```

回應:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "PostgreSQL Database": {
      "status": "Healthy"
    }
  }
}
```

### 查看日誌

```bash
# Docker 環境
docker-compose logs -f web

# 本地開發
# 日誌會輸出到 console
```

## 常見問題

### 無法連線到資料庫

確認 PostgreSQL 服務正在執行:

```bash
docker ps | grep postgres
# 或
sudo systemctl status postgresql
```

### Google OAuth 不工作

1. 確認 ClientId 和 ClientSecret 正確
2. 檢查重新導向 URI 是否符合 Google Console 設定
3. 確保使用 HTTPS (本地開發可使用 localhost)

### 圖片上傳失敗

1. 檢查 `wwwroot/uploads` 目錄權限
2. 確認檔案大小不超過 5MB
3. 僅支援 .jpg, .jpeg, .png, .gif 格式

## 開發團隊

本專案使用 Specify 工作流程開發。

## 授權

MIT License

## 貢獻指南

歡迎提交 Issue 或 Pull Request！

1. Fork 本專案
2. 建立功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交變更 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

## 聯絡資訊

如有問題或建議，請開啟 GitHub Issue。

---

使用 ASP.NET Core 8.0 與 PostgreSQL 打造 | 部署於 Docker
