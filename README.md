# Xbox 搖桿測試 Monitor
<img width="850" height="580" alt="image" src="https://github.com/user-attachments/assets/388bcf45-5b51-4149-8166-4e0540405564" />

使用 .NET 10 開發的 Xbox 手把即時監控程式，視覺化顯示手把的所有按鈕、搖桿和扳機狀態。

## 專案架構

```
xbox_gamepad/
├── Program.cs                      # 程式進入點
├── Form1.cs                        # 主視窗邏輯
├── Form1.Designer.cs              # 主視窗 UI 配置
├── XboxControllerState.cs         # 手把狀態資料結構
├── IControllerDriver.cs           # 手把驅動介面
├── WgiDriver.cs                   # XInput 驅動實作
├── ControllerStateManager.cs     # 狀態管理器
├── DoubleBufferedXYPanel.cs      # 自訂雙緩衝繪圖面板
└── README.md                      # 專案說明文件
```

## 程式架構

### 1. 架構層次

```
┌─────────────────────────────────────────┐
│          Form1 (UI Layer)               │
│  - 顯示所有手把狀態                       │
│  - 60Hz 更新渲染                         │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   ControllerStateManager (Logic Layer)  │
│  - 60Hz 讀取原始狀態                     │
│  - 正規化處理 (死區、縮放)                │
│  - 執行緒安全的狀態存取                   │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│    WgiDriver (Hardware Layer)           │
│  - 使用 SharpDX.XInput                  │
│  - 自動偵測手把連線                       │
│  - 讀取原始輸入資料                       │
└─────────────────────────────────────────┘
```

### 2. 資料流程

```
Hardware → WgiDriver → ControllerStateManager → Form1 → UI Display
                ↓              ↓                    ↓
          原始資料      正規化資料              視覺化呈現
```

### 3. 核心元件說明

#### XboxControllerState.cs
儲存手把的完整狀態資訊：
- **按鈕狀態**: A, B, X, Y, LB, RB, 方向鍵, View, Menu, L3, R3
- **搖桿狀態**: LeftStick (Vector2), RightStick (Vector2)
- **扳機狀態**: LT (float), RT (float)
- **系統狀態**: IsConnected, BatteryStatus, RemainingLevel

#### WgiDriver.cs
使用 SharpDX.XInput 與 Xbox 手把溝通：
- 每 500ms 掃描並偵測手把連線
- 支援最多 4 個手把 (UserIndex 0-3)
- 將原始 XInput 資料轉換為統一格式
- 自動處理連線/斷線事件

#### ControllerStateManager.cs
管理手把狀態並執行正規化處理：
- **60Hz 邏輯更新頻率**
- 套用搖桿死區 (Radial Dead Zone: 10%)
- 套用扳機死區 (Linear Dead Zone: 2%)
- 提供執行緒安全的狀態讀取

#### Form1.cs
主視窗，負責 UI 顯示：
- **60Hz 渲染更新頻率** (~16ms)
- 按鈕按下時改變顏色 (白底黑字)
- 即時顯示搖桿座標和扳機數值
- 居中對稱的視覺化配置

#### DoubleBufferedXYPanel.cs
自訂的雙緩衝繪圖控制項：
- 繪製搖桿位置的視覺化圓點
- 消除閃爍的雙緩衝繪圖
- 座標映射 ([-1, 1] → 像素座標)

## 如何讀取手把狀態值

### 在 Form1.cs 中讀取狀態

```csharp
private void OnRenderTick(object? sender, EventArgs e)
{
    if (_stateManager == null) return;

    // 取得當前手把狀態 (執行緒安全)
    var state = _stateManager.Current;

    // 現在可以讀取所有狀態值
    ReadControllerValues(state);
}
```

### 讀取 Left Stick 的 X、Y 座標值

```csharp
// state 是 XboxControllerState 物件
Vector2 leftStickPosition = state.LeftStick;

// 取得 X 座標 (範圍: -1.0 到 +1.0)
float leftStickX = state.LeftStick.X;

// 取得 Y 座標 (範圍: -1.0 到 +1.0)
float leftStickY = state.LeftStick.Y;

// 範例：顯示在 UI 上
_leftStickCoordLabel.Text = $"X: {state.LeftStick.X:F2}  Y: {state.LeftStick.Y:F2}";
```

**座標說明**：
- **X 軸**: -1.0 (最左) → 0.0 (中間) → +1.0 (最右)
- **Y 軸**: -1.0 (最下) → 0.0 (中間) → +1.0 (最上)
- 已套用 10% 的徑向死區 (Radial Dead Zone)

### 讀取 Right Stick 的 X、Y 座標值

