# UNIHper 框架

### 核心功能模块

1. 资源管理
2. 场景管理
3. UI 管理
4. 事件管理
5. 网络模块 (支持 protobuf)
6. 串口模块
7. 配置系统
8. 编辑器拓展

### 核心插件集成

- DOTween
- Unirx
- Rx.net
- Google.Protobuf

## 安装

1. 下载插件包

```bash
# clone into your Assets directory
git clone https://github.com/MrBaoquan/UNIHper.git
```

2. 初始化项目

   - 菜单栏: UNIHper ==> Initialize

3. 开始开发
   - 创建 UI 脚本 UNIHper ==> Create ==> UIScript
   - 创建场景脚本 UNIHper ==> Create ==> SceneScript
   - 创建配置文件脚本 UNIHper ==> Create ==> ConfigScript

## Examples

### 场景管理

```c#
// 场景脚本 SceneGameScript.cs
using System;
using UnityEngine;
using UNIHper;

// 场景脚本生命周期
public class SceneGameScript : SceneScriptBase
{
    public override void Start(){}

    public override void Update(){}

    public override void OnDestroy(){}

    public override void OnApplicationQuit(){}
}
```

```c#
Managements.Scene.LoadSceneAsync("SceneGame",_progress=>{
    // lading progress..
},()=>{
    // load scene finshed...
});
```

### 资源管理

```c#
var _btnSmallSprite = Managements.Resource.Get<Sprite>("button_small");
var _monsterPrefab = Managements.Resource.Get<GameObject>("Monster");
```

### UI 管理

```c#
Managements.UI.Show<IdleUI>(_idleUI=>{
    // ...
});
Managements.UI.Hide<IdleUI>();
```

```c#
// IdleUI.cs
void Start()
{

    // button clicked event
    this.Get<Button>("btn_start")
        .OnClickAsObservable()
        .Subscribe(_=>{
            // Handle start game action ...
        });

    // replace sprite example
    var _imageBG = this.Get<Image>("img_background");
    _imageBG.sprite = Managements.Resource.Get<Sprite>("background");
}


```

### 网络管理

```c#
// 开启一个 TCP server
Managements.Network.BuildTcpListener("127.0.0.1", 6666, new StringMsgReceiver())
    .Listen()
    .OnReceived((_netMessage,_socket)=>{
        var _msg = _netMessage.Message as NetStringMessage;
        Debug.LogFormat("Received: {0}", _msg.Content);
    });

// 开启一个 UDP client
Managements.Network.BuildUdpClient("127.0.0.1", 7777, new StringMsgReceiver())
    .Listen()
    .OnReceived((_netMessage,_socket)=>{
        var _msg = _netMessage.Message as NetStringMessage;
        Debug.LogFormat("Received: {0}", _msg.Content);
    });

```

### 配置系统

```c#

// GameConfig.cs
using UNIHper;

public class GameConfig : UConfig
{
    public int PlayerCount = 1;
}
```

```c#
// application.cs
// get config instance
var _gameConfig = Managements.Config.Get<GameConfig>();

// serialize config
Managements.Config.Serialize<GameConfig>();

// serialize all
Managements.Config.SerializeAll();
```

### 事件管理

```c#
// RPCEvent.cs
class RPCEvent :UEvent{
    public int ID;
}
```

```c#
// application.cs
Action<RPCEvent> rpcEventHandler =(rpcEvent) =>{
    // handle event here...
};
// Register&Unregister event example
Managements.Event.Register<RPCEvent>(rpcEventHandler);
Managements.Event.Unregister<RPCEvent>(rpcEventHandler);
```

### 串口模块 (需要 x64 .net4.x 支持)

```c#
SerialPortManager.Instance.BuildConnect("COM2",115200,new SPStringLineReceiver())
    .Open()
    .OnReceive(_spMessage=>{
        var _message = _spMessage as SPLineMessage;
        Debug.Log(_message.Content);
    });
```
