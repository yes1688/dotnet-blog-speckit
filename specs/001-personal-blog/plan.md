# 實作計畫:個人部落格系統

**Branch**: `001-personal-blog` | **Date**: 2025-10-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-personal-blog/spec.md`

## Summary

建立一個基於 .NET 和 Podman 容器的個人部落格系統,提供完整的前台閱讀體驗與後台管理功能。系統使用 PostgreSQL 儲存資料,Google OAuth 2.0 進行後台認證,支援 Markdown 文章撰寫、分類標籤管理、文章搜尋等功能。採用 ASP.NET Core MVC 架構,分離前台與後台邏輯,確保安全性與可維護性。

## Technical Context

**Language/Version**: .NET 8.0 (LTS)
**Primary Dependencies**:
- ASP.NET Core 8.0 (MVC + Razor Pages)
- Entity Framework Core 8.0 (PostgreSQL provider)
- Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.AspNetCore.Authentication.Google (OAuth 2.0)
- Markdig (Markdown 解析)

**Storage**: PostgreSQL 15+ (容器化部署,使用 volume 持久化)
**Testing**: xUnit + Moq + FluentAssertions + Testcontainers (整合測試)
**Target Platform**: Linux (容器環境,Podman)
**Project Type**: Web application (前台 + 後台管理)
**Performance Goals**:
- 首頁載入 < 2 秒
- 文章詳細頁載入 < 1.5 秒
- 搜尋回應 < 2 秒
- 支援 100 並發使用者

**Constraints**:
- 必須透過 Podman Compose 啟動
- 圖片上傳限制 5MB
- Session 有效期 7 天
- 支援中文 URL slug

**Scale/Scope**:
- 個人部落格規模 (~500 篇文章)
- 單一管理員或少數管理員 (透過環境變數配置)
- 預期日訪客 < 1000 人

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 核心原則檢查

✅ **I. 正體中文優先**
- 所有文件使用正體中文撰寫
- 程式碼註解將使用正體中文
- 符合憲章要求

✅ **II. 容器化優先**
- 使用 Podman 容器技術建構
- 提供 podman-compose.yml 配置檔
- 資料庫與應用程式均容器化
- 符合憲章要求

✅ **III. 規格先行 (不可妥協)**
- 已完成 spec.md 規格文件
- 已完成 clarify 澄清流程
- 符合憲章要求

✅ **IV. 測試驅動開發**
- 計畫包含單元測試、整合測試、契約測試
- 將遵循 TDD 流程
- 符合憲章要求

✅ **V. 簡約設計**
- 採用標準 ASP.NET Core MVC 架構
- 使用 Entity Framework Core (標準 ORM)
- 避免過度設計,符合 YAGNI 原則
- 符合憲章要求

### 技術規範檢查

✅ **容器技術**
- 容器引擎: Podman ✓
- 編排工具: podman compose ✓

✅ **.NET 開發**
- 目標框架: .NET 8.0 ✓
- 專案結構: 分層架構 (Presentation, Business Logic, Data Access) ✓
- 命名慣例: 遵循 C# 官方指引 ✓

**結論**: 所有憲章檢查通過,可以進入 Phase 0 研究階段。

## Project Structure

### Documentation (this feature)

```text
specs/001-personal-blog/
├── plan.md              # 本檔案 (/speckit.plan 命令輸出)
├── spec.md              # 功能規格
├── research.md          # Phase 0 輸出 (技術研究)
├── data-model.md        # Phase 1 輸出 (資料模型)
├── quickstart.md        # Phase 1 輸出 (快速入門指南)
├── contracts/           # Phase 1 輸出 (API 規格)
│   ├── api-spec.yml     # OpenAPI 規格
│   └── README.md        # API 文件
└── checklists/          # 品質檢查清單
    └── requirements.md  # 需求檢查清單
