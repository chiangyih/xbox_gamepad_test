# 1. 文件管制資訊（Document Control）
## 1.1 文件名稱
Microsoft Xbox Wireless Controller 輸入驅動模組 SSD（Software System Design）— DevReady（Optimized）

## 1.2 文件版本
- 版本：v1.2 (DevReady-Optimized)
- 日期：2026-02-03
- 狀態：可開工版本（已納入核心邏輯與 WinForms 渲染效能優化）

## 1.3 適用範圍
適用於 **Visual Studio 2026 / C# / .NET 10 / WinForms / Windows x64**  
控制器：**Microsoft Xbox Wireless Controller（Bluetooth）**（含 Series X|S 版本）

---

# 2. 文件目的與背景
## 2.1 文件目的
本 SSD 文件定義：
1. WGI（Windows.Gaming.Input）驅動層與裝置事件機制（Added/Removed）
2. 控制器狀態模型（含電池資訊）與正規化規格（Radial Dead Zone + Rescaling）
3. WinForms 60Hz 的渲染與更新策略（雙重緩衝 + UI 解耦 Render Loop）
4. 依賴注入（DI）與可測試性（Mock Driver）
5. 需求追蹤（RTM）與測試案例（TC）骨架

---

# 3. 系統環境、技術選型與限制
## 3.1 環境

| 項目 | 規格 |
|---|---|
| OS | Windows 10 / 11 x64 |
| IDE | Visual Studio 2026 |
| Language | C# |
| Framework | .NET 10 |
| UI | WinForms (GDI+) |
| Controller | Xbox Wireless Controller |
| Connection | Bluetooth |
| Primary API | Windows.Gaming.Input (WGI) |

## 3.2 技術選型（Driver API）
- **Primary：Windows.Gaming.Input（WGI）**（v1.2 必須）
- Fallback（選配）：XInput（v1.3 之後再納入）

## 3.3 系統限制（v1.2）
- 預設單一 Active Controller（但可偵測多支，需依策略選擇 Active）
- 不包含震動回饋（Rumble）
- Xbox / Share 按鍵採 Best-effort（可能被 OS 攔截或不可讀，需環境測試/設定配合）

---

# 4. 系統架構設計（Optimized）
## 4.1 架構總覽（事件 + 邏輯更新 + UI 解耦）

```
WGI GamepadAdded/Removed Events
            │
            ▼
[Driver Layer]  (select active gamepad, read reading + battery)
            │  UpdateModel()
            ▼
[State Manager]  (60Hz Logic Tick, normalization, thread-safe CurrentState)
            │
            ├── (NO direct 60Hz BeginInvoke)
            │
            ▼
[WinForms UI Render Loop]  (Timer 16ms, pull CurrentState, draw with DoubleBuffer)
```

## 4.2 模組責任
### 4.2.1 Driver Layer（WGI）
- 訂閱 `Gamepad.GamepadAdded` / `Gamepad.GamepadRemoved`
- 維護可用 Gamepad 清單
- 依「Active Controller 選擇策略」決定目前使用的裝置
- 提供 `ReadState()`（含 Battery）

### 4.2.2 State Manager（Logic Loop）
- 維持 60Hz 邏輯更新（讀取 Driver → 套用正規化 → 更新 CurrentState）
- Thread-safe：`CurrentState` 可被 UI Thread 安全讀取（建議 immutable snapshot 或 lock）
- 不直接推送 UI（避免 BeginInvoke 堆積）

### 4.2.3 WinForms UI Layer（Render Loop）
- 使用 `System.Windows.Forms.Timer`（16ms）拉取 `CurrentState` 進行 UI 更新與重繪
- 繪圖區（Stick XY）必須啟用 Double Buffering（見 11.1）
- 若 UI 繪製 >16ms，自然掉幀（Drop frame），不堆積事件

---

# 5. 功能需求規格（Functional Requirements）
## 5.1 FR-01 控制器連線管理（事件驅動為主）
- 主要機制：訂閱 `GamepadAdded` / `GamepadRemoved`
- Removed 事件觸發：狀態重置、連線狀態轉為 Disconnected
- Added 事件觸發：依 Active Controller 策略選定裝置並轉為 Connected

## 5.2 FR-02 正面按鍵（ABXY）
- 即時顯示 A/B/X/Y 是否按下（Boolean）

## 5.3 FR-03 方向鍵（D-Pad）
- Up/Down/Left/Right 狀態顯示（Boolean）

