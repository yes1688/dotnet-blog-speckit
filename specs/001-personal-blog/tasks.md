# 任務清單:個人部落格系統

**輸入**: 設計文件來自 `/specs/001-personal-blog/`
**前置需求**: plan.md (必需), spec.md (必需,包含使用者情境), research.md, data-model.md, contracts/

**測試**: 本專案遵循憲章的測試驅動開發原則,因此包含完整的測試任務。

**組織方式**: 任務依使用者情境分組,使每個情境都能獨立實作與測試。

## 格式: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行 (不同檔案,無依賴)
- **[Story]**: 此任務屬於哪個使用者情境 (如 US1, US2, US3)
- 描述中包含確切的檔案路徑

## 路徑慣例

- **Web 專案**: `BlogSystem.Web/`
- **核心層**: `BlogSystem.Core/`
- **基礎設施層**: `BlogSystem.Infrastructure/`
- **測試專案**: `BlogSystem.Tests/`
- **容器部署**: `deployment/`

## Phase 1: Setup (專案初始化)

**目的**: 專案初始化與基本結構建立

- [X] T001 建立 .NET Solution 檔案 BlogSystem.sln
- [X] T002 [P] 建立 BlogSystem.Web 專案 (ASP.NET Core MVC)
- [X] T003 [P] 建立 BlogSystem.Core 專案 (類別庫)
- [X] T004 [P] 建立 BlogSystem.Infrastructure 專案 (類別庫)
- [X] T005 [P] 建立 BlogSystem.Tests 專案 (xUnit)
- [X] T006 加入專案參考關係 (Web→Core→Infrastructure, Tests→All)
- [X] T007 [P] 安裝 NuGet 套件: EF Core, Npgsql, Google Auth 到各專案
- [X] T008 [P] 建立 .gitignore 檔案 (排除 bin/, obj/, appsettings.Development.json)
- [X] T009 [P] 建立 deployment/Dockerfile 容器映像檔
- [X] T010 [P] 建立 deployment/podman-compose.yml 容器編排檔
- [X] T011 [P] 建立 deployment/.env.example 環境變數範本

---

## Phase 2: Foundational (阻塞性前置需求)

**目的**: 所有使用者情境都依賴的核心基礎設施

**⚠️ 重要**: 此階段必須完成後,才能開始任何使用者情境的開發

- [X] T012 建立 BlogSystem.Core/Entities/BlogPost.cs 實體類別
- [X] T013 [P] 建立 BlogSystem.Core/Entities/Category.cs 實體類別
- [X] T014 [P] 建立 BlogSystem.Core/Entities/Tag.cs 實體類別
- [X] T015 [P] 建立 BlogSystem.Core/Entities/AdminLog.cs 實體類別
- [X] T016 [P] 建立 PostStatus enum 在 BlogSystem.Core/Enums/PostStatus.cs
- [X] T017 建立 BlogSystem.Infrastructure/Data/BlogDbContext.cs DbContext
- [X] T018 [P] 建立 BlogSystem.Infrastructure/Data/Configurations/BlogPostConfiguration.cs EF 配置
- [X] T019 [P] 建立 BlogSystem.Infrastructure/Data/Configurations/CategoryConfiguration.cs EF 配置
- [X] T020 [P] 建立 BlogSystem.Infrastructure/Data/Configurations/TagConfiguration.cs EF 配置
- [X] T021 配置連線字串在 BlogSystem.Web/appsettings.json
- [X] T022 註冊 DbContext 在 BlogSystem.Web/Program.cs
- [X] T023 執行 EF Core 初始遷移: dotnet ef migrations add InitialCreate
- [X] T024 建立 BlogSystem.Core/Interfaces/IRepository.cs 通用 Repository 介面
- [X] T025 [P] 建立 BlogSystem.Infrastructure/Repositories/Repository.cs 實作
- [X] T026 註冊 Repository 服務在 BlogSystem.Web/Program.cs

**Checkpoint**: 基礎設施就緒 - 使用者情境實作現在可以平行開始

---

## Phase 3: User Story 1 - 閱讀部落格文章 (Priority: P1) 🎯 MVP

**目標**: 訪客能瀏覽並閱讀已發布的部落格文章列表與詳細內容