```csharp
// state 是 XboxControllerState 物件
Vector2 rightStickPosition = state.RightStick;

// 取得 X 座標 (範圍: -1.0 到 +1.0)
float rightStickX = state.RightStick.X;

// 取得 Y 座標 (範圍: -1.0 到 +1.0)
float rightStickY = state.RightStick.Y;

// 範例：顯示在 UI 上
_rightStickCoordLabel.Text = $"X: {state.RightStick.X:F2}  Y: {state.RightStick.Y:F2}";
```

### 讀取 LT (Left Trigger) 狀態值

```csharp
// state 是 XboxControllerState 物件

// 取得 LT 扳機值 (範圍: 0.0 到 1.0)
float leftTrigger = state.LT;

// 0.0 = 完全放開
// 1.0 = 完全按下
// 已套用 2% 的線性死區

// 範例：顯示在 UI 上
_ltLabel.Text = $"LT: {state.LT:F2}";
_ltProgress.Value = (int)(state.LT * 100);  // 進度條 0-100
```

### 讀取 RT (Right Trigger) 狀態值

```csharp
// state 是 XboxControllerState 物件

// 取得 RT 扳機值 (範圍: 0.0 到 1.0)
float rightTrigger = state.RT;

// 0.0 = 完全放開
// 1.0 = 完全按下
// 已套用 2% 的線性死區

// 範例：顯示在 UI 上
_rtLabel.Text = $"RT: {state.RT:F2}";
_rtProgress.Value = (int)(state.RT * 100);  // 進度條 0-100
```

### 完整範例：讀取所有軸向資料

```csharp
private void ReadAllAxesValues(XboxControllerState state)
{
    // === 左搖桿 ===
    float leftStickX = state.LeftStick.X;     // -1.0 到 +1.0
    float leftStickY = state.LeftStick.Y;     // -1.0 到 +1.0
    
    // === 右搖桿 ===
    float rightStickX = state.RightStick.X;   // -1.0 到 +1.0
    float rightStickY = state.RightStick.Y;   // -1.0 到 +1.0
    
    // === 扳機 ===
    float leftTrigger = state.LT;             // 0.0 到 1.0
    float rightTrigger = state.RT;            // 0.0 到 1.0
    
    // 使用這些值進行遊戲邏輯處理
    Console.WriteLine($"Left Stick: ({leftStickX:F2}, {leftStickY:F2})");
    Console.WriteLine($"Right Stick: ({rightStickX:F2}, {rightStickY:F2})");
    Console.WriteLine($"Triggers: LT={leftTrigger:F2}, RT={rightTrigger:F2}");
}
```

## 重要變數對照表

| UI 顯示 | 變數名稱 | 類型 | 範圍 | 說明 |
|---------|---------|------|------|------|
| Left Stick X | `state.LeftStick.X` | float | -1.0 ~ +1.0 | 左搖桿 X 軸 |
| Left Stick Y | `state.LeftStick.Y` | float | -1.0 ~ +1.0 | 左搖桿 Y 軸 |
| Right Stick X | `state.RightStick.X` | float | -1.0 ~ +1.0 | 右搖桿 X 軸 |
| Right Stick Y | `state.RightStick.Y` | float | -1.0 ~ +1.0 | 右搖桿 Y 軸 |
| LT | `state.LT` | float | 0.0 ~ 1.0 | 左扳機 |
| RT | `state.RT` | float | 0.0 ~ 1.0 | 右扳機 |

## 死區 (Dead Zone) 設定

程式內建的死區設定在 `ControllerStateManager.cs`：

```csharp
private const float StickDeadZone = 0.10f;      // 搖桿: 10% 徑向死區
private const float TriggerDeadZone = 0.02f;    // 扳機: 2% 線性死區
```

- **搖桿死區**: 使用徑向死區演算法，當搖桿距離中心點小於 10% 時，視為 (0, 0)
- **扳機死區**: 使用線性死區，當扳機值小於 2% 時，視為 0.0

## 系統需求

- **.NET 10.0 SDK**
- **Windows 10/11** (支援 XInput)
- **Xbox 無線/有線手把** (或相容 XInput 的手把)

## 依賴套件

### NuGet 套件

本專案使用以下 NuGet 套件：

#### 1. SharpDX.XInput (4.2.0)
**用途**: Xbox 手把輸入處理

**功能說明**:
- 提供對 Windows XInput API 的 .NET 封裝
- 支援讀取 Xbox 360、Xbox One 和 Xbox Series X|S 手把
- 可讀取所有按鈕、搖桿、扳機和 D-Pad 狀態
- 支援震動回饋功能（本專案未使用）
- 支援電池狀態查詢

**安裝方式**:
```bash
dotnet add package SharpDX.XInput --version 4.2.0
```