## 5.4 FR-04 類比搖桿（LS/RS）
- Stick 範圍：-1.0 ~ +1.0
- 需套用 **Radial Dead Zone + Rescaling**（見 8.1）
- 支援 LSB / RSB

## 5.5 FR-05 系統鍵（View/Menu/Xbox/Share）
- View / Menu：必須支援
- Xbox / Share：Best-effort  
  - **Share** 可能被 Windows Game Bar/OS 快捷鍵攔截，需在文件中註記「環境依賴」並於整合測試驗證。

## 5.6 FR-06 肩鍵與扳機（LB/RB/LT/RT）
- LB/RB：Boolean
- LT/RT：0.0 ~ 1.0
- Trigger 需套用 **微小 Dead Zone（Anti-jitter）**（見 8.2）

## 5.7 FR-07 更新頻率（Logic 60Hz）
- Logic Loop 目標 60Hz（16ms tick）
- UI Render Loop 目標 60Hz，但允許掉幀（不堆積）

## 5.8 FR-08 斷線偵測備援（Fallback）
- 主要斷線偵測：`GamepadRemoved`
- 備援：若 `GetCurrentReading()` 出現特定裝置遺失例外（或讀取異常），可立即判定斷線
- 仍保留「N 次失敗」作為最終 fallback，但 **N 建議縮小**（預設 N=3）

## 5.9 FR-09 Active Controller 選擇策略（單一控制器定義）
WGI 可能同時存在多支控制器，系統需明確定義 Active 選擇規則：
- 預設策略（v1.2）：`Active = Gamepads[0]`（第一支被列舉到的）
- 建議策略（v1.3）：**Last Input Wins**（最後發生按鍵/軸值變化者成為 Active）

---

# 6. 非功能需求（Non-Functional Requirements）
| 編號 | 項目 | 規格 |
|---|---|---|
| NFR-01 | 端到端延遲 | 平均 < 20ms |
| NFR-02 | 穩定性 | 連續運行 ≥ 8 小時 |
| NFR-03 | CPU 使用率（分解） | **Logic Thread < 1%**；**UI Thread 依渲染複雜度**（目標 < 5%~10%） |
| NFR-04 | 可維護性 | 分層 + 介面化 + DI |
| NFR-05 | UI 響應性 | 拖動視窗/操作 UI 不可明顯卡頓（避免 60Hz BeginInvoke 堆積） |
| NFR-06 | 渲染品質 | 60Hz 下 XY 繪圖區不得嚴重閃爍（必做 DoubleBuffer） |
| NFR-07 | 例外容忍 | Driver 例外不得導致崩潰，需降級（Disconnected） |

---

# 7. 資料結構設計（Data Model）
## 7.1 XboxControllerState（使用 SIMD 友善型別）
建議使用 `System.Numerics.Vector2` 表示 Stick，提升可讀性與效能（.NET 10 SIMD）。

```csharp
using System.Numerics;

public enum BatteryStatus
{
    Unknown = 0,
    Charging,
    Discharging,
    Idle,
    NotPresent
}

public sealed class XboxControllerState
{
    public bool IsConnected { get; set; }

    // Buttons
    public bool A { get; set; }
    public bool B { get; set; }
    public bool X { get; set; }
    public bool Y { get; set; }

    public bool DPadUp { get; set; }
    public bool DPadDown { get; set; }
    public bool DPadLeft { get; set; }
    public bool DPadRight { get; set; }

    public bool LB { get; set; }
    public bool RB { get; set; }

    public bool ViewButton { get; set; }
    public bool MenuButton { get; set; }
    public bool XboxButton { get; set; }   // Best-effort
    public bool ShareButton { get; set; }  // Best-effort

    public bool LeftStickButton { get; set; }
    public bool RightStickButton { get; set; }

    // Axes
    public Vector2 LeftStick { get; set; }   // X/Y in [-1..1], normalized
    public Vector2 RightStick { get; set; }  // X/Y in [-1..1], normalized
    public float LT { get; set; }            // [0..1], anti-jitter applied
    public float RT { get; set; }            // [0..1], anti-jitter applied

    // Battery (Bluetooth important)
    public BatteryStatus BatteryStatus { get; set; }     // Charging/Discharging/...
    public double RemainingLevel { get; set; }           // 0.0~1.0 (if supported), else -1
}
```

---