**Independent Test**: 手動新增測試文章到資料庫,然後從瀏覽器訪問首頁與文章詳細頁驗證顯示正確

### 測試 for User Story 1 (TDD)

> **重要**: 先寫這些測試並確認失敗,再進行實作

- [X] T027 [P] [US1] 建立整合測試 BlogSystem.Tests/Integration/Controllers/HomeControllerTests.cs
- [X] T028 [P] [US1] 建立整合測試 BlogSystem.Tests/Integration/Controllers/PostControllerTests.cs
- [X] T029 [P] [US1] 建立單元測試 BlogSystem.Tests/Unit/Services/BlogPostServiceTests.cs

### 實作 for User Story 1

- [X] T030 [P] [US1] 建立 BlogSystem.Core/Interfaces/IBlogPostService.cs 介面
- [X] T031 [US1] 建立 BlogSystem.Core/Services/BlogPostService.cs 服務 (包含分頁邏輯)
- [X] T032 [P] [US1] 建立 BlogSystem.Web/ViewModels/PostListViewModel.cs 視圖模型
- [X] T033 [P] [US1] 建立 BlogSystem.Web/ViewModels/PostDetailViewModel.cs 視圖模型
- [X] T034 [P] [US1] 建立 BlogSystem.Web/ViewModels/PagedResult.cs 分頁輔助類別
- [X] T035 [US1] 建立 BlogSystem.Web/Controllers/HomeController.cs 首頁控制器
- [X] T036 [US1] 建立 BlogSystem.Web/Controllers/PostController.cs 文章控制器
- [X] T037 [P] [US1] 建立 BlogSystem.Web/Views/Home/Index.cshtml 首頁視圖
- [X] T038 [P] [US1] 建立 BlogSystem.Web/Views/Post/Details.cshtml 文章詳細頁視圖
- [X] T039 [P] [US1] 建立 BlogSystem.Web/Views/Shared/_Layout.cshtml 共用版面配置
- [X] T040 [P] [US1] 建立 BlogSystem.Web/Views/Shared/_PostCard.cshtml 文章卡片部分視圖
- [X] T041 [P] [US1] 建立 BlogSystem.Web/Views/Shared/_Pagination.cshtml 分頁導航部分視圖
- [X] T042 [US1] 設定 Markdig Markdown 解析器在 BlogSystem.Web/Program.cs
- [X] T043 [US1] 配置路由支援中文 URL slug: {year}/{month}/{slug} 在 Program.cs
- [X] T044 [P] [US1] 加入基本 CSS 樣式 BlogSystem.Web/wwwroot/css/site.css
- [X] T045 [US1] 註冊 BlogPostService 在 BlogSystem.Web/Program.cs

**Checkpoint**: 此時 User Story 1 應完全可用且可獨立測試 (MVP 完成!)

---

## Phase 4: User Story 2 - 管理員登入後台 (Priority: P2)

**目標**: 管理員透過 Google OAuth 2.0 安全登入後台

**Independent Test**: 設定 ADMIN_EMAILS 環境變數,使用 Google 帳號登入,驗證授權邏輯

### 測試 for User Story 2 (TDD)

- [X] T046 [P] [US2] 建立整合測試 BlogSystem.Tests/Integration/Areas/Admin/AuthControllerTests.cs
- [X] T047 [P] [US2] 建立單元測試 BlogSystem.Tests/Unit/Services/AuthServiceTests.cs

### 實作 for User Story 2

- [X] T048 [P] [US2] 建立 BlogSystem.Core/Interfaces/IAuthService.cs 介面
- [X] T049 [US2] 建立 BlogSystem.Core/Services/AuthService.cs 服務 (email 授權檢查)
- [X] T050 [US2] 安裝 Microsoft.AspNetCore.Authentication.Google NuGet 套件
- [X] T051 [US2] 配置 Google OAuth 2.0 在 BlogSystem.Web/Program.cs
- [X] T052 [US2] 配置 Cookie Authentication 在 Program.cs (有效期 7 天)
- [X] T053 [US2] 建立自訂 Authorization Policy "AdminOnly" 在 Program.cs
- [X] T054 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Controllers/AuthController.cs
- [X] T055 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Views/Auth/Login.cshtml 登入頁
- [X] T056 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Views/Auth/AccessDenied.cshtml 無權限頁
- [X] T057 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Controllers/DashboardController.cs 後台首頁
- [X] T058 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Views/Dashboard/Index.cshtml 儀表板
- [X] T059 [P] [US2] 建立 BlogSystem.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml 後台版面
- [X] T060 [US2] 實作 AdminLog 記錄功能在 AuthController (登入/登出)
- [X] T061 [US2] 註冊 AuthService 在 BlogSystem.Web/Program.cs

