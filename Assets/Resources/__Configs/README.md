## 资源管理配置项

> 在 resources.json 配置的资源，将会被框架进行管理

```json
{
    // 持久层资源，不会跟随场景切换而销毁
    "Persistence": [
        {
            "type": "Texture2D",
            "path": "Textures"
        }
    ],

    // SceneEntry.scene 需要加载的资源
    "SceneEntry": [
        {
            "driver": "Resources", // Resources 资源
            "type": "GameObject", //资源类型
            "path": "Prefabs/UI/SceneEntry" // 资源路径
        },
        {
            "driver": "AssetBundle", // AssetBundle资源
            "path": "main.bundle" // 资源路径 相对于 StreamingAssets/AssetBundles/
        },
        {
            "driver": "Addressable", // 可寻址资源
            "type": "Sprite", // 资源类型
            "label": "scene_entry_textures" // 资源标签
        }
    ]
}
```

## UI 配置项

```json
{
    // 持久层UI
    "Persistence": {
        "WatermarkUI": {
            "type": "Normal", // UI类型
            "script": "WatermarkUI", // 脚本名称
            "asset": "WatermarkUI", // 资源名称
            "canvas": "CanvasDefault" // 指定 Canvas 结点
        }
    },

    "SceneEntry": {
        "IdleUI": {} // 默认type为Normal, asset为IdleUI, script为IdleUI
    },

    "SceneHall": {
        "HallUI": {}
    },

    "SceneGame": {
        "GameUI": {}
    }
}
```
