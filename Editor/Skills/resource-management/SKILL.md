---
name: resource-management
description: 'Manage resources using UNIHper ResourceManager. Use when asked to load assets, configure Addressable resources, set up resources.json, load external files like audio or images, or work with asset bundles.'
---

# 资源管理系统

## 资源驱动

| 驱动 | 说明 | 适用场景 |
|------|------|---------|
| `Resources` | Unity Resources 文件夹 | 小型资源 |
| `Addressable` | Unity Addressable 系统 | 大型资源、热更新 |
| `AssetBundle` | 传统 AB 包 | 兼容旧项目 |

## resources.json 配置

位置：`Assets/Resources/UNIHper/resources.json`

```json
{
  "Persistence": [
    { "driver": "Addressable", "type": "Object", "label": "default" },
    { "driver": "Addressable", "type": "Sprite", "label": "default" },
    { "driver": "Resources", "type": "GameObject", "path": "Prefabs/Common" }
  ],
  "SceneEntry": [
    { "driver": "Addressable", "type": "AudioClip", "label": "scene_entry" }
  ]
}
```

## 资源生命周期

| 生命周期 | 加载时机 | 卸载时机 |
|---------|---------|---------|
| `Persistence` | 游戏启动 | 游戏退出 |
| `Scene{Name}` | 进入场景 | 离开场景 |

## API

```csharp
// 同步获取
var prefab = Managements.Resource.Get<GameObject>("PlayerPrefab");
var sprite = Managements.Resource.Get<Sprite>("Icons/Star");

// 检查/筛选
bool exists = Managements.Resource.Exists<Sprite>("Icons/Star");
var icons = Managements.Resource.GetMany<Sprite>("Icons/");
var labeled = Managements.Resource.GetLabelAssets<Sprite>("ui_icons");

// 加载外部文件
Managements.Resource.AppendAudioClip("D:/Audio/bgm.wav")
    .Subscribe(clip => audioSource.clip = clip).AddTo(this);
Managements.Resource.AppendTexture2D("D:/Images/photo.png")
    .Subscribe(tex => rawImage.texture = tex).AddTo(this);

// AB 包
Managements.Resource.AppendAssetBundle("characters/hero");
Managements.Resource.UnloadAssetBundle("characters/hero");
```

## 重要提示

1. `Get<T>()` 是同步方法，仅获取已加载的资源
2. 外部文件加载返回 `IObservable`，需 `.Subscribe()` 和 `.AddTo()`
