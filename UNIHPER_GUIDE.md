# UNIHper 框架使用指南

> 此文件由 AI 上下文生成器自动管理，用于向 AI 提供框架使用上下文

## 核心架构

UNIHper 是一个 Unity 应用开发框架，通过 `Managements` 门面类提供统一的服务访问。

### 架构总览

```
┌─────────────────────────────────────────────────────────────────┐
│                      Managements 门面层                          │
│  Config | UI | UIToolkit | Resource | Scene | Audio | Event...  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────┼──────────────────────────────────┐
│                        Manager 管理层                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ConfigManager│  │  UIManager  │  │ResourceMgr  │  ...         │
│  │  (配置管理)  │  │ (UGUI管理)  │  │ (资源管理)  │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│                        ┌─────────────────┐                      │
│                        │UIToolkitManager │                      │
│                        │(UI Toolkit管理) │                      │
│                        └─────────────────┘                      │
└─────────────────────────────────────────────────────────────────┘
                               │
┌──────────────────────────────┼──────────────────────────────────┐
│                        基础设施层                                │
│  UConfig | UIBase | UIToolkitBase | UniRx | Singleton...        │
└─────────────────────────────────────────────────────────────────┘
```

### Managements 门面 API

```csharp
using UNIHper;

// UI 管理 (UGUI)
Managements.UI.Show<MyUI>();                    // 显示 UI
Managements.UI.Hide<MyUI>();                    // 隐藏 UI
Managements.UI.Get<MyUI>();                     // 获取 UI 实例
Managements.UI.IsShowing<MyUI>();               // 检查是否可见
Managements.UI.Toggle<MyUI>();                  // 切换显示/隐藏
Managements.UI.HideAll();                       // 隐藏所有 UI
Managements.UI.StashActiveUI();                 // 暂存当前活动 UI
Managements.UI.PopStashedUI();                  // 恢复暂存的 UI

// UI Toolkit 管理 (新一代 UI)
Managements.UIToolkit.Show<MyToolkitUI>();      // 显示 UI Toolkit 页面
Managements.UIToolkit.Hide<MyToolkitUI>();      // 隐藏 UI Toolkit 页面
Managements.UIToolkit.Get<MyToolkitUI>();       // 获取实例

// 资源管理
Managements.Resource.Get<T>("resourceName");    // 同步获取资源
Managements.Resource.GetMany<T>("filter");      // 筛选获取多个资源
Managements.Resource.GetLabelAssets<T>("label");// 按标签获取资源
Managements.Resource.Exists<T>("name");         // 检查资源是否存在
Managements.Resource.AppendAudioClip("path");   // 加载外部音频（返回 IObservable）
Managements.Resource.AppendTexture2D("path");   // 加载外部图片（返回 IObservable）
Managements.Resource.AppendAssetBundle("name"); // 加载 AB 包

// 音频管理
Managements.Audio.PlayMusic("BGM");             // 播放背景音乐
Managements.Audio.PlayEffect("Click");          // 播放音效
Managements.Audio.StopMusic();                  // 停止音乐
Managements.Audio.PauseMusic();                 // 暂停音乐

// 事件系统
Managements.Event.Fire(new MyEvent());          // 触发事件
Managements.Event.Register<MyEvent>(handler);   // 注册事件处理
Managements.Event.Unregister<MyEvent>(handler); // 注销事件处理
Managements.Event.Unregister<MyEvent>();        // 注销某事件所有处理

// 配置管理
Managements.Config.Get<GameConfig>();           // 获取配置实例
Managements.Config.Save<GameConfig>();          // 保存配置
Managements.Config.Reload<GameConfig>();        // 重新加载配置
Managements.Config.SaveAll();                   // 保存所有配置

// 定时器
Managements.Timer.Delay(1f, callback);          // 延迟执行
Managements.Timer.Interval(0.5f, callback);     // 循环执行
Managements.Timer.Cancel("timerKey");           // 取消定时器
Managements.Timer.Countdown(10f);               // 倒计时
Managements.Timer.NextFrame(callback);          // 下一帧执行
Managements.Timer.Throttle(0.5f, callback);     // 节流
Managements.Timer.Debounce(0.3f, callback);     // 防抖

// 场景管理
Managements.Scene.LoadSceneAsync("SceneName");  // 异步加载场景
```

### 简写门面类

框架还提供简写门面类，用于更简洁的代码：