# 8. 正規化規格（Normalization）— Critical Logic
## 8.1 Stick Dead Zone：Radial + Rescaling（取代 Axial Dead Zone）
### 8.1.1 問題（Axial）
原本規格 `if abs(value) < DeadZone => value = 0` 為軸向死區，中心形成十字，畫圓會卡頓。

### 8.1.2 改用徑向死區（Radial Dead Zone）
- 設定 `DeadZone = 0.10`（可配置）
- 計算向量長度：
  - `magnitude = sqrt(x*x + y*y)`
- 若 `magnitude < DeadZone`：
  - `x = 0; y = 0`

### 8.1.3 Rescaling（避免 Jump）
若超過死區，需將剩餘區間映射回 0..1，避免數值從 0 直接跳到 0.10。

- `dir = (x, y) / magnitude`
- `scaled = (magnitude - DeadZone) / (1 - DeadZone)`
- `output = dir * scaled`

> 輸出必須保持線性、連續。

## 8.2 Trigger Anti-jitter Dead Zone
- Trigger 微小抖動常見，加入極小 Dead Zone 避免 UI 0.00~0.01 跳動
- 設定 `TriggerDeadZone = 0.02`
- 規則：
  - `if value < TriggerDeadZone => value = 0`
  - 若需要同樣避免 jump，可進一步 rescale：`(value - dz)/(1 - dz)`（v1.2 可選）

---

# 9. 模組介面規格（Interface Specification）— 加入 DI
## 9.1 IControllerDriver
```csharp
public interface IControllerDriver
{
    bool IsConnected { get; }
    XboxControllerState ReadState();
    event EventHandler<bool>? ConnectionChanged;
}
```

## 9.2 ControllerStateManager（以 DI 注入 Driver）
> 規格要求：Manager 不可在內部 `new Driver()`，需由外部注入，方便單元測試以 MockDriver 驗證 TC。

```csharp
public sealed class ControllerStateManager
{
    private readonly IControllerDriver _driver;

    public ControllerStateManager(IControllerDriver driver)
    {
        _driver = driver;
    }

    public XboxControllerState Current { get; private set; } = new XboxControllerState();
    public void Start(int hz = 60);
    public void Stop();
}
```

## 9.3 UI 更新策略（解耦）
- 禁止：`StateUpdated -> BeginInvoke` 以 60Hz 直接推 UI（避免 Message Pump 堆積）
- 必須：UI 用 Timer（16ms）pull `Current`，並更新控件/重繪

---

# 10. WGI 事件與斷線機制（Optimized）
## 10.1 事件訂閱
- `Gamepad.GamepadAdded += ...`
- `Gamepad.GamepadRemoved += ...`

## 10.2 斷線判定
- Removed 事件：最優先、最準確、最低 CPU
- Fallback：`GetCurrentReading()` 若出現裝置遺失相關錯誤 → 立即斷線
- 最終 fallback：連續 N 次失敗（預設 N=3）

---

# 11. WinForms GUI 渲染規格（Flicker-Free）
## 11.1 強制要求 Double Buffering
### 11.1.1 目的
WinForms GDI+ 在 60Hz 頻繁重繪下若未啟用雙緩衝，會明顯閃爍（Flickering）。

### 11.1.2 實作要求
對於繪製搖桿點位的 XY Panel，需繼承 Panel 並 override `OnPaint`，於建構式設定：

```csharp
this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer |
              ControlStyles.OptimizedDoubleBuffer, true);
```

## 11.2 UI Render Loop（Timer-based）
- 使用 `System.Windows.Forms.Timer` Interval=16ms
- Timer Tick：
  - 讀取 `StateManager.Current`（thread-safe snapshot）
  - 更新 Label/ProgressBar
  - 觸發 XY Panel `Invalidate()` 進行重繪

---

# 12. 需求追蹤矩陣（RTM）
| Requirement ID | 說明 | 設計章節 | 對應模組/類別 | 測試案例 ID |
|---|---|---|---|---|
| FR-01 | 事件驅動連線管理 | 5.1 / 10 | WgiDriver / Manager / UI | TC-01 |
| FR-02 | ABXY | 5.2 | Driver / UI | TC-02 |
| FR-03 | D-Pad | 5.3 | Driver / UI | TC-03 |
| FR-04 | Stick（Radial + Rescale） | 5.4 / 8.1 | Manager | TC-04 |
| FR-05 | 系統鍵 Best-effort | 5.5 | Driver / UI | TC-05 |
| FR-06 | Trigger Anti-jitter | 5.6 / 8.2 | Manager | TC-06 |
| FR-07 | Logic 60Hz + UI 掉幀容忍 | 5.7 / 11.2 | Manager / UI | TC-07 |
| FR-08 | 斷線偵測 fallback | 5.8 / 10.2 | Driver / Manager | TC-08 |
| FR-09 | Active Controller 策略 | 5.9 | Driver | TC-09 |