```

### Source Code (repository root)

本專案採用 **Web application** 結構,因為包含前台展示與後台管理兩個獨立的介面區域:

```text
BlogSystem/
├── BlogSystem.Web/                    # ASP.NET Core Web 專案
│   ├── Areas/
│   │   └── Admin/                     # 後台管理區域
│   │       ├── Controllers/           # 後台控制器
│   │       ├── Views/                 # 後台視圖
│   │       └── ViewModels/            # 後台視圖模型
│   ├── Controllers/                   # 前台控制器
│   ├── Views/                         # 前台視圖 (Razor)
│   │   ├── Home/                      # 首頁視圖
│   │   ├── Post/                      # 文章視圖
│   │   ├── Category/                  # 分類視圖
│   │   ├── Tag/                       # 標籤視圖
│   │   ├── Search/                    # 搜尋視圖
│   │   └── Shared/                    # 共用視圖 (Layout, 部分視圖)
│   ├── ViewModels/                    # 前台視圖模型
│   ├── wwwroot/                       # 靜態檔案
│   │   ├── css/                       # 樣式表
│   │   ├── js/                        # JavaScript
│   │   └── uploads/                   # 上傳的圖片 (volume 掛載點)
│   ├── Program.cs                     # 應用程式進入點
│   └── appsettings.json               # 配置檔 (範本)
│
├── BlogSystem.Core/                   # 核心業務邏輯層
│   ├── Entities/                      # 實體類別
│   │   ├── BlogPost.cs
│   │   ├── Category.cs
│   │   ├── Tag.cs
│   │   ├── AdminLog.cs
│   │   └── Image.cs
│   ├── Interfaces/                    # 介面定義
│   │   ├── IRepository.cs
│   │   ├── IBlogPostService.cs
│   │   ├── ICategoryService.cs
│   │   ├── ITagService.cs
│   │   ├── IAuthService.cs
│   │   └── IImageService.cs
│   └── Services/                      # 業務邏輯服務
│       ├── BlogPostService.cs
│       ├── CategoryService.cs
│       ├── TagService.cs
│       ├── AuthService.cs
│       └── ImageService.cs
│
├── BlogSystem.Infrastructure/         # 基礎設施層
│   ├── Data/                          # 資料存取
│   │   ├── BlogDbContext.cs          # EF Core DbContext
│   │   ├── Configurations/           # EF Core 實體配置
│   │   └── Migrations/               # 資料庫遷移
│   ├── Repositories/                  # Repository 實作
│   │   ├── Repository.cs
│   │   └── BlogPostRepository.cs
│   └── Extensions/                    # 擴充方法
│       └── ServiceCollectionExtensions.cs
│
├── BlogSystem.Tests/                  # 測試專案
│   ├── Unit/                          # 單元測試
│   │   ├── Services/
│   │   └── Controllers/
│   ├── Integration/                   # 整合測試
│   │   ├── Api/
│   │   └── Database/
│   └── Contract/                      # 契約測試
│       └── Api/
│
├── deployment/                        # 部署配置
│   ├── podman-compose.yml            # Podman Compose 配置
│   ├── Dockerfile                    # 應用程式 Dockerfile
│   ├── .env.example                  # 環境變數範本
│   └── init-db.sql                   # 資料庫初始化腳本 (選用)
│
└── BlogSystem.sln                     # Solution 檔案
```

**Structure Decision**:

選擇 Web application 結構的原因:

1. **前後台分離**: 使用 ASP.NET Core Areas 功能將後台管理 (Admin) 與前台展示分離,便於權限控制與路由管理
2. **分層架構**: 遵循 Clean Architecture 原則,分為 Web (Presentation)、Core (Business Logic)、Infrastructure (Data Access) 三層
3. **可測試性**: 核心業務邏輯與資料存取分離,便於撰寫單元測試與整合測試
4. **擴展性**: 未來若需要 API 或其他前端技術,可輕鬆加入新的 Presentation 專案

## Complexity Tracking

> 本專案無違反憲章的複雜度,無需填寫此表格。