**Checkpoint**: User Stories 1 和 2 現在都可獨立運作

---

## Phase 5: User Story 3 - 管理文章內容 (Priority: P3)

**目標**: 管理員在後台建立、編輯、刪除、發布文章

**Independent Test**: 登入後台,執行 CRUD 操作,在前台驗證變更

### 測試 for User Story 3 (TDD)

- [X] T062 [P] [US3] 建立整合測試 BlogSystem.Tests/Integration/Areas/Admin/PostsControllerTests.cs
- [X] T063 [P] [US3] 建立單元測試 BlogSystem.Tests/Unit/Services/ImageServiceTests.cs

### 實作 for User Story 3

- [X] T064 [P] [US3] 建立 BlogSystem.Core/Interfaces/IImageService.cs 介面
- [X] T065 [US3] 建立 BlogSystem.Core/Services/ImageService.cs 服務 (圖片上傳處理)
- [X] T066 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/ViewModels/PostCreateViewModel.cs
- [X] T067 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/ViewModels/PostEditViewModel.cs
- [X] T068 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/ViewModels/PostListViewModel.cs
- [X] T069 [US3] 建立 BlogSystem.Web/Areas/Admin/Controllers/PostsController.cs
- [X] T070 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/Views/Posts/Index.cshtml 文章列表
- [X] T071 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/Views/Posts/Create.cshtml 新增表單
- [X] T072 [P] [US3] 建立 BlogSystem.Web/Areas/Admin/Views/Posts/Edit.cshtml 編輯表單
- [X] T073 [US3] 建立 BlogSystem.Web/Areas/Admin/Controllers/UploadController.cs (AJAX 圖片上傳)
- [X] T074 [US3] 實作 Slug 產生邏輯在 BlogPostService (支援中文,衝突處理)
- [X] T075 [US3] 實作草稿/發布狀態切換邏輯在 BlogPostService
- [X] T076 [P] [US3] 整合 Markdown 編輯器 (EasyMDE) 到 Create/Edit 視圖
- [X] T077 [US3] 配置檔案上傳限制 (5MB) 在 Program.cs FormOptions
- [X] T078 [US3] 建立 wwwroot/uploads 目錄並配置 volume 掛載在 podman-compose.yml
- [X] T079 [US3] 註冊 ImageService 在 BlogSystem.Web/Program.cs

**Checkpoint**: User Stories 1, 2, 3 現在都可獨立運作

---

## Phase 6: User Story 4 - 分類與標籤管理 (Priority: P4)

**目標**: 管理員管理分類與標籤,訪客透過分類/標籤瀏覽文章

**Independent Test**: 建立分類與標籤,歸類文章,點擊前台分類/標籤連結驗證過濾功能

### 測試 for User Story 4 (TDD)

- [ ] T080 [P] [US4] 建立整合測試 BlogSystem.Tests/Integration/Controllers/CategoryControllerTests.cs
- [ ] T081 [P] [US4] 建立整合測試 BlogSystem.Tests/Integration/Controllers/TagControllerTests.cs
- [ ] T082 [P] [US4] 建立單元測試 BlogSystem.Tests/Unit/Services/CategoryServiceTests.cs
- [ ] T083 [P] [US4] 建立單元測試 BlogSystem.Tests/Unit/Services/TagServiceTests.cs

### 實作 for User Story 4

