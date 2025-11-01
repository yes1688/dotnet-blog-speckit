# Clean Architecture 詳細說明

## 業務邏輯 vs UI 邏輯的區別

### 核心概念圖

```
┌─────────────────────────────────────────────────────────┐
│                    使用者界面層                          │
│                  (Presentation Layer)                   │
│                                                          │
│  職責：展示資料、處理使用者輸入                          │
│  ├── Controllers (控制器)                                │
│  │   └── UI 邏輯：參數驗證、頁面導航、錯誤顯示          │
│  ├── Views (視圖)                                        │
│  │   └── UI 邏輯：畫面呈現、表單驗證、使用者互動        │
│  └── ViewModels (視圖模型)                               │
│      └── UI 邏輯：資料格式化、分頁計算                   │
│                                                          │
│  ❌ 不包含業務邏輯                                       │
│  ❌ 不直接操作資料庫                                     │
└─────────────────────────────────────────────────────────┘
                            ↓ 呼叫
┌─────────────────────────────────────────────────────────┐
│                    業務邏輯層                            │
│                  (Business Logic Layer)                 │
│                                                          │
│  職責：核心業務規則、商業邏輯                            │
│  ├── Services (服務)                                     │
│  │   └── 業務邏輯：文章發布規則、權限檢查、業務流程      │
│  ├── Entities (實體)                                     │
│  │   └── 業務邏輯：資料驗證、狀態轉換                   │
│  └── Interfaces (介面)                                   │
│      └── 定義業務操作契約                                │
│                                                          │
│  ✅ 包含核心業務規則                                     │
│  ❌ 不知道 UI 如何顯示                                   │
│  ❌ 不知道資料庫細節                                     │
└─────────────────────────────────────────────────────────┘
                            ↓ 呼叫
┌─────────────────────────────────────────────────────────┐
│                    資料存取層                            │
│                  (Data Access Layer)                    │
│                                                          │
│  職責：資料持久化、資料庫操作                            │
│  ├── Repositories (儲存庫)                               │
│  ├── DbContext (資料庫上下文)                            │
│  └── Migrations (資料庫遷移)                             │
│                                                          │
│  ✅ 處理資料庫操作                                       │
│  ❌ 不包含業務邏輯                                       │
└─────────────────────────────────────────────────────────┘
```

---

## 🔍 詳細對比

### 1. 業務邏輯（Business Logic）

**定義**：與業務領域相關的規則和流程，不管用什麼 UI 或資料庫都不變

**特徵**：
- ✅ 反映真實世界的業務規則
- ✅ 可以脫離 UI 和資料庫獨立測試
- ✅ 跨平台通用（Web、Mobile、Desktop 都適用）
- ✅ 穩定不常變

**範例**：
```csharp
// 位置：BlogSystem.Core/Services/BlogPostService.cs

public class BlogPostService : IBlogPostService
{
    // ✅ 這是業務邏輯
    public async Task<BlogPost> PublishPostAsync(int postId)
    {
        var post = await _repository.GetByIdAsync(postId);

        // 業務規則 1：只有草稿可以發布
        if (post.Status != PostStatus.Draft)
        {
            throw new InvalidOperationException("只有草稿狀態的文章可以發布");
        }

        // 業務規則 2：文章必須有標題和內容
        if (string.IsNullOrWhiteSpace(post.Title) ||
            string.IsNullOrWhiteSpace(post.Content))
        {
            throw new InvalidOperationException("文章標題和內容不能為空");
        }

        // 業務規則 3：自動生成 Slug
        if (string.IsNullOrWhiteSpace(post.Slug))
        {
            post.Slug = GenerateSlug(post.Title);
        }

        // 業務規則 4：設定發布時間
        post.Status = PostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(post);
        return post;
    }

    // ✅ 這也是業務邏輯
    private string GenerateSlug(string title)
    {
        // Slug 生成規則：移除特殊字元、轉小寫、空格變連字號
        return title.ToLower()
                    .Replace(" ", "-")
                    .Replace("，", "")
                    .Replace("。", "");
    }
}
```

**為什麼這是業務邏輯？**
- 反映真實世界規則：「只有草稿可以發布」是編輯流程的規則
- 不依賴 UI：不管是網頁、手機 App 還是 API，規則都一樣
- 可獨立測試：不需要啟動網頁就能測試這個邏輯

---

### 2. UI 邏輯（Presentation Logic）

**定義**：與使用者界面相關的邏輯，處理顯示和互動

