# 快速入門指南:個人部落格系統

**版本**: 1.0.0
**日期**: 2025-10-31
**目標讀者**: 開發者

## 前置需求

### 必要軟體

- **.NET SDK 8.0+**: [下載連結](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Podman**: [安裝指南](https://podman.io/getting-started/installation)
- **podman-compose**: `pip install podman-compose`
- **PostgreSQL 客戶端工具** (選用): `psql`, pgAdmin, DBeaver

### 環境檢查

```bash
# 檢查 .NET 版本
dotnet --version  # 應顯示 8.0.x

# 檢查 Podman 版本
podman --version  # 應顯示 4.0+

# 檢查 podman-compose
podman-compose --version
```

---

## 快速啟動 (5 分鐘)

### 步驟 1: 取得程式碼

```bash
git clone <repository-url>
cd dotnet-blog-speckit
git checkout 001-personal-blog
```

### 步驟 2: 設定環境變數

```bash
cd deployment
cp .env.example .env
```

編輯 `.env` 檔案:

```bash
# 資料庫密碼
DB_PASSWORD=your_secure_password

# Google OAuth 2.0 憑證 (需先在 Google Cloud Console 建立)
GOOGLE_CLIENT_ID=your_client_id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_client_secret

# 管理員 email (逗號分隔,無空格)
ADMIN_EMAILS=admin@example.com,user@example.com
```

### 步驟 3: 啟動容器服務

```bash
# 建構並啟動所有服務
podman-compose up -d

# 查看服務狀態
podman-compose ps

# 查看應用程式日誌
podman-compose logs -f blog-app
```

### 步驟 4: 執行資料庫遷移

```bash
# 第一次啟動需執行遷移
podman exec blog-app dotnet ef database update --project BlogSystem.Infrastructure

# 或在容器外執行
cd ../BlogSystem
dotnet ef database update --project BlogSystem.Infrastructure --startup-project BlogSystem.Web
```

### 步驟 5: 訪問應用程式

- **前台**: http://localhost:8080
- **後台**: http://localhost:8080/Admin

---

## 開發環境設定

### 步驟 1: 還原 NuGet 套件

```bash
cd BlogSystem
dotnet restore
```

### 步驟 2: 建構專案

```bash
dotnet build
```

### 步驟 3: 啟動開發用 PostgreSQL (容器)

```bash
cd deployment
podman-compose up -d postgres
```

### 步驟 4: 更新 appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=blogdb;Username=bloguser;Password=your_password"
  },
  "Google": {
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret"
  },
  "AdminEmails": "admin@example.com"
}
```

### 步驟 5: 執行遷移並啟動應用程式

```bash
cd BlogSystem.Web
dotnet ef database update --project ../BlogSystem.Infrastructure
dotnet run
```

應用程式將在 http://localhost:5000 啟動。

---

## Google OAuth 2.0 設定

### 步驟 1: 建立 Google Cloud 專案

1. 前往 [Google Cloud Console](https://console.cloud.google.com/)
2. 建立新專案或選擇現有專案
3. 啟用「Google+ API」

### 步驟 2: 建立 OAuth 2.0 憑證

1. 導航至「API 和服務」→「憑證」
2. 點擊「建立憑證」→「OAuth 客戶端 ID」
3. 應用程式類型選擇「網頁應用程式」
4. 設定授權重新導向 URI:
   - 開發環境: `http://localhost:5000/signin-google`
   - 生產環境: `https://yourdomain.com/signin-google`
5. 複製「客戶端 ID」和「客戶端密鑰」

### 步驟 3: 更新環境變數

將複製的憑證貼到 `.env` 或 `appsettings.Development.json`。

---

## 常用開發命令

### 資料庫操作

```bash
# 建立新遷移
dotnet ef migrations add MigrationName --project BlogSystem.Infrastructure --startup-project BlogSystem.Web

# 更新資料庫
dotnet ef database update --project BlogSystem.Infrastructure --startup-project BlogSystem.Web

# 回滾到指定遷移
dotnet ef database update PreviousMigrationName --project BlogSystem.Infrastructure --startup-project BlogSystem.Web

# 移除最後一個遷移 (尚未套用到資料庫時)
dotnet ef migrations remove --project BlogSystem.Infrastructure --startup-project BlogSystem.Web

# 查看所有遷移
dotnet ef migrations list --project BlogSystem.Infrastructure --startup-project BlogSystem.Web
```

### 執行測試

```bash
# 執行所有測試
dotnet test

# 執行特定測試專案
dotnet test BlogSystem.Tests/BlogSystem.Tests.csproj

# 執行測試並產生覆蓋率報告
dotnet test --collect:"XPlat Code Coverage"
```

### 程式碼格式化

```bash
# 格式化所有程式碼
dotnet format

# 檢查格式但不修改
dotnet format --verify-no-changes
```

---

## 專案結構導覽

