# UNIHper 框架使用指南

> 此文件由 AI 上下文生成器自动管理，用于向 AI 提供框架使用上下文

## 核心架构

UNIHper 是一个 Unity 应用开发框架，通过 `Managements` 门面类提供统一的服务访问。

### Managements 门面 API

```csharp
using UNIHper;

// UI 管理
Managements.UI.Show<MyUI>();                    // 显示 UI
Managements.UI.Hide<MyUI>();                    // 隐藏 UI
Managements.UI.Get<MyUI>();                     // 获取 UI 实例
Managements.UI.IsVisible<MyUI>();               // 检查是否可见

// 资源管理
Managements.Resource.Get<T>("resourceName");    // 同步获取资源
Managements.Resource.LoadAsync<T>("name");      // 异步加载（返回 IObservable）
Managements.Resource.Instantiate("prefabName"); // 实例化预制体

// 音频管理
Managements.Audio.PlayMusic("BGM");             // 播放背景音乐
Managements.Audio.PlaySound("Click");           // 播放音效
Managements.Audio.StopMusic();                  // 停止音乐
Managements.Audio.SetMusicVolume(0.8f);         // 设置音量

// 事件系统
Managements.Event.Fire(new MyEvent());          // 触发事件
Managements.Event.Register<MyEvent>(handler);   // 注册事件处理
Managements.Event.Unregister<MyEvent>(handler); // 注销事件处理

// 配置管理
Managements.Config.Get<GameConfig>();           // 获取配置实例
config.Save();                                  // 保存配置到文件

// 定时器
Managements.Timer.Delay(1f, callback);          // 延迟执行
Managements.Timer.Interval(0.5f, callback);     // 循环执行
Managements.Timer.Cancel(timerId);              // 取消定时器

// 场景管理
Managements.Scene.Load("SceneName");            // 加载场景
Managements.Scene.LoadAsync("SceneName");       // 异步加载场景
```

---

## UI 开发模式

### UIBase 基类

所有 UI 脚本继承自 `UIBase`，使用特性标注配置：

```csharp
using UNIHper;
using UniRx;

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
        Managements.Scene.Load("GameScene");
    }
}
```

### UIType 类型

| 类型 | 说明 |
|------|------|
| `UIType.Normal` | 普通页面，可叠加显示 |
| `UIType.Popup` | 弹窗，显示时暂停背后 UI |
| `UIType.Fixed` | 固定 UI，不参与页面栈管理 |

### Get<T>() 方法

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

## 配置系统

### UConfig 基类

配置类继承 `UConfig`，支持 XML/JSON/YAML 序列化：

```csharp
using UNIHper;
using System.Xml.Serialization;

[SerializedAt(AppPath.StreamingDir)]  // 存储位置
[SerializeWith(ConfigDriver.XML)]      // 序列化格式
public class GameConfig : UConfig
{
    // 简单属性
    public int MaxScore = 100;
    public float MusicVolume = 0.8f;
    public string PlayerName = "Player";

    // 复杂对象
    public DifficultySettings Difficulty = new DifficultySettings();

    // 不序列化的属性
    [XmlIgnore]
    public bool IsInitialized { get; set; }

    // 配置加载后调用
    protected override void OnLoaded()
    {
        // 验证和修正值
        if (MaxScore < 0) MaxScore = 100;
    }
}

public class DifficultySettings
{
    public int Level = 1;
    public float TimeLimit = 60f;
}
```

### 使用配置

```csharp
// 获取配置
var config = Managements.Config.Get<GameConfig>();

// 读取值
int maxScore = config.MaxScore;

// 修改并保存
config.MaxScore = 200;
config.Save();
```

配置文件位置：`StreamingAssets/Configs/{ConfigName}.xml`

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

## 资源加载

### resources.json 配置

资源在 `Assets/Resources/UNIHper/resources.json` 中按场景注册：

```json
{
  "Shared": {
    "driver": "Resources",
    "assets": ["Prefabs/Common", "Audio/BGM"]
  },
  "SceneMain": {
    "driver": "Addressable",
    "assets": ["UI/MainMenu", "Characters/Player"]
  }
}
```

### 代码加载

```csharp
// 同步加载（需已在 resources.json 注册）
var prefab = Managements.Resource.Get<GameObject>("PlayerPrefab");

// 异步加载
Managements.Resource.LoadAsync<Sprite>("Icons/Star")
    .Subscribe(sprite => image.sprite = sprite)
    .AddTo(this);

// 实例化预制体
var instance = Managements.Resource.Instantiate("EnemyPrefab");
instance.transform.position = spawnPoint;
```

---

## 命名约定

| 类型 | 命名规则 | 示例 |
|------|---------|------|
| UI 脚本 | `{功能}UI.cs` | `MainMenuUI.cs`, `SettingsUI.cs` |
| 场景脚本 | `Scene{场景名}Script.cs` | `SceneEntryScript.cs` |
| 配置类 | `{功能}Config.cs` | `GameConfig.cs`, `AudioConfig.cs` |
| 事件类 | `{动作}Event.cs` | `GameStartEvent.cs`, `ScoreChangedEvent.cs` |
| UI 预制体 | 与脚本同名 | `MainMenuUI.prefab` |

---

## 常用代码片段

### 显示/隐藏 UI

```csharp
Managements.UI.Show<GameUI>();
Managements.UI.Hide<GameUI>();
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
Managements.Scene.LoadAsync("GameScene")
    .Subscribe(_ => Debug.Log("场景加载完成"))
    .AddTo(this);
```

### 播放音效

```csharp
Managements.Audio.PlaySound("ButtonClick");
Managements.Audio.PlayMusic("BGM_Main", loop: true);
```