---

# 13. 測試案例骨架（TC）
## TC-04 Stick Radial Dead Zone & Rescaling
- 步驟：
  1. 緩慢從中心推至剛超過 dead zone
  2. 觀察輸出是否平滑（不可從 0 跳到 0.10）
  3. 畫圓：沿圓周旋轉搖桿，觀察是否無「十字斷層」
- 期望：
  - 小於死區 → (0,0)
  - 剛超過死區 → 小幅非零，連續變化
  - 圓周旋轉 → 平滑連續

## TC-06 Trigger Anti-jitter
- 步驟：手指輕放在 Trigger，觀察數值
- 期望：小於 0.02 顯示為 0.00，不再跳動

## TC-01 事件斷線
- 步驟：開啟程式後關閉控制器電源或斷開藍牙
- 期望：收到 Removed 事件後立即顯示 Disconnected，UI 無 freeze

（其餘 TC-02/03/05/07/08/09 依 RTM 擴充）

---

# 14. 開發工作分解（WBS）
1. 建立 WinForms 專案與 DI 容器（Host）
2. 實作 WgiDriver：GamepadAdded/Removed + Active selection
3. 實作 Manager：60Hz logic tick + radial deadzone + trigger anti-jitter
4. 實作 UI：Timer render loop + double buffered XY panel
5. 電池資訊：BatteryReport 映射與 UI 呈現
6. 測試：TC-01/04/06 優先
7. 效能驗證：Logic thread、UI thread CPU 分離觀測

---

# 15. 未來擴充（v1.3+）
- Last Input Wins（Active Controller 更合理）
- XInput fallback
- 震動回饋
- 設定檔（dead zone / trigger dz / mapping）
- 更完整的 BatteryReport 顯示與低電量提示

---

---

# 17. 新增注意事項：可維護與簡潔優先（Maintainability-First）
## 17.1 設計原則（必遵守）
為確保在 **Visual Studio .NET / WinForms** 專案中「**簡單易懂、容易維護、避免過度複雜**」，本專案設計需遵守以下原則：

1. **KISS（Keep It Simple, Stupid）**：能用直觀方式完成的，不引入複雜架構或過度抽象。
2. **最小必要分層**：維持 `Driver / Manager / UI` 三層即可；避免再拆出過多中介層（如過度的 Service/Repository/Factory）。
3. **介面只為測試與替換**：`IControllerDriver` 僅用於 Driver 可替換與 Mock 測試；其餘若無實際需求，避免濫用介面。
4. **DI 使用「輕量」模式**：只注入核心依賴（Driver → Manager → UI），不導入複雜容器設定或過多生命週期管理。
5. **避免事件風暴與過度反應式設計**：採用「Logic 60Hz 更新 + UI Timer pull」為主，不採用高複雜度的 Reactive/Observer chain。
6. **資料結構清晰可讀**：`XboxControllerState` 欄位命名需直觀一致，避免 magic number；DeadZone/Interval 使用常數集中管理。
7. **可除錯性優先**：關鍵流程（裝置 Added/Removed、Active selection、例外降級）需可透過簡單 log 或 Debug Output 追蹤。
8. **避免微優化造成可讀性下降**：允許使用 `Vector2`（清晰且高效），但不為了極端效能引入不易理解的 SIMD 手寫或 unsafe code。

## 17.2 程式碼複雜度約束（建議）
- 單一類別建議控制在 **< 300 行**（不含 Designer 生成碼）
- 單一方法建議控制在 **< 40 行**，超過即抽出私有方法
- 一個模組只做一件事（Single Responsibility）

## 17.3 不採用項目（v1.2 範圍內明確排除）
- MVVM / MVP 等大型 UI 架構導入（WinForms 專案在 v1.2 以簡單為主）
- 反應式框架（Rx）或複雜訊息匯流排（Event Bus）
- 多執行緒渲染管線或自製 scheduler（維持 Timer + OnPaint）


# 16. 核准資訊（Approval）
- Author：System Designer
- Reviewer：Project Lead
- Status：Approved