```
BlogSystem/
├── BlogSystem.Web/              # Web 應用程式 (Presentation 層)
│   ├── Areas/Admin/             # 後台管理
│   ├── Controllers/             # 前台控制器
│   ├── Views/                   # Razor 視圖
│   └── Program.cs               # 應用程式進入點
│
├── BlogSystem.Core/             # 核心業務邏輯
│   ├── Entities/                # 領域實體
│   ├── Interfaces/              # 介面定義
│   └── Services/                # 業務邏輯服務
│
├── BlogSystem.Infrastructure/   # 基礎設施層
│   ├── Data/                    # EF Core DbContext
│   └── Repositories/            # Repository 實作
│
└── BlogSystem.Tests/            # 測試專案
    ├── Unit/                    # 單元測試
    ├── Integration/             # 整合測試
    └── Contract/                # 契約測試
```

---

## 常見問題排查

### 1. 容器無法啟動

**問題**: `podman-compose up` 失敗

**解決方案**:
```bash
# 檢查 Podman 服務是否運行
podman info

# 檢查 .env 檔案是否存在且格式正確
cat deployment/.env

# 查看詳細錯誤訊息
podman-compose up
```

### 2. 資料庫連線失敗

**問題**: 應用程式無法連線到 PostgreSQL

**解決方案**:
```bash
# 檢查 PostgreSQL 容器狀態
podman ps | grep postgres

# 測試資料庫連線
podman exec blog-postgres psql -U bloguser -d blogdb -c "SELECT version();"

# 檢查連線字串
echo $DB_PASSWORD
```

### 3. OAuth 重新導向失敗

**問題**: Google 登入後出現錯誤

**解決方案**:
- 確認 Google Cloud Console 的「授權重新導向 URI」設定正確
- 檢查 `GOOGLE_CLIENT_ID` 和 `GOOGLE_CLIENT_SECRET` 是否正確
- 確認應用程式的 URL 與 Google 設定一致

### 4. 圖片上傳失敗

**問題**: 上傳圖片時出現錯誤

**解決方案**:
```bash
# 確認 uploads 目錄存在且有寫入權限
ls -la deployment/uploads

# 檢查容器內掛載點
podman exec blog-app ls -la /app/wwwroot/uploads

# 確認檔案大小限制
# 編輯 Program.cs 中的 FormOptions.MultipartBodyLengthLimit
```

---

## 資料庫管理

### 連線到 PostgreSQL

```bash
# 使用 podman exec
podman exec -it blog-postgres psql -U bloguser -d blogdb

# 或使用 GUI 工具
# Host: localhost
# Port: 5432
# Database: blogdb
# Username: bloguser
# Password: (從 .env 取得)
```

### 常用 SQL 查詢

```sql
-- 查看所有文章
SELECT "Id", "Title", "Status", "PublishedAt" FROM "BlogPosts" ORDER BY "PublishedAt" DESC;

-- 查看分類統計
SELECT c."Name", COUNT(p."Id") as PostCount
FROM "Categories" c
LEFT JOIN "BlogPosts" p ON c."Id" = p."CategoryId"
GROUP BY c."Id", c."Name";

-- 查看熱門標籤
SELECT "Name", "UsageCount" FROM "Tags" ORDER BY "UsageCount" DESC LIMIT 10;

-- 查看管理員操作記錄
SELECT * FROM "AdminLogs" ORDER BY "Timestamp" DESC LIMIT 20;
```

### 備份與還原

```bash
# 備份資料庫
podman exec blog-postgres pg_dump -U bloguser blogdb > backup_$(date +%Y%m%d_%H%M%S).sql

# 還原資料庫
podman exec -i blog-postgres psql -U bloguser blogdb < backup_20251031_100000.sql
```

---

## 部署到生產環境

### 步驟 1: 更新環境變數

編輯 `deployment/.env`:
```bash
# 使用強密碼
DB_PASSWORD=<strong-password>

# 生產環境 Google OAuth 憑證
GOOGLE_CLIENT_ID=<production-client-id>
GOOGLE_CLIENT_SECRET=<production-client-secret>

# 實際管理員 email
ADMIN_EMAILS=admin@yourdomain.com
```

### 步驟 2: 建構生產映像

```bash
cd deployment
podman-compose build
```

### 步驟 3: 啟動服務

```bash
podman-compose up -d
```

### 步驟 4: 設定 HTTPS (選用,建議使用 Nginx/Caddy 反向代理)

```yaml
# 在 podman-compose.yml 加入 Nginx 服務
nginx:
  image: nginx:alpine
  ports:
    - "443:443"
  volumes:
    - ./nginx.conf:/etc/nginx/nginx.conf:ro
    - ./certs:/etc/nginx/certs:ro
  depends_on:
    - blog-app
```

---

## 下一步

1. **閱讀規格文件**: [spec.md](./spec.md)
2. **了解資料模型**: [data-model.md](./data-model.md)
3. **查看 API 文件**: [contracts/README.md](./contracts/README.md)
4. **執行測試**: `dotnet test`
5. **開始開發**: 參考 `/speckit.tasks` 產生的任務清單

---

## 取得協助

- **專案文件**: `specs/001-personal-blog/`
- **問題回報**: 建立 GitHub Issue
- **憲章**: `.specify/memory/constitution.md`

---

**祝開發順利!** 🚀
