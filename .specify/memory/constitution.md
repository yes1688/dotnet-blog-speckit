<!--
Sync Impact Report
==================
Version Change: (Initial) → 1.0.0
Modified Principles: N/A (Initial creation)
Added Sections:
  - Core Principles (5 principles defined)
  - 技術規範 (Technology Standards)
  - 開發流程 (Development Workflow)
  - Governance
Templates Status:
  ✅ plan-template.md - Reviewed, compatible
  ✅ spec-template.md - Reviewed, compatible
  ✅ tasks-template.md - Reviewed, compatible
  ✅ checklist-template.md - Exists, compatible
  ✅ agent-file-template.md - Exists, compatible
Follow-up TODOs: None
-->

# .NET 部落格專案憲章

## 核心原則

### I. 正體中文優先

**規範:**
- 所有專案文件 (規格、計畫、任務清單) **必須**使用正體中文撰寫
- 程式碼註解與文件字串**應**使用正體中文
- 互動過程 (AI 對話、審查評論) **必須**使用繁體中文
- 變數名稱、函式名稱等程式識別符**應**使用英文,但可在註解中提供中文說明

**理由:**
確保團隊成員能以母語進行需求分析、設計討論與知識傳遞,降低溝通成本,提升理解精確度。

### II. 容器化優先

**規範:**
- 專案**必須**基於 Podman 容器技術建構
- 所有服務**必須**能透過 `podman compose` 命令啟動
- 開發環境、測試環境、生產環境**必須**使用相同的容器映像
- 容器配置檔**必須**納入版本控制
- 不得依賴主機特定的環境配置

**理由:**
確保環境一致性,消除「在我機器上可以執行」的問題,簡化部署流程,提升可移植性。

### III. 規格先行 (不可妥協)

**規範:**
- 任何功能開發**必須**先完成規格文件 (`spec.md`)
- 規格**必須**包含:
  - 使用者情境與驗收標準
  - 功能需求 (Functional Requirements)
  - 成功標準 (Success Criteria)
- 規格不清楚時**必須**使用 `/speckit.clarify` 進行澄清
- 實作期間發現規格不足**必須**暫停實作,先更新規格

**理由:**
避免需求理解偏差,減少返工成本,確保實作符合真實需求,提供可追溯的決策記錄。

### IV. 測試驅動開發

**規範:**
- 核心業務邏輯**必須**先寫測試
- 測試**必須**涵蓋:
  - 單元測試 (Unit Tests)
  - 整合測試 (Integration Tests) - 針對服務間互動
  - 契約測試 (Contract Tests) - 針對 API 端點
- 測試**必須**先失敗 (Red),實作後通過 (Green),最後重構 (Refactor)
- 不得提交未通過測試的程式碼

**理由:**
確保程式碼品質,防止迴歸錯誤,建立可靠的安全網,促進良好的程式設計。

### V. 簡約設計

**規範:**
- 優先選擇最簡單的可行方案
- 遵循 YAGNI 原則 (You Aren't Gonna Need It) - 不實作目前不需要的功能
- 引入新技術、框架或模式**必須**在憲章中記錄理由
- 複雜度增加**必須**在實作計畫的「Complexity Tracking」表格中說明

**理由:**
降低維護成本,加快開發速度,減少錯誤機會,保持程式碼可讀性。

## 技術規範

### 容器技術
- **容器引擎**: Podman (必須)
- **編排工具**: `podman compose` (必須)
- **映像倉庫**: 使用官方或經驗證的映像作為基底

### .NET 開發
- **目標框架**: .NET 8.0 或更高版本
- **專案結構**: 遵循清晰的分層架構 (Presentation, Business Logic, Data Access)
- **命名慣例**: 遵循 C# 官方命名指引

### 版本控制
- **版本格式**: MAJOR.MINOR.PATCH (語義化版本)
- **重大變更**: 增加 MAJOR 版本
- **新增功能**: 增加 MINOR 版本
- **修正錯誤**: 增加 PATCH 版本

## 開發流程

### 功能開發循環
1. **規格階段**: 使用 `/speckit.specify` 建立功能規格
2. **澄清階段**: 使用 `/speckit.clarify` 解決模糊需求
3. **計畫階段**: 使用 `/speckit.plan` 產生技術實作計畫
4. **任務階段**: 使用 `/speckit.tasks` 拆解可執行任務
5. **分析階段**: 使用 `/speckit.analyze` 檢查一致性 (選用)
6. **實作階段**: 使用 `/speckit.implement` 執行任務
7. **審查階段**: 程式碼審查與測試驗證
8. **整合階段**: 合併至主分支

### 需求變更處理
- **規格階段變更**: 直接更新 `spec.md`,重新澄清
- **計畫階段變更**: 評估影響,必要時回到規格階段
- **實作階段變更**: 小調整可直接修改,設計問題必須回到規格階段
- **完成後新需求**: 視為新功能,重新開始流程

### 程式碼審查
- 所有程式碼**必須**經過審查才能合併
- 審查**必須**驗證:
  - 符合規格要求
  - 通過所有測試
  - 遵循憲章原則
  - 程式碼品質與可讀性

## 治理規範

### 憲章權威
本憲章優先於所有其他開發慣例與個人偏好。如有衝突,以憲章為準。

### 修訂程序
1. 提出修訂需求,說明理由與影響
2. 使用 `/speckit.constitution` 更新憲章
3. 檢查並更新受影響的模板檔案
4. 記錄版本變更與修訂內容
5. 團隊審查與批准

### 版本控制
- **MAJOR**: 移除原則、重新定義核心規範 (向後不相容)
- **MINOR**: 新增原則、擴充指導方針
- **PATCH**: 文字修正、澄清說明

### 合規檢查
- 每個功能的計畫階段**必須**通過「Constitution Check」
- 發現違反憲章**必須**在「Complexity Tracking」表格中說明理由與替代方案
- 持續違反憲章視為需要修訂憲章或重新設計

**版本**: 1.0.0 | **批准日期**: 2025-10-31 | **最後修訂**: 2025-10-31