**主要使用的類別**:
- `Controller`: 代表一個 XInput 手把
- `State`: 包含手把的完整狀態資訊
- `Gamepad`: 包含按鈕和軸向的原始數值
- `GamepadButtonFlags`: 按鈕狀態的位元旗標
- `UserIndex`: 手把索引 (One, Two, Three, Four)
- `BatteryInformation`: 電池資訊結構

**官方文件**: https://sharpdx.org/

**授權**: MIT License

### .NET Framework 相依性

本專案基於 **.NET 10.0** 並使用以下內建套件：

#### 1. System.Numerics.Vectors
**用途**: 提供 `Vector2` 類型用於搖桿座標

**功能說明**:
- `Vector2` 結構用於表示 2D 向量 (X, Y)
- 支援向量運算（加減乘除、長度計算等）
- 用於儲存 Left Stick 和 Right Stick 的座標

**使用範例**:
```csharp
Vector2 leftStick = new Vector2(x, y);
float magnitude = leftStick.Length();  // 計算向量長度
```

#### 2. System.Windows.Forms
**用途**: WinForms UI 框架

**功能說明**:
- 提供 Windows 桌面應用程式的 UI 元件
- 包含 Form, Button, Label, ProgressBar 等控制項
- 提供 Timer 用於定時更新 UI
- 支援雙緩衝繪圖消除閃爍

**主要使用的類別**:
- `Form`: 主視窗基礎類別
- `Button`: 按鈕控制項
- `Label`: 文字標籤控制項
- `ProgressBar`: 進度條控制項
- `Timer`: 定時器（用於 60Hz UI 更新）
- `Panel`: 容器控制項（自訂繪圖面板的基礎）

#### 3. System.Threading
**用途**: 執行緒安全和並行處理

**功能說明**:
- 提供 `lock` 關鍵字用於執行緒安全
- `ControllerStateManager` 使用 lock 保護共享狀態
- 確保邏輯執行緒和 UI 執行緒間的資料一致性

**使用範例**:
```csharp
private readonly object _stateLock = new();

lock (_stateLock)
{
    return _currentState.Clone();
}
```

### 開發工具需求

- **Visual Studio 2022** (17.8 或更新版本) 或 **Visual Studio Code**
- **.NET 10.0 SDK** (Preview 版本)
- **NuGet Package Manager** (通常已內建於 Visual Studio)

### 套件還原

專案首次建置時會自動還原所有 NuGet 套件：

```bash
# 手動還原套件
dotnet restore

# 檢視已安裝的套件
dotnet list package

# 更新套件到最新版本（不建議，可能破壞相容性）
dotnet add package SharpDX.XInput
```

### 套件版本鎖定

本專案使用 `PackageReference` 格式並鎖定特定版本以確保穩定性：

```xml
<PackageReference Include="SharpDX.XInput" Version="4.2.0" />
```

**注意**: SharpDX.XInput 4.2.0 是最後一個穩定版本，建議不要升級。

### 相容性說明

| 套件 | 版本 | .NET 10 相容性 | Windows 相容性 |
|------|------|---------------|---------------|
| SharpDX.XInput | 4.2.0 | ✅ 完全相容 | Windows 7+ |
| System.Numerics.Vectors | 內建 | ✅ 原生支援 | 全平台 |
| System.Windows.Forms | 內建 | ✅ 原生支援 | Windows Only |

### 授權資訊

- **SharpDX.XInput**: MIT License - 可自由用於商業和開源專案
- **.NET Runtime**: MIT License
- **本專案**: 開源專案，歡迎使用和修改

## 建置專案

```bash
# 還原套件
dotnet restore

# 建置專案
dotnet build

# 執行程式
dotnet run
```

## 程式特點

✅ **即時監控**: 60Hz 更新頻率，無延遲顯示  
✅ **自動偵測**: 自動掃描並連接可用的 Xbox 手把  
✅ **視覺化顯示**: 所有按鈕、搖桿、扳機一目了然  
✅ **精確數值**: 顯示搖桿座標和扳機的精確浮點數值  
✅ **死區處理**: 內建死區演算法，消除搖桿飄移  
✅ **執行緒安全**: 狀態讀取使用鎖定機制保證安全  
✅ **緊湊配置**: 850x550 視窗，元件對稱居中  

## 技術亮點

1. **三層式架構**: 硬體層、邏輯層、UI層分離
2. **執行緒安全**: 使用 `lock` 保護共享狀態
3. **雙緩衝繪圖**: 自訂控制項消除閃爍
4. **即時渲染**: 獨立的邏輯更新和 UI 渲染循環
5. **徑向死區**: 比線性死區更精確的搖桿正規化

## 作者

**Tseng**

## 授權

此專案為開源專案，歡迎使用和修改。

---

**最后更新**: 2025-02-03
**版本**: v1.2-DevReady-Optimized

---