- [ ] T084 [P] [US4] 建立 BlogSystem.Core/Interfaces/ICategoryService.cs 介面
- [ ] T085 [P] [US4] 建立 BlogSystem.Core/Interfaces/ITagService.cs 介面
- [ ] T086 [P] [US4] 建立 BlogSystem.Core/Services/CategoryService.cs 服務
- [ ] T087 [P] [US4] 建立 BlogSystem.Core/Services/TagService.cs 服務
- [ ] T088 [US4] 建立 BlogSystem.Web/Areas/Admin/Controllers/CategoriesController.cs
- [ ] T089 [P] [US4] 建立 BlogSystem.Web/Areas/Admin/Views/Categories/Index.cshtml
- [ ] T090 [P] [US4] 建立 BlogSystem.Web/Areas/Admin/Views/Categories/_CreateModal.cshtml
- [ ] T091 [P] [US4] 建立 BlogSystem.Web/Controllers/CategoryController.cs 前台分類控制器
- [ ] T092 [P] [US4] 建立 BlogSystem.Web/Controllers/TagController.cs 前台標籤控制器
- [ ] T093 [P] [US4] 建立 BlogSystem.Web/Views/Category/Index.cshtml 分類文章列表
- [ ] T094 [P] [US4] 建立 BlogSystem.Web/Views/Tag/Index.cshtml 標籤文章列表
- [ ] T095 [US4] 更新 BlogSystem.Web/Areas/Admin/Views/Posts/Create.cshtml 加入分類標籤選擇
- [ ] T096 [US4] 更新 BlogSystem.Web/Areas/Admin/Views/Posts/Edit.cshtml 加入分類標籤編輯
- [ ] T097 [US4] 更新 BlogSystem.Web/Views/Shared/_PostCard.cshtml 顯示分類標籤
- [ ] T098 [US4] 註冊 Category/Tag Services 在 BlogSystem.Web/Program.cs

**Checkpoint**: 所有前 4 個使用者情境均可獨立運作

---

## Phase 7: User Story 5 - 搜尋文章 (Priority: P5)

**目標**: 訪客透過關鍵字搜尋文章標題與內容

**Independent Test**: 建立多篇文章,輸入關鍵字搜尋,驗證結果正確

### 測試 for User Story 5 (TDD)

- [ ] T099 [P] [US5] 建立整合測試 BlogSystem.Tests/Integration/Controllers/SearchControllerTests.cs
- [ ] T100 [P] [US5] 建立單元測試 BlogSystem.Tests/Unit/Services/SearchServiceTests.cs

### 實作 for User Story 5

- [ ] T101 [P] [US5] 建立 BlogSystem.Core/Interfaces/ISearchService.cs 介面
- [ ] T102 [US5] 建立 BlogSystem.Core/Services/SearchService.cs 服務 (使用 EF.Functions.ILike)
- [ ] T103 [P] [US5] 建立 BlogSystem.Web/ViewModels/SearchResultViewModel.cs
- [ ] T104 [US5] 建立 BlogSystem.Web/Controllers/SearchController.cs
- [ ] T105 [P] [US5] 建立 BlogSystem.Web/Views/Search/Index.cshtml 搜尋結果頁
- [ ] T106 [US5] 加入搜尋框到 BlogSystem.Web/Views/Shared/_Layout.cshtml
- [ ] T107 [US5] (選用) 建立 PostgreSQL GIN 索引優化搜尋效能 (透過遷移)
- [ ] T108 [US5] 註冊 SearchService 在 BlogSystem.Web/Program.cs

**Checkpoint**: 所有使用者情境均已完成且可獨立測試

---

## Phase 8: Polish & Cross-Cutting Concerns

**目的**: 影響多個使用者情境的改進與整合

- [ ] T109 [P] 建立 BlogSystem.Infrastructure/Data/DbInitializer.cs 種子資料
- [ ] T110 [P] 建立 deployment/init-db.sql PostgreSQL 初始化腳本
- [ ] T111 [P] 加入健康檢查端點 /health 在 BlogSystem.Web/Program.cs
- [ ] T112 [P] 建立 BlogSystem.Web/Views/Shared/Error.cshtml 錯誤頁面
- [ ] T113 [P] 建立 BlogSystem.Web/Views/Shared/NotFound.cshtml 404 頁面
- [ ] T114 [US2] 實作全域錯誤處理中介軟體在 Program.cs
- [ ] T115 [P] 加入 Serilog 結構化日誌記錄
- [ ] T116 [P] 加入 ResponseCompression 中介軟體優化效能
- [ ] T117 [P] 設定 Static Files Caching (30 天)
- [ ] T118 [P] 加入 Anti-Forgery Token 驗證到所有 POST 表單
- [ ] T119 執行完整的整合測試套件驗證所有功能
- [ ] T120 執行 quickstart.md 步驟驗證部署流程
- [ ] T121 [P] 撰寫 README.md 專案說明文件
- [ ] T122 [P] 更新 CLAUDE.md Agent 上下文檔案

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無依賴 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - **阻塞所有使用者情境**
- **User Stories (Phase 3-7)**: 全部依賴 Foundational 完成
  - 使用者情境可平行進行 (如有足夠人力)
  - 或依優先順序循序執行 (P1 → P2 → P3 → P4 → P5)
