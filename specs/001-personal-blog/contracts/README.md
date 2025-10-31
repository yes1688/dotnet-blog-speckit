# API 契約文件:個人部落格系統

**版本**: 1.0.0
**日期**: 2025-10-31
**基礎 URL**: `http://localhost:8080`

## 概述

本專案採用 ASP.NET Core MVC 架構,主要提供 HTML 頁面渲染,部分功能提供 AJAX API 端點。

## 端點分類

### 前台端點 (Public)

#### 1. 首頁 - 文章列表

**GET** `/`

**功能**: 顯示已發布文章列表 (分頁)

**查詢參數**:
- `page` (int, optional): 頁碼,預設 1
- `pageSize` (int, optional): 每頁筆數,預設 10

**回應**: HTML 頁面

---

#### 2. 文章詳細頁

**GET** `/{year}/{month}/{slug}`

**功能**: 顯示單篇文章完整內容

**路徑參數**:
- `year` (int): 發布年份
- `month` (int): 發布月份
- `slug` (string): 文章 slug

**回應**: HTML 頁面 或 404 錯誤頁

---

#### 3. 分類文章列表

**GET** `/category/{slug}`

**功能**: 顯示特定分類下的文章

**路徑參數**:
- `slug` (string): 分類 slug

**查詢參數**:
- `page` (int, optional): 頁碼,預設 1

**回應**: HTML 頁面

---

#### 4. 標籤文章列表

**GET** `/tag/{slug}`

**功能**: 顯示包含特定標籤的文章

**路徑參數**:
- `slug` (string): 標籤 slug

**查詢參數**:
- `page` (int, optional): 頁碼,預設 1

**回應**: HTML 頁面

---

#### 5. 搜尋文章

**GET** `/search`

**功能**: 依關鍵字搜尋文章

**查詢參數**:
- `q` (string, required): 搜尋關鍵字
- `page` (int, optional): 頁碼,預設 1

**回應**: HTML 頁面

---

### 後台端點 (Admin Area)

**基礎路徑**: `/Admin`
**認證要求**: 需透過 Google OAuth 2.0 登入且 email 在授權清單中

#### 6. 後台登入

**GET** `/Admin/Auth/Login`

**功能**: 顯示登入頁面

**回應**: HTML 頁面

---

**GET** `/Admin/Auth/SignInWithGoogle`

**功能**: 重新導向至 Google OAuth 授權頁面

**回應**: 302 Redirect

---

**GET** `/Admin/Auth/Callback`

**功能**: Google OAuth 回呼端點

**回應**: 302 Redirect 至後台首頁或錯誤頁

---

#### 7. 後台首頁

**GET** `/Admin`

**功能**: 顯示後台儀表板

**回應**: HTML 頁面

**需求**: 已認證且為授權管理員

---

#### 8. 文章管理

**GET** `/Admin/Posts`

**功能**: 文章列表 (含草稿)

**查詢參數**:
- `status` (string, optional): 篩選狀態 (draft/published/all)
- `page` (int, optional): 頁碼

**回應**: HTML 頁面

---

**GET** `/Admin/Posts/Create`

**功能**: 顯示新增文章表單

**回應**: HTML 頁面

---

**POST** `/Admin/Posts/Create`

**功能**: 建立新文章

**表單欄位**:
```json
{
  "title": "文章標題",
  "content": "Markdown 內容",
  "summary": "摘要 (選填)",
  "categoryId": "分類 GUID",
  "tagIds": ["標籤1 GUID", "標籤2 GUID"],
  "status": "draft 或 published"
}
```

**回應**: 302 Redirect 至文章編輯頁

---

**GET** `/Admin/Posts/Edit/{id}`

**功能**: 顯示編輯文章表單

**路徑參數**:
- `id` (Guid): 文章 ID

**回應**: HTML 頁面

---

**POST** `/Admin/Posts/Edit/{id}`

**功能**: 更新文章