| 简写类 | 对应管理器 | 说明 |
|--------|-----------|------|
| `UIMgr` | UIManager | UGUI 管理 |
| `ResMgr` | ResourceManager | 资源管理 |
| `CfgMgr` | ConfigManager | 配置管理 |
| `AudioMgr` | AudioManager | 音频管理 |
| `TimerMgr` | TimerManager | 定时器管理 |
| `EventMgr` | EventManager | 事件管理 |
| `SceneMgr` | SceneManager | 场景管理 |

---

## 资源管理系统 (ResourceManager)

### 架构概述

ResourceManager 支持三种资源驱动，通过 `resources.json` 配置资源加载策略：

| 驱动类型 | 说明 | 适用场景 |
|---------|------|---------|
| `Resources` | Unity 内置 Resources 文件夹加载 | 小型资源、配置文件 |
| `Addressable` | Unity Addressable 系统 | 大型资源、按需加载、热更新 |
| `AssetBundle` | 传统 AB 包方式 | 兼容旧项目 |

### resources.json 配置

配置文件位置：`Assets/Resources/UNIHper/resources.json`

```json
{
  "Persistence": [
    {
      "driver": "Addressable",
      "type": "Object",
      "label": "default"
    },
    {
      "driver": "Addressable",
      "type": "Sprite",
      "label": "default"
    },
    {
      "driver": "Addressable",
      "type": "VisualTreeAsset",
      "label": "default"
    },
    {
      "driver": "Resources",
      "type": "GameObject",
      "path": "Prefabs/Common"
    }
  ],
  "SceneEntry": [
    {
      "driver": "Addressable",
      "type": "AudioClip",
      "label": "scene_entry"
    }
  ]
}
```

### 配置字段说明

| 字段 | 说明 | 示例 |
|------|------|------|
| `driver` | 资源驱动类型 | `"Resources"`, `"Addressable"`, `"AssetBundle"` |
| `type` | Unity 资源类型（不含命名空间） | `"GameObject"`, `"Sprite"`, `"AudioClip"`, `"VisualTreeAsset"` |
| `path` | Resources 文件夹相对路径 | `"Prefabs/UI"` |
| `label` | Addressable 标签名 | `"default"`, `"scene_entry"` |

### 资源生命周期

| 生命周期 | 说明 | 加载时机 | 卸载时机 |
|---------|------|---------|---------|
| **Persistence** | 持久性资源 | 游戏启动时 | 游戏退出时 |
| **Scene{Name}** | 场景级资源 | 进入场景时 | 离开场景时 |
| **__custom** | 自定义资源 | 运行时动态加载 | 手动卸载 |

### 代码使用

```csharp
using UNIHper;

// 同步获取已加载资源（支持部分路径匹配）
var prefab = Managements.Resource.Get<GameObject>("PlayerPrefab");
var sprite = Managements.Resource.Get<Sprite>("Icons/Star");
var uxml = Managements.Resource.Get<VisualTreeAsset>("UI/MainMenu");

// 检查资源是否存在
bool exists = Managements.Resource.Exists<Sprite>("Icons/Star");

// 按标签获取资源列表
var sprites = Managements.Resource.GetLabelAssets<Sprite>("ui_icons");

// 筛选获取多个资源（支持通配符匹配）
var allIcons = Managements.Resource.GetMany<Sprite>("Icons/");

// 加载外部音频文件
Managements.Resource.AppendAudioClip("D:/Audio/bgm.wav")
    .Subscribe(clip => audioSource.clip = clip)
    .AddTo(this);

// 加载外部目录下的所有音频
Managements.Resource.AppendAudioClips("D:/Audio", "*.wav|*.mp3")
    .Subscribe(clips => ProcessClips(clips))
    .AddTo(this);

// 加载外部图片
Managements.Resource.AppendTexture2D("D:/Images/photo.png")
    .Subscribe(texture => rawImage.texture = texture)
    .AddTo(this);

// 加载外部目录下的所有图片
Managements.Resource.AppendTexture2Ds("D:/Images", "*.png|*.jpg")
    .Subscribe(textures => ProcessTextures(textures))
    .AddTo(this);

// 加载 AB 包
var bundle = Managements.Resource.AppendAssetBundle("characters/hero");
// 卸载 AB 包
Managements.Resource.UnloadAssetBundle("characters/hero");
```

### Addressable 资源配置