**特徵**：
- ✅ 只關心如何顯示資料
- ✅ 處理使用者輸入和導航
- ✅ 依賴特定 UI 框架（MVC、Razor Pages 等）
- ⚠️ 經常變動（UI 改版）

**範例**：
```csharp
// 位置：BlogSystem.Web/Areas/Admin/Controllers/PostsController.cs

public class PostsController : Controller
{
    private readonly IBlogPostService _blogPostService;

    // ❌ 這不是業務邏輯，這是 UI 邏輯
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        // UI 邏輯 1：參數驗證（防止惡意輸入）
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        // 呼叫業務邏輯層（不是自己實作）
        var posts = await _blogPostService.GetPagedPostsAsync(page, pageSize);

        // UI 邏輯 2：準備視圖資料
        var viewModel = new PostListViewModel
        {
            Posts = posts,
            CurrentPage = page,
            PageSize = pageSize,
            // UI 邏輯 3：計算分頁資訊（純顯示用）
            TotalPages = (int)Math.Ceiling(posts.TotalCount / (double)pageSize),
            HasPreviousPage = page > 1,
            HasNextPage = page < (posts.TotalCount / pageSize)
        };

        // UI 邏輯 4：決定顯示哪個視圖
        return View(viewModel);
    }

    // ❌ 這也不是業務邏輯，這是 UI 流程控制
    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            // 呼叫業務邏輯（委派給 Service）
            await _blogPostService.PublishPostAsync(id);

            // UI 邏輯：成功後的處理
            TempData["SuccessMessage"] = "文章已成功發布！";
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            // UI 邏輯：錯誤顯示
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction("Edit", new { id });
        }
    }
}
```

**為什麼這是 UI 邏輯？**
- 關心如何顯示：分頁計算、錯誤訊息顯示
- 依賴 UI 框架：使用 ASP.NET MVC 的 Controller、View
- 處理導航：RedirectToAction、返回不同視圖
- 不包含業務規則：「文章發布規則」委派給 Service

---

### 3. 視圖邏輯（View Logic）

**定義**：在 View（視圖）中的邏輯，純粹處理畫面呈現

**範例**：
```cshtml
<!-- 位置：BlogSystem.Web/Areas/Admin/Views/Posts/Index.cshtml -->

@model PostListViewModel

<h1>文章管理</h1>

<!-- ❌ 這是 UI 邏輯：條件顯示 -->
@if (Model.Posts.Any())
{
    <table class="table">
        <thead>
            <tr>
                <th>標題</th>
                <th>狀態</th>
                <th>發布時間</th>
                <th>操作</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var post in Model.Posts)
            {
                <tr>
                    <td>@post.Title</td>

                    <!-- ❌ UI 邏輯：狀態顯示 -->
                    <td>
                        @switch (post.Status)
                        {
                            case PostStatus.Draft:
                                <span class="badge bg-secondary">草稿</span>
                                break;
                            case PostStatus.Published:
                                <span class="badge bg-success">已發布</span>
                                break;
                        }
                    </td>

                    <!-- ❌ UI 邏輯：日期格式化 -->
                    <td>
                        @(post.PublishedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-")
                    </td>

                    <!-- ❌ UI 邏輯：按鈕顯示條件 -->
                    <td>
                        @if (post.Status == PostStatus.Draft)
                        {
                            <form asp-action="Publish" method="post">
                                <input type="hidden" name="id" value="@post.Id" />
                                <button type="submit" class="btn btn-sm btn-primary">
                                    發布
                                </button>
                            </form>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <!-- ❌ UI 邏輯：分頁導航 -->
    <nav>
        <ul class="pagination">
            @if (Model.HasPreviousPage)
            {
                <li class="page-item">
                    <a class="page-link" asp-route-page="@(Model.CurrentPage - 1)">
                        上一頁
                    </a>
                </li>
            }

            <li class="page-item active">
                <span class="page-link">
                    第 @Model.CurrentPage 頁 / 共 @Model.TotalPages 頁
                </span>
            </li>

            @if (Model.HasNextPage)
            {
                <li class="page-item">
                    <a class="page-link" asp-route-page="@(Model.CurrentPage + 1)">
                        下一頁
                    </a>
                </li>
            }
        </ul>
    </nav>
}
else
{
    <!-- ❌ UI 邏輯：空狀態顯示 -->
    <div class="alert alert-info">
        目前沒有文章，<a asp-action="Create">立即建立</a>第一篇文章吧！
    </div>
}
```

