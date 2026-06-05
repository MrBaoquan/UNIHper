# UI Toolkit 支持

UNIHper 框架现已支持 Unity UI Toolkit 系统。

## 快速开始

### 1. 创建 UI Toolkit 页面

```csharp
using UNIHper;
using UNIHper.UI;
using UnityEngine.UIElements;

[UIToolkitPage(Asset = "MainMenuUI")]
public class MainMenuUIToolkit : UIToolkitBase
{
    private Button _startButton;
    private Label _titleLabel;

    // UI 开始显示时调用
    protected override void OnShowing()
    {
        // 使用 Q<T>() 方法查询元素（类似 CSS 选择器）
        _startButton = Q<Button>("StartButton");
        _titleLabel = Q<Label>("TitleLabel");

        // 绑定事件
        _startButton.clicked += OnStartClicked;

        // 或使用便捷方法
        BindButton("SettingsButton", OnSettingsClicked);
        BindTextField("PlayerName", OnNameChanged);
        BindSlider("VolumeSlider", OnVolumeChanged);
    }

    // UI 完全显示后调用
    protected override void OnShown()
    {
        _titleLabel.text = "欢迎回来！";
    }

    // UI 开始隐藏时调用
    protected override void OnHiding()
    {
        // 清理事件
        _startButton.clicked -= OnStartClicked;
    }

    private void OnStartClicked()
    {
        Managements.UIToolkit.Hide<MainMenuUIToolkit>();
        // 加载游戏场景...
    }

    private void OnSettingsClicked() { }
    private void OnNameChanged(string name) { }
    private void OnVolumeChanged(float volume) { }
}
```

### 2. 显示/隐藏 UI

```csharp
// 显示 UI
Managements.UIToolkit.Show<MainMenuUIToolkit>();

// 隐藏 UI
Managements.UIToolkit.Hide<MainMenuUIToolkit>();

// 获取 UI 实例
var ui = Managements.UIToolkit.Get<MainMenuUIToolkit>();

// 检查是否正在显示
bool isShowing = Managements.UIToolkit.IsShowing<MainMenuUIToolkit>();

// 隐藏所有 UI
Managements.UIToolkit.HideAll();
```

### 3. 自定义动画

```csharp
using System.Threading;
using System.Threading.Tasks;

[UIToolkitPage(Asset = "PopupUI")]
public class PopupUIToolkit : UIToolkitBase
{
    public PopupUIToolkit()
    {
        ShowDuration = 0.3f;
        HideDuration = 0.2f;
    }

    // 重写显示动画 - 缩放进入
    protected override async Task HandleShowAnimation(CancellationToken cancellationToken)
    {
        await UIToolkitAnimations.ScaleIn(Root, ShowDuration, cancellationToken);
    }

    // 重写隐藏动画 - 缩放退出
    protected override async Task HandleHideAnimation(CancellationToken cancellationToken)
    {
        await UIToolkitAnimations.ScaleOut(Root, HideDuration, cancellationToken);
    }
}
```

### 4. 内置动画效果

`UIToolkitAnimations` 提供以下动画：

| 方法 | 说明 |
|------|------|
| `FadeIn` | 淡入 |
| `FadeOut` | 淡出 |
| `ScaleIn` | 缩放进入（带回弹） |
| `ScaleOut` | 缩放退出 |
| `SlideInFromBottom` | 从底部滑入 |
| `SlideOutToBottom` | 向底部滑出 |
| `SlideInFromRight` | 从右侧滑入 |

### 5. 监听 UI 事件

```csharp
// 监听所有 UI 显示事件
Managements.UIToolkit.OnUIShownAsObservable()
    .Subscribe(ui => Debug.Log($"UI 显示: {ui.Key}"));

// 监听特定类型 UI
Managements.UIToolkit.OnUIShownAsObservable<MainMenuUIToolkit>()
    .Subscribe(ui => Debug.Log("主菜单显示了"));
```

## 资源配置

### UXML 文件

将 `.uxml` 文件放入资源目录，并在 `resources.json` 中注册：

```json
{
  "Persistence": {
    "driver": "Addressable",
    "assets": ["UI/MainMenuUI.uxml", "UI/PopupUI.uxml"]
  }
}
```

### Panel Settings

可以在 `UIToolkitPage` 特性中指定 Panel Settings：

```csharp
[UIToolkitPage(Asset = "MainMenuUI", PanelSettings = "MyPanelSettings")]
public class MainMenuUIToolkit : UIToolkitBase { }
```

如果未指定，将使用默认的 Panel Settings（1920x1080 参考分辨率）。

## 与 UGUI 共存

UI Toolkit 和现有的 UGUI 系统可以同时使用：

```csharp
// UGUI
Managements.UI.Show<MainMenuUI>();

// UI Toolkit
Managements.UIToolkit.Show<SettingsUIToolkit>();
```

两个系统相互独立，互不干扰。

## 生命周期

| 回调 | 说明 |
|------|------|
| `OnLoaded()` | UI 加载完成（仅一次） |
| `OnShowing()` | UI 开始显示 |
| `OnShown()` | UI 完全显示 |
| `OnHiding()` | UI 开始隐藏 |
| `OnHidden()` | UI 完全隐藏 |

## 查询元素

```csharp
// 按名称查询
var button = Q<Button>("MyButton");

// 按 CSS 类名查询
var label = Q<Label>(className: "title-text");

// 查询所有匹配元素
var allButtons = QAll<Button>().ToList();

// 便捷绑定方法
BindButton("SubmitBtn", () => Debug.Log("提交"));
BindTextField("InputField", text => Debug.Log(text));
BindToggle("Checkbox", isOn => Debug.Log(isOn));
BindSlider("Volume", value => Debug.Log(value));
```
