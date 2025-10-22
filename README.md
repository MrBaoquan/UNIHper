# UNIHper 框架文档

UNIHper是一个基于Unity的游戏开发框架，提供了完整的管理系统，包括UI管理、资源管理、音频管理、事件管理等核心功能。

## 快速开始

通过`Managements`类可以统一访问所有管理器：

```csharp
using UNIHper;

// 所有管理器都通过 Managements 统一访问
Managements.UI.Show<MyUI>();
Managements.Resource.Get<GameObject>("MyPrefab");
Managements.Audio.PlayMusic("BGM");
```

## 资源管理 (ResourceManager)

### 概述

ResourceManager 负责管理游戏中的所有资源，支持 Resources、AssetBundle 和 Addressables 三种资源加载方式。

### 基础用法

#### 获取资源

```csharp
// 获取单个资源
var gameObject = Managements.Resource.Get<GameObject>("MyPrefab");
var texture = Managements.Resource.Get<Texture2D>("MyTexture");
var audioClip = Managements.Resource.Get<AudioClip>("MyAudio");

// 获取多个资源（支持模糊匹配）
var textures = Managements.Resource.GetMany<Texture2D>("UI/Icons");
var prefabs = Managements.Resource.GetMany<GameObject>("Characters");
```

#### 检查资源是否存在

```csharp
bool exists = Managements.Resource.Exists<GameObject>("MyPrefab");
if (exists)
{
    var prefab = Managements.Resource.Get<GameObject>("MyPrefab");
}
```

#### 通过标签获取资源

```csharp
// 获取指定标签的所有资源
var uiTextures = Managements.Resource.GetLabelAssets<Texture2D>("UITextures");
var characterModels = Managements.Resource.GetLabelAssets<GameObject>("Characters");
```

### 资源配置管理

#### 添加配置文件

```csharp
// 动态添加资源配置文件
Managements.Resource.AddConfig("Config/NewResourceConfig");
```

#### AssetBundle 管理

```csharp
// 加载并添加 AssetBundle
var bundle = Managements.Resource.AppendAssetBundle("mybundle");
```

### 异步资源加载

ResourceManager 支持异步加载，返回 IObservable 对象：

#### 异步加载纹理

```csharp
// 加载单个纹理
Managements.Resource.AppendTexture2D("path/to/texture.png")
    .Subscribe(texture => {
        // 使用加载的纹理
        myImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
    });

// 批量加载纹理
var texturePaths = new[] { "tex1.png", "tex2.png", "tex3.png" };
Managements.Resource.AppendTexture2Ds(texturePaths)
    .Subscribe(textures => {
        // 使用加载的纹理列表
        for (int i = 0; i < textures.Count; i++)
        {
            // 处理每个纹理
        }
    });

// 从目录加载所有纹理
Managements.Resource.LoadTexture2Ds("Assets/UI/Textures", "*.png|*.jpg")
    .Subscribe(textures => {
        // 使用加载的纹理列表
    });
```

#### 异步加载音频

```csharp
// 加载单个音频文件
Managements.Resource.AppendAudioClip("Audio/BGM.wav")
    .Subscribe(audioClip => {
        Managements.Audio.PlayMusic(audioClip);
    });

// 批量加载音频
var audioPaths = new[] { "sfx1.wav", "sfx2.mp3", "bgm.wav" };
Managements.Resource.AppendAudioClips(audioPaths)
    .Subscribe(audioClips => {
        // 使用加载的音频列表
    });

// 从目录加载音频文件
Managements.Resource.AppendAudioClips("Assets/Audio/SFX", "*.wav|*.mp3")
    .Subscribe(audioClips => {
        // 处理加载的音频
    });
```

## UI管理 (UIManager)

### 概述

UIManager 是框架的核心UI管理组件，负责UI的创建、显示、隐藏、销毁和生命周期管理。

### UI 基类

所有 UI 组件都需要继承自 `UIBase` 类：

```csharp
using UNIHper.UI;

[UIPage(
    Asset = "MainMenuUI",           // UI资源名称
    Type = UIType.Normal,           // UI类型
    Order = 100,                    // 显示层级
    Canvas = "CanvasDefault",       // 渲染画布
    Scene = "Persistence"           // 所属场景
)]
public class MainMenuUI : UIBase
{
    protected override void OnCreate()
    {
        // UI创建时调用
    }

    protected override void OnShow()
    {
        // UI显示时调用
    }

    protected override void OnHide()
    {
        // UI隐藏时调用
    }

    protected override void OnDestroy()
    {
        // UI销毁时调用
    }
}
```

### UI 类型

框架支持三种UI类型：

- **UIType.Normal**: 普通UI，在场景中正常显示
- **UIType.Popup**: 弹窗UI，支持堆叠显示
- **UIType.Standalone**: 独立UI，不受其他UI影响

### 基础操作

#### 显示 UI

```csharp
// 显示指定类型的UI
var mainMenu = Managements.UI.Show<MainMenuUI>();
var settingsPanel = Managements.UI.Show<SettingsPanelUI>();

// 显示UI（如果已显示则不重复显示）
Managements.UI.Show<InventoryUI>();
```

#### 隐藏 UI

```csharp
// 隐藏指定UI
Managements.UI.Hide<MainMenuUI>();

// 隐藏所有UI
Managements.UI.HideAll();
```

#### 获取 UI 实例

```csharp
// 获取UI实例（不显示）
var inventoryUI = Managements.UI.Get<InventoryUI>();
if (inventoryUI != null)
{
    // 操作UI实例
}
```

#### 检查 UI 状态

```csharp
// 检查UI是否正在显示
bool isShowing = Managements.UI.IsShowing<MainMenuUI>();
if (isShowing)
{
    Debug.Log("主菜单正在显示");
}
```