**為什麼這是 UI 邏輯？**
- 純粹處理顯示：CSS 類別、HTML 結構
- 格式化資料：日期格式、狀態標籤
- 條件渲染：顯示/隱藏按鈕、空狀態提示

---

## 🎯 關鍵區別總結

| 特性 | 業務邏輯 | UI 邏輯 |
|------|---------|---------|
| **位置** | Core 層 (Services) | Web 層 (Controllers/Views) |
| **關注點** | 業務規則、商業流程 | 使用者互動、資料顯示 |
| **範例問題** | "什麼時候可以發布文章？" | "如何顯示發布按鈕？" |
| **變動頻率** | 低（業務規則穩定） | 高（UI 改版常見） |
| **可測試性** | 容易（不需 UI） | 較難（需要 UI 框架） |
| **可重用性** | 高（跨平台） | 低（特定 UI） |
| **依賴** | 只依賴領域實體 | 依賴 UI 框架 |

---

## 💡 本專案的實際例子

### 案例 1：發布文章

#### ✅ 業務邏輯（在 Core 層）
```csharp
// BlogSystem.Core/Services/BlogPostService.cs

// 業務規則：發布文章的條件
public async Task PublishPostAsync(int postId)
{
    var post = await _repository.GetByIdAsync(postId);

    // 規則 1：必須是草稿
    if (post.Status != PostStatus.Draft)
        throw new InvalidOperationException("只有草稿可以發布");

    // 規則 2：必須有內容
    if (string.IsNullOrWhiteSpace(post.Content))
        throw new InvalidOperationException("文章內容不能為空");

    // 規則 3：設定發布時間
    post.Status = PostStatus.Published;
    post.PublishedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(post);
}
```

#### ❌ UI 邏輯（在 Web 層）
```csharp
// BlogSystem.Web/Areas/Admin/Controllers/PostsController.cs

// UI 流程：處理發布請求
[HttpPost]
public async Task<IActionResult> Publish(int id)
{
    try
    {
        // 委派給業務邏輯層
        await _blogPostService.PublishPostAsync(id);

        // UI：顯示成功訊息
        TempData["Success"] = "發布成功！";
        return RedirectToAction("Index");
    }
    catch (InvalidOperationException ex)
    {
        // UI：顯示錯誤訊息
        TempData["Error"] = ex.Message;
        return RedirectToAction("Edit", new { id });
    }
}
```

---

### 案例 2：搜尋文章

#### ✅ 業務邏輯（在 Core 層）
```csharp
// BlogSystem.Core/Services/SearchService.cs

// 業務邏輯：搜尋規則
public async Task<List<BlogPost>> SearchAsync(string keyword)
{
    // 規則 1：關鍵字清理
    keyword = keyword?.Trim();
    if (string.IsNullOrWhiteSpace(keyword))
        return new List<BlogPost>();

    // 規則 2：最小搜尋長度
    if (keyword.Length < 2)
        throw new ArgumentException("搜尋關鍵字至少需要 2 個字元");

    // 規則 3：只搜尋已發布的文章
    var query = _context.BlogPosts
        .Where(p => p.Status == PostStatus.Published)
        .Where(p =>
            p.Title.Contains(keyword) ||
            p.Content.Contains(keyword) ||
            p.Summary.Contains(keyword));

    // 規則 4：按相關性排序（標題優先）
    var results = await query
        .OrderByDescending(p => p.Title.Contains(keyword))
        .ThenByDescending(p => p.PublishedAt)
        .ToListAsync();

    return results;
}
```

#### ❌ UI 邏輯（在 Web 層）
```csharp
// BlogSystem.Web/Controllers/SearchController.cs

// UI 邏輯：搜尋頁面處理
public async Task<IActionResult> Index(string q, int page = 1)
{
    // UI：參數驗證
    if (page < 1) page = 1;

    // UI：處理空查詢
    if (string.IsNullOrWhiteSpace(q))
    {
        return View(new SearchResultViewModel
        {
            Keyword = "",
            Message = "請輸入搜尋關鍵字"
        });
    }

    try
    {
        // 呼叫業務邏輯
        var results = await _searchService.SearchAsync(q);

        // UI：分頁處理
        const int pageSize = 10;
        var pagedResults = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // UI：準備顯示資料
        var viewModel = new SearchResultViewModel
        {
            Keyword = q,
            Results = pagedResults,
            TotalCount = results.Count,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(results.Count / (double)pageSize)
        };

        return View(viewModel);
    }
    catch (ArgumentException ex)
    {
        // UI：錯誤顯示
        return View(new SearchResultViewModel
        {
            Keyword = q,
            ErrorMessage = ex.Message
        });
    }
}
```