1. **添加到 Addressable 系统**: 
   - 在 Unity 中右键资源/目录 → `Add To Addressable System`
   - 或手动在 Addressable Groups 窗口配置

2. **设置标签**:
   - 在 Addressable Groups 中为资源设置 Label
   - resources.json 中使用对应的 label 名称

3. **资源键格式**: `{路径}_{类型全名}`
   - 例如: `Assets/UI/MainMenu.uxml_UnityEngine.UIElements.VisualTreeAsset`

---

## 配置管理系统 (ConfigManager)

### 架构概述

ConfigManager 提供自动化的配置序列化/反序列化，支持 XML、JSON、YAML 三种格式。

### UConfig 基类

所有配置类继承 `UConfig`，使用特性标注配置：

```csharp
using UNIHper;
using System.Xml.Serialization;
using Newtonsoft.Json;

// 存储位置和序列化格式
[SerializedAt(AppPath.StreamingDir, "Configs", Priority = 0)]
[SerializeWith(ConfigDriver.XML)]
public class GameConfig : UConfig
{
    // 简单属性
    public int MaxScore = 100;
    public float MusicVolume = 0.8f;
    public string PlayerName = "Player";

    // 复杂对象
    public DifficultySettings Difficulty = new DifficultySettings();

    // 不序列化的属性
    [XmlIgnore, JsonIgnore]
    public bool IsInitialized { get; set; }

    // 反序列化后调用
    protected override void OnDeserialized()
    {
        Debug.Log("配置反序列化完成");
    }

    // 配置加载后调用（所有配置加载完成后）
    protected override void OnLoaded()
    {
        // 验证和修正值
        if (MaxScore < 0) MaxScore = 100;
        IsInitialized = true;
    }

    // 配置卸载时调用
    protected override void OnUnloaded()
    {
        Debug.Log("配置已卸载");
    }

    // 序列化前调用
    protected override void OnSerializing()
    {
        Debug.Log("即将保存配置");
    }

    // 序列化后调用
    protected override void OnSerialized()
    {
        Debug.Log("配置已保存");
    }

    // XML 文件注释
    protected override string Comment()
    {
        return "游戏主配置文件";
    }
}

public class DifficultySettings
{
    public int Level = 1;
    public float TimeLimit = 60f;
}
```

### SerializedAt 特性

| 参数 | 类型 | 说明 |
|------|------|------|
| `RootDir` | `AppPath` | 根目录位置 |
| `SubDir` | `string` | 子目录（默认 "Configs"） |
| `FileName` | `string` | 文件名（默认使用类名） |
| `Priority` | `int` | 加载优先级（数值小优先） |
| `RecoverOnError` | `bool` | 配置错误时是否自动恢复 |

### AppPath 枚举

| 值 | 路径 | 说明 |
|----|------|------|
| `StreamingDir` | `Application.streamingAssetsPath` | 流媒体目录（只读） |
| `PersistentDir` | `Application.persistentDataPath` | 持久化目录（可读写） |
| `DataDir` | `Application.dataPath` | 数据目录 |
| `ProjectDir` | 项目根目录 | 仅编辑器可用 |
| `None` | - | 不保存配置 |

### SerializeWith 特性

| 驱动 | 文件后缀 | 说明 |
|------|---------|------|
| `ConfigDriver.XML` | `.xml` | XML 格式（默认） |
| `ConfigDriver.JSON` | `.json` | JSON 格式 |
| `ConfigDriver.YAML` | `.yaml` | YAML 格式 |

### 代码使用

```csharp
using UNIHper;

// 获取配置实例
var config = Managements.Config.Get<GameConfig>();

// 读取值
int maxScore = config.MaxScore;
string playerName = config.PlayerName;

// 修改值
config.MaxScore = 200;
config.PlayerName = "NewPlayer";

// 保存配置（方式1：通过实例）
config.Save();

// 保存配置（方式2：通过管理器）
Managements.Config.Save<GameConfig>();

// 重新加载配置（从文件重新读取）
var reloaded = Managements.Config.Reload<GameConfig>();

// 保存所有配置
Managements.Config.SaveAll();

// 删除配置文件
config.Delete();

// 转换为 JSON 字符串
string json = config.ToJson();

// 动态设置序列化位置
Managements.Config.SetSerializedAt<GameConfig>(AppPath.PersistentDir, "MyConfigs");
```

### 配置文件自动备份与恢复

