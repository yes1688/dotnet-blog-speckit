# 專案架構說明文檔

**專案名稱**: BlogSystem (個人部落格系統)
**架構模式**: Clean Architecture (整潔架構) + ASP.NET Core MVC
**建立日期**: 2025-10-31
**目標環境**: 容器化部署 (Podman/Docker)

---

## 📋 目錄

- [專案架構說明文檔](#專案架構說明文檔)
  - [📋 目錄](#-目錄)
  - [🏗️ 專案結構](#️-專案結構)
    - [多層架構設計](#多層架構設計)
  - [🔄 與 dotnet-blog-bmad 的差異](#-與-dotnet-blog-bmad-的差異)
    - [架構對比表](#架構對比表)
  - [🐳 .NET 運行環境](#-net-運行環境)
    - [容器化架構](#容器化架構)
    - [Dockerfile 多階段建構](#dockerfile-多階段建構)
  - [🚀 快速開始](#-快速開始)
    - [方式一：使用容器運行（推薦）](#方式一使用容器運行推薦)
    - [方式二：本機開發（需安裝 .NET SDK）](#方式二本機開發需安裝-net-sdk)
  - [🔧 開發指南](#-開發指南)
    - [新增功能流程](#新增功能流程)
    - [資料庫遷移](#資料庫遷移)
  - [📁 重要檔案說明](#-重要檔案說明)
  - [🔐 Google OAuth 設定](#-google-oauth-設定)
    - [問題修復記錄](#問題修復記錄)
  - [🌐 應用程式端點](#-應用程式端點)
  - [🐛 常見問題](#-常見問題)
    - [1. 為什麼 `dotnet run` 無法執行？](#1-為什麼-dotnet-run-無法執行)
    - [2. 登入後出現 405 錯誤](#2-登入後出現-405-錯誤)
    - [3. Google OAuth redirect\_uri\_mismatch 錯誤](#3-google-oauth-redirect_uri_mismatch-錯誤)
    - [4. 容器無法連接到 oauth2.googleapis.com](#4-容器無法連接到-oauth2googleapiscom)
  - [📚 參考資源](#-參考資源)

---

## 🏗️ 專案結構

```
dotnet-blog-speckit/
│
├── BlogSystem.Core/                    # 核心業務邏輯層（Domain Layer）
│   ├── Entities/                       # 領域實體
│   │   ├── BlogPost.cs                 # 部落格文章實體
│   │   ├── Category.cs                 # 分類實體
│   │   ├── Tag.cs                      # 標籤實體
│   │   └── AdminLog.cs                 # 管理員操作日誌
│   ├── Interfaces/                     # 介面定義（依賴倒置原則）
│   │   ├── IRepository.cs              # 通用儲存庫介面
│   │   ├── IBlogPostService.cs         # 文章服務介面
│   │   ├── IAuthService.cs             # 認證服務介面
│   │   └── ...
│   ├── Services/                       # 業務邏輯服務
│   │   ├── BlogPostService.cs          # 文章業務邏輯
│   │   ├── AuthService.cs              # 認證業務邏輯
│   │   └── ...
│   └── Enums/                          # 列舉定義
│       └── PostStatus.cs               # 文章狀態
│
├── BlogSystem.Infrastructure/          # 基礎設施層（Data Access Layer）
│   ├── Data/
│   │   ├── BlogDbContext.cs            # Entity Framework DbContext
│   │   ├── DbInitializer.cs            # 資料庫初始化
│   │   └── Configurations/             # Entity 配置
│   ├── Repositories/                   # Repository 實作
│   │   ├── Repository.cs               # 通用儲存庫實作
│   │   └── BlogPostRepository.cs       # 文章儲存庫
│   └── Migrations/                     # EF Core 資料庫遷移檔案
│
├── BlogSystem.Web/                     # 展示層（Presentation Layer）
│   ├── Program.cs                      # 應用程式進入點
│   ├── appsettings.json                # 應用程式配置
│   ├── Areas/
│   │   └── Admin/                      # 後台管理區域
│   │       ├── Controllers/            # 後台控制器
│   │       │   ├── AuthController.cs   # 登入/登出控制器
│   │       │   ├── PostsController.cs  # 文章管理控制器
│   │       │   └── ...
│   │       └── Views/                  # 後台視圖
│   │           ├── _ViewImports.cshtml # Tag Helpers 配置 ⭐
│   │           ├── Auth/
│   │           │   └── Login.cshtml    # 登入頁面
│   │           └── ...
│   ├── Controllers/                    # 前台控制器
│   │   ├── HomeController.cs           # 首頁控制器
│   │   ├── PostController.cs           # 文章顯示控制器
│   │   └── ...
│   ├── Views/                          # 前台視圖
│   │   ├── _ViewImports.cshtml         # Tag Helpers 配置
│   │   ├── Home/
│   │   ├── Post/
│   │   └── Shared/
│   ├── ViewModels/                     # 視圖模型
│   ├── Middleware/                     # 中介軟體
│   └── wwwroot/                        # 靜態檔案（CSS, JS, 圖片）
│
├── BlogSystem.Tests/                   # 測試專案
│   ├── Unit/                           # 單元測試
│   └── Integration/                    # 整合測試
│
├── deployment/                         # 容器部署配置
│   ├── Dockerfile                      # 容器建構檔
│   ├── podman-compose.yml              # 服務編排配置
│   ├── .env                            # 環境變數（不提交到 Git）
│   └── .env.example                    # 環境變數範例
│
├── specs/                              # 規格文檔（spec-kit）
│   └── 001-personal-blog/
│       ├── quickstart.md               # 快速開始指南
│       └── ...
│
├── BlogSystem.sln                      # Visual Studio Solution 檔
├── README.md                           # 專案說明
└── PROJECT_ARCHITECTURE.md             # 本檔案（架構說明）
```

### 多層架構設計

本專案採用 **Clean Architecture** 原則，具有以下優點：

1. **關注點分離** (Separation of Concerns)
   - 業務邏輯與資料存取分離
   - UI 與業務邏輯分離

2. **依賴倒置** (Dependency Inversion)
   - Core 層不依賴任何其他層
   - Infrastructure 和 Web 層依賴 Core 層的介面

3. **易於測試**
   - 可以 mock Interface 進行單元測試
   - 不需要實際資料庫即可測試業務邏輯

4. **易於擴展**
   - 可以輕鬆替換 ORM 框架（如從 EF Core 換成 Dapper）
   - 可以輕鬆添加新的 UI 層（如 Web API）

---

## 🔄 與 dotnet-blog-bmad 的差異

### 架構對比表

| 特性 | dotnet-blog-bmad | dotnet-blog-speckit (本專案) |
|------|------------------|------------------------------|
| **架構模式** | 單體式 Razor Pages | Clean Architecture + MVC |
| **專案數量** | 1 個專案 | 4 個專案（Core, Infrastructure, Web, Tests） |
| **UI 技術** | Razor Pages | MVC + Razor Views + Areas |
| **資料存取** | DbContext 直接在專案內 | Repository Pattern |
| **業務邏輯** | 在 Services 資料夾 | 獨立的 Core 專案 |
| **測試性** | 較難測試 | 易於單元測試 |
| **適用規模** | 小型專案、快速原型 | 中大型專案、長期維護 |
| **學習曲線** | 較平緩 | 較陡峭 |
| **目錄結構** | 平面結構 | 分層結構 |
| **DI 容器** | 簡單配置 | 多層次依賴注入 |

**dotnet-blog-bmad 結構**：
```
dotnet-blog-bmad/
├── dotnet-blog-bmad.csproj  ← 所有程式碼在一個專案
├── Program.cs
├── Data/                    ← DbContext 和 Entities
├── Pages/                   ← Razor Pages (UI + 邏輯)
└── Services/                ← 業務邏輯
```

**dotnet-blog-speckit 結構**（本專案）：
```
dotnet-blog-speckit/
├── BlogSystem.Core/         ← 核心業務邏輯（獨立專案）
├── BlogSystem.Infrastructure/ ← 資料存取（獨立專案）
├── BlogSystem.Web/          ← UI 層（MVC）
└── BlogSystem.Tests/        ← 測試專案
```

---

## 🐳 .NET 運行環境

### 容器化架構

本專案**完全基於容器運行**，不需要在主機系統安裝 .NET SDK。

```
┌─────────────────────────────────────────────────┐
│              主機系統 (Linux)                    │
│  ❌ 不需要安裝 .NET SDK                          │
│                                                  │
│  ┌────────────────────────────────────────┐    │
│  │     Podman 容器 (blog-app)              │    │
│  │                                          │    │
│  │  ✅ .NET 8.0 Runtime (內建於映像)        │    │
│  │  ✅ BlogSystem.Web.dll (編譯好的應用)    │    │
│  │                                          │    │
│  │  ENTRYPOINT: dotnet BlogSystem.Web.dll  │    │
│  │  PORT: 8080                              │    │
│  └────────────────────────────────────────┘    │
│                                                  │
│  ┌────────────────────────────────────────┐    │
│  │  Podman 容器 (blog-postgres)            │    │
│  │  PostgreSQL 15                           │    │
│  │  PORT: 5432                              │    │
│  └────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
```

### Dockerfile 多階段建構

```dockerfile
# ===== Stage 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# 包含完整的 .NET SDK，用於編譯程式碼
WORKDIR /src
COPY BlogSystem.sln ./
COPY BlogSystem.Web/ ./BlogSystem.Web/
# ... 複製其他專案
RUN dotnet restore
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish

# ===== Stage 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# 只包含 ASP.NET Core Runtime，映像更小（~200MB vs ~700MB）
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlogSystem.Web.dll"]
```

**為什麼使用多階段建構？**
- ✅ 最終映像不包含 SDK，體積更小
- ✅ 更安全（不暴露建構工具）
- ✅ 更快的部署和啟動

---

## 🚀 快速開始

### 方式一：使用容器運行（推薦）

**此方式不需要安裝 .NET SDK**

```bash
# 1. 進入部署目錄
cd deployment

# 2. 設定環境變數
cp .env.example .env
nano .env  # 編輯設定

# 3. 啟動所有服務
podman-compose up -d --build

# 4. 查看日誌
podman-compose logs -f blog-app

# 5. 訪問應用
# 前台: http://localhost:8080
# 後台: http://localhost:8080/Admin/Auth/Login

# 6. 停止服務
podman-compose down
```

### 方式二：本機開發（需安裝 .NET SDK）

**僅在需要本機調試時使用**

```bash
# 1. 安裝 .NET SDK 8.0
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# 2. 新增到 PATH
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

# 3. 驗證安裝
dotnet --version  # 應顯示 8.0.x

# 4. 還原套件
cd BlogSystem.Web
dotnet restore

# 5. 執行資料庫遷移（需先啟動 PostgreSQL）
dotnet ef database update --project ../BlogSystem.Infrastructure

# 6. 運行應用
dotnet run --urls "http://localhost:5000"
```

---

## 🔧 開發指南

### 新增功能流程

假設要新增「留言功能」：

```bash
# 1. 在 Core 層新增實體
BlogSystem.Core/Entities/Comment.cs

# 2. 在 Core 層新增介面
BlogSystem.Core/Interfaces/ICommentService.cs

# 3. 在 Core 層實作服務
BlogSystem.Core/Services/CommentService.cs

# 4. 在 Infrastructure 層配置 DbContext
BlogSystem.Infrastructure/Data/Configurations/CommentConfiguration.cs

# 5. 在 Infrastructure 層新增 Repository（如需要）
BlogSystem.Infrastructure/Repositories/CommentRepository.cs

# 6. 建立資料庫遷移
cd BlogSystem.Web
dotnet ef migrations add AddComment --project ../BlogSystem.Infrastructure

# 7. 在 Web 層新增控制器
BlogSystem.Web/Controllers/CommentController.cs

# 8. 在 Web 層新增視圖
BlogSystem.Web/Views/Comment/

# 9. 註冊服務（Program.cs）
builder.Services.AddScoped<ICommentService, CommentService>();

# 10. 重新建構容器
cd deployment
podman-compose up -d --build
```

### 資料庫遷移

```bash
# 容器環境（不需要 .NET SDK）
podman exec blog-app dotnet ef database update --project BlogSystem.Infrastructure

# 本機環境（需要 .NET SDK）
cd BlogSystem.Web
dotnet ef migrations add MigrationName --project ../BlogSystem.Infrastructure
dotnet ef database update --project ../BlogSystem.Infrastructure
dotnet ef migrations list --project ../BlogSystem.Infrastructure
```

---

## 📁 重要檔案說明

| 檔案路徑 | 用途 | 重要性 |
|----------|------|--------|
| `deployment/.env` | 環境變數配置（資料庫密碼、OAuth 金鑰） | ⭐⭐⭐⭐⭐ |
| `deployment/podman-compose.yml` | 服務編排配置（容器、網路、卷） | ⭐⭐⭐⭐⭐ |
| `deployment/Dockerfile` | 容器建構腳本 | ⭐⭐⭐⭐ |
| `BlogSystem.Web/Program.cs` | 應用程式進入點、服務註冊 | ⭐⭐⭐⭐⭐ |
| `BlogSystem.Web/appsettings.json` | 應用程式配置（連線字串、日誌） | ⭐⭐⭐⭐ |
| `BlogSystem.Web/Areas/Admin/Views/_ViewImports.cshtml` | Tag Helpers 配置（修復登入問題） | ⭐⭐⭐⭐ |
| `BlogSystem.Infrastructure/Data/BlogDbContext.cs` | Entity Framework 資料庫上下文 | ⭐⭐⭐⭐⭐ |
| `BlogSystem.Core/Entities/` | 領域實體定義 | ⭐⭐⭐⭐⭐ |

---

## 🔐 Google OAuth 設定

### 問題修復記錄

**問題 1: HTTP 405 錯誤（已修復）**

**原因**: Admin Area 缺少 `_ViewImports.cshtml`，導致 Tag Helpers 未啟用

**解決方案**: 建立 `BlogSystem.Web/Areas/Admin/Views/_ViewImports.cshtml`
```csharp
@using BlogSystem.Web
@using BlogSystem.Web.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

**問題 2: redirect_uri_mismatch（待修復）**

**原因**: Google Cloud Console 配置的回調 URL 與應用程式不匹配

**解決方案**:

1. 前往 [Google Cloud Console](https://console.cloud.google.com/)
2. 選擇專案 → API 和服務 → 憑證
3. 編輯 OAuth 2.0 客戶端 ID
4. 在「已授權的重定向 URI」新增：
   ```
   http://localhost:8080/signin-google
   ```
   或（如果使用自訂回調路徑）：
   ```
   http://localhost:8080/Admin/Auth/GoogleCallback
   ```

**問題 3: 網路連接錯誤（待排查）**

**現象**: 容器無法連接到 `oauth2.googleapis.com`

**可能原因**:
- 容器網路配置問題
- 防火牆阻擋
- 需要 Proxy 設定（中國大陸）

**排查步驟**:
```bash
# 1. 檢查主機網路
curl -I https://oauth2.googleapis.com

# 2. 檢查容器網路配置
podman inspect blog-app | grep -A 20 "Networks"

# 3. 檢查 DNS
podman exec blog-app cat /etc/resolv.conf
```

---

## 🌐 應用程式端點

| 端點 | 用途 | 權限 |
|------|------|------|
| `http://localhost:8080/` | 首頁（文章列表） | 公開 |
| `http://localhost:8080/Post/{year}/{month}/{slug}` | 文章詳細頁 | 公開 |
| `http://localhost:8080/Category/{id}` | 分類文章列表 | 公開 |
| `http://localhost:8080/Tag/{id}` | 標籤文章列表 | 公開 |
| `http://localhost:8080/Search?q=關鍵字` | 全文搜尋 | 公開 |
| `http://localhost:8080/Admin/Auth/Login` | 管理員登入 | 公開 |
| `http://localhost:8080/Admin/Dashboard` | 後台儀表板 | 需認證 |
| `http://localhost:8080/Admin/Posts` | 文章管理 | 需認證 |
| `http://localhost:8080/Admin/Categories` | 分類管理 | 需認證 |
| `http://localhost:8080/health` | 健康檢查 | 公開 |

---

## 🐛 常見問題

### 1. 為什麼 `dotnet run` 無法執行？

**原因**: 主機系統未安裝 .NET SDK

**解決方案**:
- 推薦：使用容器運行（`podman-compose up`）
- 或：安裝 .NET SDK 8.0（參考「快速開始 - 方式二」）

### 2. 登入後出現 405 錯誤

**原因**: Tag Helpers 未啟用

**解決方案**: 已修復，確保存在以下檔案：
- `BlogSystem.Web/Views/_ViewImports.cshtml`
- `BlogSystem.Web/Areas/Admin/Views/_ViewImports.cshtml`

### 3. Google OAuth redirect_uri_mismatch 錯誤

**解決方案**: 在 Google Cloud Console 配置正確的回調 URL（參考上方「Google OAuth 設定」）

### 4. 容器無法連接到 oauth2.googleapis.com

**檢查項目**:
```bash
# 1. 容器是否啟動
podman ps

# 2. 查看日誌
podman logs blog-app

# 3. 測試網路連通性
curl -I https://oauth2.googleapis.com
```

---

## 📚 參考資源

- [ASP.NET Core 官方文檔](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Google OAuth 2.0 設定](https://developers.google.com/identity/protocols/oauth2)
- [Podman 官方文檔](https://docs.podman.io/)

---

**文檔維護**: 請在架構或配置有重大變更時更新本檔案
**最後更新**: 2025-10-31
**維護者**: 開發團隊

---

## 📌 補充說明：.NET 環境配置（2025-10-31 更新）

### 系統中的 .NET 安裝狀態

經確認，本系統**已安裝** .NET SDK 8.0.415，位於：
```
/home/davidliou/.dotnet/
```

**已安裝元件**：
- .NET SDK 8.0.415
- ASP.NET Core Runtime 8.0.21
- .NET Core Runtime 8.0.21

### PATH 環境變數設定

若遇到 `dotnet: command not found` 錯誤，請確認環境變數：

**自動設定**（推薦）：
```bash
# 已加入到 ~/.bashrc，新終端機會自動載入
# 無需額外操作
```

**手動設定**（臨時）：
```bash
# 在當前終端機中執行
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
```

**驗證安裝**：
```bash
dotnet --version          # 顯示 8.0.415
dotnet --list-sdks        # 列出 SDK
dotnet --list-runtimes    # 列出 Runtime
```

### 本機開發 vs 容器開發

| 方式 | 優點 | 缺點 | 適用情境 |
|------|------|------|----------|
| **容器開發** | 環境一致、易於部署 | 需要重建容器 | 生產環境、團隊協作 |
| **本機開發** | 快速修改測試、即時熱重載 | 環境差異風險 | 快速開發、調試 |

**建議**：
- 開發階段：使用本機 `dotnet run`（熱重載、快速迭代）
- 測試/部署：使用容器 `podman-compose up`（環境一致）

---

**文檔更新日誌**：
- 2025-10-31 15:00 - 確認 .NET 已安裝，更新環境變數設定說明
- 2025-10-31 14:30 - 修復 Tag Helpers 問題，創建 _ViewImports.cshtml
- 2025-10-31 14:00 - 初始文檔建立


---

## 🎯 為什麼選擇容器開發？

### 核心原則：環境隔離，零污染

本專案**強烈建議使用容器開發**，原因如下：

#### 1. 零環境污染 ✅

```
主機系統
├── 只需要 Podman
└── 專案原始碼

容器內
├── .NET Runtime
├── PostgreSQL
├── 所有依賴套件
└── 應用程式

刪除容器 = 完全清除，不留痕跡
```

#### 2. 版本隔離 ✅

```bash
# 可以同時運行不同版本
專案 A: .NET 6 + PostgreSQL 13 (port 5001, 5433)
專案 B: .NET 8 + PostgreSQL 15 (port 5000, 5432)
專案 C: .NET 7 + MySQL 8 (port 5002, 3306)

# 互不干擾！
```

#### 3. 一致性保證 ✅

| 環境 | 本機開發 | 容器開發 |
|------|---------|---------|
| 開發者 A 的機器 | Ubuntu 22.04 + .NET 8.0.100 | 容器映像 abc123 |
| 開發者 B 的機器 | macOS + .NET 8.0.200 | 容器映像 abc123 |
| CI/CD 伺服器 | CentOS 7 + .NET 8.0.300 | 容器映像 abc123 |
| 生產環境 | Ubuntu 24.04 + ? | 容器映像 abc123 |

**容器：完全相同！** ✅

#### 4. 簡化部署流程 ✅

```bash
# 本機測試
podman-compose up -d

# 推送到生產環境（完全相同）
podman push myregistry.com/blog-app:latest
```

#### 5. 易於清理 ✅

```bash
# 完全移除專案環境
podman-compose down -v  # 停止容器、移除卷
podman rmi blog-app     # 移除映像

# 主機系統：完全乾淨！
```

### 容器開發工作流程

```bash
# 1. 修改程式碼
nano BlogSystem.Web/Controllers/HomeController.cs

# 2. 重建並啟動
cd deployment
podman-compose up -d --build

# 3. 測試
curl http://localhost:8080

# 4. 查看日誌
podman logs -f blog-app

# 5. 結束後清理
podman-compose down
```

### 何時使用本機開發？

**僅在以下情況考慮**：

1. **快速原型開發**：需要頻繁修改測試
2. **學習 .NET 基礎**：不需要完整環境
3. **除錯特定問題**：需要使用 IDE 調試器

**但仍建議**：開發完成後在容器中最終測試

### 主機上的 .NET SDK

**保留即可，不影響容器開發**

```bash
# .NET SDK 用途：
✅ 執行 CLI 工具（dotnet ef, dotnet tool）
✅ 查看文件（dotnet --help）
✅ 學習實驗（dotnet new console）

# 不用於：
❌ 運行本專案（使用容器代替）
❌ 編譯本專案（容器內編譯）
```

---

**最佳實踐總結**：
- ✅ 開發：容器
- ✅ 測試：容器  
- ✅ 部署：容器
- ⚠️ 快速實驗：本機（可選）