#### 切换 UI 显示状态

```csharp
// 切换UI显示/隐藏状态
Managements.UI.Toggle<SettingsPanelUI>();
```

### 高级功能

#### Canvas 渲染模式设置

```csharp
// 设置默认Canvas的渲染模式
Managements.UI.SetRenderMode(RenderMode.ScreenSpaceOverlay);

// 设置指定Canvas的渲染模式
Managements.UI.SetRenderMode(RenderMode.WorldSpace, "GameCanvas");
```

#### UI 堆栈管理

```csharp
// 暂存当前活跃的所有UI
Managements.UI.StashActiveUI();

// 恢复之前暂存的UI
Managements.UI.PopStashedUI();
```

### UI 生命周期示例

```csharp
public class GameHUDUI : UIBase
{
    private IDisposable healthUpdateSubscription;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        // 初始化UI组件
        InitializeComponents();
    }

    protected override void OnShow()
    {
        base.OnShow();
        // 订阅游戏事件
        healthUpdateSubscription = Managements.Event.OnHealthChanged
            .Subscribe(health => UpdateHealthBar(health));
    }

    protected override void OnHide()
    {
        base.OnHide();
        // 取消订阅
        healthUpdateSubscription?.Dispose();
    }

    private void UpdateHealthBar(float health)
    {
        // 更新血条显示
    }
}
```

## 其他管理器

### 音频管理 (AudioManager)

```csharp
// 播放背景音乐
Managements.Audio.PlayMusic("MainTheme", volume: 0.8f, loop: true);

// 播放音效
Managements.Audio.PlayEffect("ButtonClick");

// 暂停/停止音乐
Managements.Audio.PauseMusic();
Managements.Audio.StopMusic();
```

### 场景管理 (SceneManager)

```csharp
// 异步加载场景
Managements.Scene.LoadSceneAsync("GameScene", 
    progress => Debug.Log($"加载进度: {progress * 100}%"),
    () => Debug.Log("场景加载完成"));

// 获取当前场景
var currentScene = Managements.Scene.Current;

// 监听场景加载事件
Managements.Scene.OnNewSceneLoadedAsObservable()
    .Subscribe(scene => Debug.Log($"新场景已加载: {scene.name}"));
```

### 事件管理 (EventManager)

```csharp
// 定义事件类
public class PlayerDeathEvent : UEvent
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
}

// 注册事件监听
Managements.Event.Register<PlayerDeathEvent>(OnPlayerDeath);

// 触发事件
Managements.Event.Fire(new PlayerDeathEvent 
{ 
    PlayerId = 1, 
    Position = transform.position 
});

// 取消注册
Managements.Event.Unregister<PlayerDeathEvent>(OnPlayerDeath);

private void OnPlayerDeath(PlayerDeathEvent eventData)
{
    Debug.Log($"玩家 {eventData.PlayerId} 在位置 {eventData.Position} 死亡");
}
```

### 定时器管理 (TimerManager)

```csharp
// 延迟执行
Managements.Timer.Delay(2.0f, () => {
    Debug.Log("2秒后执行");
});

// 异步延迟
await Managements.Timer.Delay(1.5f);

// 倒计时
var countdown = Managements.Timer.Countdown(10.0f, 1.0f);
countdown.OnTick += (remaining) => Debug.Log($"剩余时间: {remaining}");
countdown.OnComplete += () => Debug.Log("倒计时结束");

// 下一帧执行
Managements.Timer.NextFrame(() => {
    Debug.Log("下一帧执行");
});
```

### 配置管理 (ConfigManager)

```csharp
// 获取配置
var gameConfig = Managements.Config.Get<GameConfig>();

// 保存配置
Managements.Config.Save<UserSettings>();

// 重新加载配置
var reloadedConfig = Managements.Config.Reload<GameConfig>();

// 保存所有配置
Managements.Config.SaveAll();
```

## 最佳实践

### 1. 统一使用 Managements 访问

```csharp
// 推荐：使用 Managements 统一访问
Managements.UI.Show<MyUI>();
Managements.Resource.Get<GameObject>("MyPrefab");

// 不推荐：直接访问单例
UIManager.Instance.Show<MyUI>();
```

### 2. 合理使用生命周期

```csharp
public class MyUI : UIBase
{
    private IDisposable subscription;

    protected override void OnShow()
    {
        base.OnShow();
        // 在显示时订阅事件
        subscription = Managements.Event.OnSomeEvent
            .Subscribe(HandleEvent);
    }

    protected override void OnHide()
    {
        base.OnHide();
        // 在隐藏时取消订阅
        subscription?.Dispose();
    }
}
```

### 3. 异步资源加载

```csharp
// 推荐：使用异步加载避免卡顿
Managements.Resource.AppendTexture2D("LargeTexture.png")
    .Subscribe(texture => {
        // 加载完成后使用
        ApplyTexture(texture);
    });

// 不推荐：同步加载大资源
var texture = Managements.Resource.Get<Texture2D>("LargeTexture");
```

### 4. 错误处理

```csharp
// 安全的资源获取
var prefab = Managements.Resource.Get<GameObject>("MyPrefab");
if (prefab != null)
{
    var instance = Instantiate(prefab);
}
else
{
    Debug.LogWarning("预制体加载失败，使用默认预制体");
    var defaultPrefab = Managements.Resource.Get<GameObject>("DefaultPrefab");
    var instance = Instantiate(defaultPrefab);
}
```

## 总结

UNIHper 框架通过 `Managements` 类提供了统一、简洁的API接口，让开发者能够轻松管理游戏中的各种资源和组件。合理使用这些管理器可以大大提高开发效率，让代码更加清晰和易于维护。