- **自动备份**: 配置文件保存时会自动备份到 `persistentDataPath/Backup/Configs/`
- **自动恢复**: 当配置文件损坏且 `RecoverOnError=true` 时，自动从备份恢复
- **错误归档**: 损坏的配置文件会被移动到 `persistentDataPath/Error/Configs/`

---

## UI 管理系统

UNIHper 支持两套 UI 系统：**UGUI（UIManager）** 和 **UI Toolkit（UIToolkitManager）**。

### UGUI 系统 (UIManager)

基于 Unity UGUI 的传统 UI 系统，适合现有项目和需要复杂动画的场景。

#### UIBase 基类

所有 UGUI 脚本继承自 `UIBase`，使用特性标注配置：

```csharp
using UNIHper;
using UniRx;
using UnityEngine.UI;
using TMPro;

[UIPage(Asset = "MainMenuUI", Type = UIType.Normal, Canvas = "CanvasDefault")]
public class MainMenuUI : UIBase
{
    private CompositeDisposable _disposables = new CompositeDisposable();

    // UI 创建时调用（仅一次）- 初始化组件引用
    protected override void OnCreate()
    {
        // 使用 Get<T>() 获取子节点组件
        var startBtn = Get<Button>("StartButton");
        var titleText = Get<TMP_Text>("Title/Text");
    }

    // UI 显示时调用 - 订阅事件、刷新数据
    protected override void OnShow()
    {
        Get<Button>("StartButton")
            .OnClickAsObservable()
            .Subscribe(_ => OnStartClicked())
            .AddTo(_disposables);
    }

    // UI 隐藏时调用 - 清理订阅
    protected override void OnHide()
    {
        _disposables.Clear();
    }

    private void OnStartClicked()
    {
        Managements.UI.Hide<MainMenuUI>();
        Managements.Scene.LoadSceneAsync("GameScene");
    }
}
```

#### UIPage 特性

| 参数 | 类型 | 说明 |
|------|------|------|
| `Asset` | `string` | UI 预制体资源名 |
| `Type` | `UIType` | UI 类型 |
| `Canvas` | `string` | 所属 Canvas（默认 "CanvasDefault"） |
| `Order` | `int` | 排序顺序 |
| `InstID` | `int` | 实例 ID（用于多实例） |
| `Scene` | `string` | 所属场景（默认 "Persistence"） |

#### UIType 类型

| 类型 | 说明 |
|------|------|
| `UIType.Normal` | 普通页面，可叠加显示 |
| `UIType.Popup` | 弹窗，显示时暂停背后 UI |
| `UIType.Fixed` | 固定 UI，不参与页面栈管理 |

#### Get<T>() 方法

`UIBase.Get<T>(path)` 用于获取子节点组件，路径使用 `/` 分隔：

```csharp
// 直接子节点
var btn = Get<Button>("ConfirmBtn");

// 深层子节点
var text = Get<TMP_Text>("Panel/Content/Title");

// 获取 Transform
var container = Get<Transform>("ItemContainer");
```

---

### UI Toolkit 系统 (UIToolkitManager)

基于 Unity UI Toolkit 的新一代 UI 系统，适合需要更好性能和响应式布局的场景。

#### UIToolkitBase 基类

```csharp
using UNIHper;
using UNIHper.UI;
using UnityEngine;
using UnityEngine.UIElements;

[UIToolkitPage(Asset = "UI/SettingsUI", Type = UIType.Normal)]
public class SettingsUI : UIToolkitBase
{
    private TextField _nameField;
    private Slider _volumeSlider;

    // UI 加载时调用（仅一次）
    protected override void OnLoaded()
    {
        Debug.Log("UI 加载完成");
    }

    // UI 开始显示时调用
    protected override void OnShowing()
    {
        // 使用 Q<T>() 查询元素（类似 CSS 选择器）
        _nameField = Q<TextField>("NameInput");
        _volumeSlider = Q<Slider>("VolumeSlider");

        // 使用便捷绑定方法
        BindButton("SaveButton", OnSaveClicked);
        BindButton("CancelButton", () => Hide());
        BindTextField("NameInput", OnNameChanged);
        BindSlider("VolumeSlider", OnVolumeChanged);
        BindToggle("MuteToggle", OnMuteChanged);
    }

    // UI 完全显示后调用
    protected override void OnShown()
    {
        Debug.Log("UI 完全显示");
    }

    // UI 开始隐藏时调用
    protected override void OnHiding()
    {
        Debug.Log("UI 开始隐藏");
    }

    // UI 完全隐藏后调用
    protected override void OnHidden()
    {
        Debug.Log("UI 完全隐藏");
    }

    private void OnSaveClicked() => Debug.Log("保存设置");
    private void OnNameChanged(string value) => Debug.Log($"名称: {value}");
    private void OnVolumeChanged(float value) => Debug.Log($"音量: {value}");
    private void OnMuteChanged(bool value) => Debug.Log($"静音: {value}");
}
```

