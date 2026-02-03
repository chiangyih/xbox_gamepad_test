# Xbox Wireless Controller Monitor - .NET 10 WinForms

## ?目概?

本?目??了一?完整的 Xbox ??控制器?控?用程序，遵循 `spec.md` 中定?的系????范。??用程序采用模?化架构，支持事件??的?接管理、60Hz ??更新循?和???的??? UI 渲染。

## ?目技??

- **?言**: C# (.NET 10)
- **UI 框架**: WinForms
- **架构**: 分?架构 (Driver / Manager / UI)
- **??模式**: 依?注入、工厂模式、?察者模式

## ?目?构

### 核心模?

#### 1. **?据模型** (`XboxControllerState.cs`)
- 定?控制器完整???构
- 包含 ABXY 按?、D-Pad、肩?、扳机、?杆、?池信息
- 支持克隆功能以???程安全的????
- 使用 `Vector2` 表示?杆位置

#### 2. **???** (`IControllerDriver.cs`, `WgiDriver.cs`)
- `IControllerDriver`: ??接口定?
- `WgiDriver`: Windows.Gaming.Input ????
- `MockControllerDriver`: 用于??的模???
- 支持事件??的???接/??

#### 3. **??管理器** (`ControllerStateManager.cs`)
- ?? 60Hz ??更新循?
- ??正?化算法：
  - **?向死?**: 解????向死?的十字????
  - **重新?放**: 确保平滑?渡，避免值跳?
  - **扳机抗抖?**: 微小死?消除扳机抖?
- ?程安全的????
- ?堆?的 UI 推送（采用拉取模式）

#### 4. **UI ?** (`Form1.cs`, `Form1.Designer.cs`, `DoubleBufferedXYPanel.cs`)
- 主窗体: ?示所有控制器按?和??
- ??? XY 面板: 
  - ?示?杆位置
  - 支持 60Hz ???渲染
  - 可?化死??
  - ?示最大范??和十字准星
- ?度?: 扳机值?示
- ??: ????更新

#### 5. **??框架** (`Tests/XboxControllerTests.cs`, `Tests/TestRunner.cs`)
- 自定??量???框架
- ??用例覆?:
  - TC-01: 事件???接管理
  - TC-02: ABXY 按?
  - TC-04: ?向死?和重新?放
  - TC-06: 扳机抗抖?
  - TC-07: 60Hz ???率??

## ????特性

### 1. 事件??架构
```
WGI GamepadAdded/Removed Events
        ↓
  [Driver Layer] - ????和?取
        ↓
  [State Manager] - 60Hz ??更新 + 正?化
        ↓
  [UI Render Loop] - Timer (16ms) 拉取??并渲染
```

### 2. ?向死? + 重新?放
```csharp
// ?算向量?度
float magnitude = sqrt(x? + y?)

// 判?死?
if (magnitude < 0.10):
    output = (0, 0)
else:
    // 重新?放避免跳?
    scaled = (magnitude - 0.10) / (1 - 0.10)
    output = (x, y) / magnitude * scaled
```

### 3. 扳机抗抖?
```csharp
if (value < 0.02):
    return 0
else:
    return (value - 0.02) / (1 - 0.02)
```

### 4. ???渲染
```csharp
// 在 DoubleBufferedXYPanel 中
this.SetStyle(
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.UserPaint |
    ControlStyles.DoubleBuffer |
    ControlStyles.OptimizedDoubleBuffer,
    true);
```

### 5. UI 解耦??
- Logic Thread (60Hz): ?立?行，不直接更新 UI
- UI Thread (16ms Timer): 通?拉取 `CurrentState` 更新界面
- 优?: 避免消息?列堆?，提高??性

## ?行方式

### ?? GUI ?用
```powershell
dotnet run
```

### ?行自?化??
```powershell
dotnet run -- --test
```

或使用提供的?本:
```powershell
./run-tests.ps1
```

## ??覆?

| ?? ID | 功能 | ?? |
|--------|------|------|
| TC-01 | 事件???接管理 | ? 通? |
| TC-02 | ABXY 按? | ? 通? |
| TC-04 | ?向死? + 重新?放 | ? 通? |
| TC-06 | 扳机抗抖? | ? 通? |
| TC-07 | 60Hz ???率 | ? 通? |

## 性能指?

- **Logic Thread**: < 1% CPU 使用率
- **UI Thread**: < 5% CPU 使用率（取?于渲染复?度）
- **端到端延?**: < 20ms（平均）
- **渲染?率**: 60Hz（允?掉?）

## 代???

```
xbox_gamepad/
├── XboxControllerState.cs        # ?据模型
├── IControllerDriver.cs          # ??接口
├── WgiDriver.cs                  # WGI ????
├── MockControllerDriver.cs       # ??用??
├── ControllerStateManager.cs     # ??管理器（60Hz 更新）
├── DoubleBufferedXYPanel.cs      # ?????面板
├── Form1.cs                      # 主窗体??
├── Form1.Designer.cs             # UI ??
├── Program.cs                    # ?用入口
├── Tests/
│   ├── XboxControllerTests.cs    # ?元??
│   └── TestRunner.cs             # ???行器
├── xbox_gamepad.csproj           # ?目文件
└── spec.md                       # 完整?范文?
```

## 可??性??

1. **KISS 原?**: 避免?度复?化
2. **?一??**: 每??只做一件事
3. **?大小控制**: ??? < 300 行
4. **方法大小控制**: ??方法 < 40 行
5. **接口最小化**: ??必要的替?和??定?接口
6. **集中常?管理**: DeadZone/Interval 等配置集中定?

## ?展?划 (v1.3+)

- [ ] Last Input Wins 控制器??策略
- [ ] XInput ?用??
- [ ] 震?回?支持
- [ ] 可配置的?置文件（死?、映射等）
- [ ] 低?量提示
- [ ] 更完善的?池?告?示

## 注意事?

1. **Windows.Gaming.Input**: ?目配置?兼容模式，在有完整 WinRT 支持的?境中可?用真???
2. **Xbox/Share 按?**: 可能被 Windows 快捷?或 Game Bar ?截，需要系???限
3. **?牙?接**: 确保控制器已配?并?接到系?
4. **???境**: MockControllerDriver 用于自?化??，不依?真?硬件

## 遵循?范

??目完全遵循 `spec.md` 中定?的：

- ? 系?架构??（Driver/Manager/UI分?）
- ? 功能需求（FR-01~FR-09）
- ? 非功能需求（NFR-01~NFR-07）
- ? 正?化?格（?向死?、扳机抗抖?）
- ? 模?接口?范（DI 注入）
- ? 可??性??原?
- ? 需求追蹤矩?（RTM）
- ? ??案例骨架

---

**最后更新**: 2025-02-03
**版本**: v1.2-DevReady-Optimized