- **Polish (Phase 8)**: 依賴所有欲交付的使用者情境完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始 - 無其他情境依賴
- **User Story 2 (P2)**: Foundational 完成後可開始 - 無其他情境依賴 (可與 US1 平行)
- **User Story 3 (P3)**: 依賴 US2 (需要登入機制)
- **User Story 4 (P4)**: 依賴 US3 (編輯文章時需要分類標籤功能)
- **User Story 5 (P5)**: 依賴 US1 (需要文章列表基礎功能),可與 US3/US4 平行

### Within Each User Story

- 測試 **必須** 先寫並失敗,然後才實作
- Models 在 services 之前
- Services 在 controllers 之前
- Controllers 在 views 之前
- 核心實作完成後才整合其他情境

### Parallel Opportunities

- Setup 階段所有標記 [P] 的任務可平行執行
- Foundational 階段所有標記 [P] 的任務可平行執行
- Foundational 完成後,US1 與 US2 可平行開發
- US5 可與 US3/US4 平行開發
- 每個使用者情境內標記 [P] 的任務可平行執行

---

## Parallel Example: User Story 1

```bash
# 同時啟動 User Story 1 的所有測試任務:
Task: "建立整合測試 BlogSystem.Tests/Integration/Controllers/HomeControllerTests.cs"
Task: "建立整合測試 BlogSystem.Tests/Integration/Controllers/PostControllerTests.cs"
Task: "建立單元測試 BlogSystem.Tests/Unit/Services/BlogPostServiceTests.cs"

# 同時建立所有 ViewModels:
Task: "建立 BlogSystem.Web/ViewModels/PostListViewModel.cs 視圖模型"
Task: "建立 BlogSystem.Web/ViewModels/PostDetailViewModel.cs 視圖模型"
Task: "建立 BlogSystem.Web/ViewModels/PagedResult.cs 分頁輔助類別"

# 同時建立所有 Views:
Task: "建立 BlogSystem.Web/Views/Home/Index.cshtml 首頁視圖"
Task: "建立 BlogSystem.Web/Views/Post/Details.cshtml 文章詳細頁視圖"
Task: "建立 BlogSystem.Web/Views/Shared/_Layout.cshtml 共用版面配置"
```

---

## Implementation Strategy

### MVP First (僅 User Story 1)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational (**關鍵 - 阻塞所有情境**)
3. 完成 Phase 3: User Story 1
4. **停止並驗證**: 獨立測試 User Story 1
5. 可選:立即部署/展示

### Incremental Delivery (建議)

1. 完成 Setup + Foundational → 基礎就緒
2. 新增 User Story 1 → 獨立測試 → 部署/展示 (MVP!)
3. 新增 User Story 2 → 獨立測試 → 部署/展示
4. 新增 User Story 3 → 獨立測試 → 部署/展示
5. 新增 User Story 4 → 獨立測試 → 部署/展示
6. 新增 User Story 5 → 獨立測試 → 部署/展示
7. 每個情境增加價值而不破壞先前情境

### Parallel Team Strategy

若有多位開發者:

1. 團隊一起完成 Setup + Foundational
2. Foundational 完成後:
   - 開發者 A: User Story 1
   - 開發者 B: User Story 2
   - 開發者 C: User Story 5 (與 US1 依賴較少)
3. 情境完成後獨立整合

---

## Notes

- [P] 任務 = 不同檔案,無依賴
- [Story] 標籤將任務對應到特定使用者情境以便追蹤
- 每個使用者情境應可獨立完成並測試
- TDD: 驗證測試先失敗再實作
- 每個任務或邏輯群組後提交 Git
- 在任何檢查點停止以獨立驗證情境
- 避免:模糊任務、相同檔案衝突、破壞獨立性的跨情境依賴