#### UIToolkitPage 特性

| 参数 | 类型 | 说明 |
|------|------|------|
| `Asset` | `string` | UXML 资源名 |
| `Type` | `UIType` | UI 类型 |
| `Order` | `int` | 排序顺序 |
| `InstID` | `int` | 实例 ID |
| `Scene` | `string` | 所属场景 |
| `PanelSettings` | `string` | Panel Settings 资源名（可选） |

#### 元素查询方法

```csharp
// 按名称查询单个元素
var button = Q<Button>("MyButton");

// 按类名查询单个元素
var label = Q<Label>(className: "title-label");

// 同时按名称和类名查询
var field = Q<TextField>("Input", "primary-input");

// 查询所有匹配元素
var allButtons = QAll<Button>();
var allLabels = QAll<Label>(className: "item-label");
```

#### 便捷绑定方法

```csharp
// 绑定按钮点击
BindButton("ButtonName", () => { /* 点击处理 */ });

// 绑定文本框变化
BindTextField("FieldName", value => { /* 文本变化处理 */ });

// 绑定开关变化
BindToggle("ToggleName", isOn => { /* 开关变化处理 */ });

// 绑定滑块变化
BindSlider("SliderName", value => { /* 滑块变化处理 */ });
```

#### 显示/隐藏动画

UIToolkitBase 支持异步动画，可重写以下方法：

```csharp
protected override async Task HandleShowAnimation(CancellationToken cancellationToken)
{
    // 使用内置动画
    await UIToolkitAnimations.FadeIn(Root, ShowDuration, cancellationToken);
    // 或
    await UIToolkitAnimations.ScaleIn(Root, ShowDuration, cancellationToken);
    // 或
    await UIToolkitAnimations.SlideInFromBottom(Root, ShowDuration, cancellationToken);
}

protected override async Task HandleHideAnimation(CancellationToken cancellationToken)
{
    await UIToolkitAnimations.FadeOut(Root, HideDuration, cancellationToken);
}
```

#### UXML 文件示例

```xml
<?xml version="1.0" encoding="utf-8"?>
<engine:UXML xmlns:engine="UnityEngine.UIElements">
    <Style src="project://database/Assets/UI/Styles/Common.uss" />
    <engine:VisualElement name="Root" class="panel">
        <engine:Label name="Title" text="设置" class="title" />
        <engine:TextField name="NameInput" label="玩家名称" />
        <engine:Slider name="VolumeSlider" label="音量" low-value="0" high-value="100" />
        <engine:Toggle name="MuteToggle" label="静音" />
        <engine:VisualElement class="button-row">
            <engine:Button name="SaveButton" text="保存" />
            <engine:Button name="CancelButton" text="取消" />
        </engine:VisualElement>
    </engine:VisualElement>
</engine:UXML>
```

---

## 场景脚本模式

每个 Unity 场景对应一个 `SceneScriptBase` 子类：

```csharp
using UNIHper;
using UniRx;

public class SceneMainScript : SceneScriptBase
{
    private CompositeDisposable _disposables = new CompositeDisposable();

    private void Start()
    {
        // 显示主 UI
        Managements.UI.Show<MainMenuUI>();
        
        // 播放背景音乐
        Managements.Audio.PlayMusic("MainBGM");
        
        // 订阅事件
        Managements.Event.Register<GameStartEvent>(OnGameStart);
    }

    private void OnGameStart(GameStartEvent evt)
    {
        Debug.Log("游戏开始");
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
        Managements.Event.Unregister<GameStartEvent>(OnGameStart);
    }
}
```

命名约定：`Scene{场景名}Script.cs`，如 `SceneEntry.unity` → `SceneEntryScript.cs`

---

## UniRx 响应式编程

UNIHper 深度集成 UniRx，推荐使用响应式模式：

### 事件订阅