---

## 🚫 常見錯誤示範

### ❌ 錯誤：在 Controller 中寫業務邏輯

```csharp
// ❌ 不好的做法
public class PostsController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);

        // ❌ 業務邏輯寫在 Controller 中
        if (post.Status != PostStatus.Draft)
        {
            ModelState.AddModelError("", "只有草稿可以發布");
            return View(post);
        }

        // ❌ 業務邏輯寫在 Controller 中
        post.Status = PostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
```

**問題**：
1. 無法重用（其他地方要發布文章，需要複製貼上）
2. 難以測試（需要啟動整個 Web 應用）
3. 如果開發手機 App，需要重新寫一次相同邏輯

---

### ✅ 正確：業務邏輯在 Service

```csharp
// ✅ 好的做法：Controller 只處理 UI
public class PostsController : Controller
{
    private readonly IBlogPostService _blogPostService;

    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            // 委派給業務邏輯層
            await _blogPostService.PublishPostAsync(id);

            // Controller 只處理 UI 回應
            TempData["Success"] = "發布成功";
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            // Controller 只處理錯誤顯示
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction("Edit", new { id });
        }
    }
}

// ✅ 業務邏輯在 Service
public class BlogPostService : IBlogPostService
{
    public async Task PublishPostAsync(int postId)
    {
        var post = await _repository.GetByIdAsync(postId);

        // 業務規則集中管理
        if (post.Status != PostStatus.Draft)
            throw new InvalidOperationException("只有草稿可以發布");

        post.Status = PostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(post);
    }
}
```

**優點**：
1. ✅ 可重用（Web、API、Console App 都能用）
2. ✅ 易於測試（不需要 Controller）
3. ✅ 易於維護（業務規則集中管理）

---

## 🎓 判斷標準

### 如何判斷是業務邏輯還是 UI 邏輯？

問自己這些問題：

#### 1. **"如果換成手機 App，這個邏輯還需要嗎？"**
- ✅ 需要 → 業務邏輯
- ❌ 不需要 → UI 邏輯

範例：
```csharp
// "只有草稿可以發布" → 手機 App 也需要 → 業務邏輯 ✅
// "顯示綠色的成功訊息" → 手機 App 不需要 → UI 邏輯 ❌
```

#### 2. **"這個邏輯能脫離 UI 框架測試嗎？"**
- ✅ 能 → 業務邏輯
- ❌ 不能 → UI 邏輯

範例：
```csharp
// 測試發布規則 → 不需要啟動網頁 → 業務邏輯 ✅
// 測試按鈕點擊 → 需要瀏覽器 → UI 邏輯 ❌
```

#### 3. **"這個邏輯反映真實世界的規則嗎？"**
- ✅ 是 → 業務邏輯
- ❌ 否 → UI 邏輯

範例：
```csharp
// "文章必須有標題" → 真實世界規則 → 業務邏輯 ✅
// "標題最多顯示 20 字" → 畫面限制 → UI 邏輯 ❌
```

---

## 📚 延伸閱讀

### 相關概念

1. **Domain-Driven Design (DDD)**
   - 業務邏輯進一步細分為 Domain Services、Domain Events

2. **CQRS (Command Query Responsibility Segregation)**
   - 命令（寫入）和查詢（讀取）分離

3. **Dependency Inversion Principle**
   - 高層模組（業務邏輯）不依賴低層模組（UI、資料庫）

---

## 🎯 本專案的分層總結

```
BlogSystem.Web/              ← UI 邏輯
├── Controllers/             (UI 流程控制)
├── Views/                   (畫面呈現)
└── ViewModels/              (顯示資料結構)

BlogSystem.Core/             ← 業務邏輯
├── Services/                (業務流程)
├── Entities/                (領域實體)
└── Interfaces/              (業務契約)

BlogSystem.Infrastructure/   ← 資料存取
├── Repositories/            (資料操作)
└── Data/                    (資料庫配置)
```

**記住**：
- UI 層呼叫業務層，不實作業務邏輯
- 業務層不知道 UI 如何顯示
- 業務層不知道資料庫細節

---

**最後更新**: 2025-10-31
**作者**: 開發團隊