**表單欄位**: 同 Create

**回應**: 302 Redirect 至文章列表

---

**POST** `/Admin/Posts/Delete/{id}`

**功能**: 刪除文章

**路徑參數**:
- `id` (Guid): 文章 ID

**回應**: 302 Redirect 至文章列表

---

#### 9. 分類管理

**GET** `/Admin/Categories`

**功能**: 分類列表

**回應**: HTML 頁面

---

**POST** `/Admin/Categories/Create`

**功能**: 建立新分類

**表單欄位**:
```json
{
  "name": "分類名稱",
  "slug": "url-slug",
  "description": "描述 (選填)",
  "displayOrder": 0
}
```

**回應**: 302 Redirect 至分類列表

---

**POST** `/Admin/Categories/Edit/{id}`

**功能**: 更新分類

**回應**: 302 Redirect

---

**POST** `/Admin/Categories/Delete/{id}`

**功能**: 刪除分類

**回應**: 302 Redirect

---

#### 10. 圖片上傳 (AJAX API)

**POST** `/Admin/Upload/Image`

**功能**: 上傳圖片 (用於 Markdown 編輯器)

**Content-Type**: `multipart/form-data`

**表單欄位**:
- `file`: 圖片檔案 (最大 5MB)

**回應 (JSON)**:
```json
{
  "success": true,
  "url": "/uploads/2025/10/abc123.jpg",
  "message": "上傳成功"
}
```

**錯誤回應**:
```json
{
  "success": false,
  "error": "檔案格式不支援"
}
```

---

## 錯誤處理

### 標準錯誤頁面

| 狀態碼 | 說明 | 顯示頁面 |
|--------|------|---------|
| 404 | 找不到頁面/文章 | 友善 404 頁面,含返回首頁連結 |
| 403 | 無權限訪問後台 | 「您沒有權限訪問此系統」錯誤頁 |
| 500 | 伺服器錯誤 | 通用錯誤頁面 |

### AJAX API 錯誤格式

```json
{
  "success": false,
  "error": "錯誤訊息",
  "details": "詳細資訊 (僅開發環境)"
}
```

---

## 認證與授權

### Google OAuth 2.0 流程

1. 使用者點擊「使用 Google 登入」→ `/Admin/Auth/SignInWithGoogle`
2. 重新導向至 Google 授權頁面
3. 使用者授權後回到 `/Admin/Auth/Callback`
4. 系統檢查 email 是否在 `ADMIN_EMAILS` 環境變數中
5. 若授權,建立 Cookie 並導向後台首頁
6. 若未授權,顯示錯誤訊息

### Cookie 配置

- **名稱**: `.AspNetCore.Cookies`
- **有效期**: 7 天
- **HttpOnly**: true
- **Secure**: true (生產環境)
- **SameSite**: Lax

---

## 效能考量

### 快取策略

- **靜態資源**: Browser cache 30 天
- **文章頁面**: 可考慮 Output Cache (未實作在 MVP)
- **資料庫查詢**: 使用 EF Core AsNoTracking 優化唯讀查詢

### 分頁限制

- 每頁最多 10 筆文章
- 搜尋結果最多顯示 100 頁 (1000 筆)

---

## 安全性

### CSRF 保護

- 所有 POST 請求需包含 `__RequestVerificationToken`
- ASP.NET Core 自動驗證

### XSS 防護

- Razor 自動 HTML 編碼輸出
- Markdig 啟用 HTML 消毒 (sanitization)

### 檔案上傳安全

- 驗證檔案類型 (.jpg, .png, .gif, .webp)
- 限制檔案大小 (5MB)
- 使用 GUID 檔名避免路徑遍歷攻擊

---

## 測試端點

### 健康檢查

**GET** `/health`

**回應 (JSON)**:
```json
{
  "status": "healthy",
  "database": "connected",
  "timestamp": "2025-10-31T10:00:00Z"
}
```