```csharp
using UniRx;

// Button 点击
button.OnClickAsObservable()
    .Subscribe(_ => DoSomething())
    .AddTo(this);

// Toggle 变化
toggle.OnValueChangedAsObservable()
    .Subscribe(isOn => OnToggleChanged(isOn))
    .AddTo(this);

// InputField 输入
inputField.OnValueChangedAsObservable()
    .Throttle(TimeSpan.FromMilliseconds(300))  // 防抖
    .Subscribe(text => OnSearch(text))
    .AddTo(this);
```

### 定时器

```csharp
// 延迟执行
Observable.Timer(TimeSpan.FromSeconds(1))
    .Subscribe(_ => DoAfterDelay())
    .AddTo(this);

// 循环执行
Observable.Interval(TimeSpan.FromSeconds(0.5f))
    .Subscribe(_ => UpdatePerHalfSecond())
    .AddTo(this);

// 帧更新
Observable.EveryUpdate()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Subscribe(_ => OnSpacePressed())
    .AddTo(this);
```

### CompositeDisposable 管理

```csharp
private CompositeDisposable _disposables = new CompositeDisposable();

void Start()
{
    someObservable.Subscribe(...).AddTo(_disposables);
}

void OnDestroy()
{
    _disposables.Dispose();  // 一次性清理所有订阅
}
```

---

## 命名约定

| 类型 | 命名规则 | 示例 |
|------|---------|------|
| UI 脚本 (UGUI) | `{功能}UI.cs` | `MainMenuUI.cs`, `SettingsUI.cs` |
| UI 脚本 (Toolkit) | `{功能}UI.cs` | `DashboardUI.cs` |
| 场景脚本 | `Scene{场景名}Script.cs` | `SceneEntryScript.cs` |
| 配置类 | `{功能}Config.cs` | `GameConfig.cs`, `AudioConfig.cs` |
| 事件类 | `{动作}Event.cs` | `GameStartEvent.cs`, `ScoreChangedEvent.cs` |
| UI 预制体 | 与脚本同名 | `MainMenuUI.prefab` |
| UXML 文件 | 与脚本同名 | `DashboardUI.uxml` |

---

## 常用代码片段

### UGUI 显示/隐藏

```csharp
Managements.UI.Show<GameUI>();
Managements.UI.Hide<GameUI>();
Managements.UI.Toggle<GameUI>();  // 切换显示状态
```

### UI Toolkit 显示/隐藏

```csharp
Managements.UIToolkit.Show<SettingsUI>();
Managements.UIToolkit.Hide<SettingsUI>();
```

### 带参数显示 UI

```csharp
Managements.UI.Show<ResultUI>(new ResultData { Score = 100 });

// ResultUI.cs
protected override void OnShow()
{
    var data = GetData<ResultData>();
    scoreText.text = data.Score.ToString();
}
```

### 场景切换

```csharp
Managements.Scene.LoadSceneAsync("GameScene", 
    progress => Debug.Log($"加载进度: {progress}"),
    () => Debug.Log("场景加载完成")
);
```

### 播放音效

```csharp
Managements.Audio.PlayEffect("ButtonClick");
Managements.Audio.PlayMusic("BGM_Main", volume: 0.8f, loop: true);
```

### 定时器使用

```csharp
// 延迟执行
Managements.Timer.Delay(2f, () => Debug.Log("2秒后执行"));

// 带 key 的延迟（可取消）
Managements.Timer.Delay(5f, () => Debug.Log("5秒后执行"), "myTimer");
Managements.Timer.Cancel("myTimer");  // 取消

// 循环执行
Managements.Timer.Interval(1f, () => Debug.Log("每秒执行"));

// 倒计时
var countdown = Managements.Timer.Countdown(10f);
countdown.OnTick.Subscribe(remaining => Debug.Log($"剩余: {remaining}s"));
countdown.OnCompleted.Subscribe(_ => Debug.Log("倒计时结束"));
countdown.Start();
```

### 事件系统

```csharp
// 定义事件
public class ScoreChangedEvent : UEvent
{
    public int NewScore;
}

// 注册事件
Managements.Event.Register<ScoreChangedEvent>(OnScoreChanged);

// 触发事件
Managements.Event.Fire(new ScoreChangedEvent { NewScore = 100 });

// 注销事件
Managements.Event.Unregister<ScoreChangedEvent>(OnScoreChanged);
```
