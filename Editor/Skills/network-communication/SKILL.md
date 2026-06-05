---
name: network-communication
description: 'Build TCP clients/servers and UDP communication using UNIHper NetworkManager. Use when asked to implement socket networking, TCP connections with auto-reconnect, UDP broadcast, or message dispatching for exhibition projects.'
---

# 网络通信

通过 `Managements.Network` 访问。支持 TCP 客户端/服务端和 UDP 通信。

## TCP 客户端

```csharp
// 创建并连接（自动重连）
var client = Managements.Network.BuildTcpClient("192.168.1.100", 8080);
client.Connect();

// 发送数据
client.Send2Server("Hello");
client.Send2Server(byteArray);

// 检查连接状态
bool connected = client.Connected;

// 断开
client.Disconnect();
Managements.Network.CloseTcpClient("192.168.1.100_8080");

// 自定义消息接收器
var client = Managements.Network.BuildTcpClient("127.0.0.1", 9090, new StringMsgReceiver());
client.Connect();
```

## TCP 服务端

```csharp
// 创建并监听
var server = Managements.Network.BuildTcpListener("0.0.0.0", 6666);
server.SetBacklog(10).Listen();

// 发送给所有客户端
server.Send2Clients("broadcast message");
server.Send2Clients(byteArray);

// 发送给指定客户端（key = "IP_Port"）
server.Send2Client("hello", "192.168.1.50_12345");

// 关闭
Managements.Network.CloseTcpServer("0.0.0.0_6666");
```

## UDP 客户端（点对点）

```csharp
// 创建 UDP 客户端
var udpClient = Managements.Network.BuildUdpClient("192.168.1.100", 9090);
udpClient.Connect();

// 发送
Managements.Network.Send2UdpServer(byteArray);
Managements.Network.Send2UdpServer("message");
```

## UDP 监听/广播

```csharp
// 创建 UDP 监听器
var udpServer = Managements.Network.BuildUdpListener("0.0.0.0", 9090);
udpServer.EnableBroadcast().Listen();

// 发送到指定地址
Managements.Network.Send2UdpClient(byteArray, "192.168.1.255", 9090);

// 广播
Managements.Network.SendUdpBroadcast(byteArray, 9090);
```

## 网络事件

通过 UNIHper 事件系统接收连接/断开通知：

```csharp
Managements.Event.Register<UNetConnectedEvent>(evt => {
    Debug.Log($"已连接: {evt.RemoteIP}:{evt.RemotePort}");
});

Managements.Event.Register<UNetDisconnectedEvent>(evt => {
    Debug.Log($"已断开: {evt.RemoteIP}:{evt.RemotePort}");
});
```

## 消息接收器

内置 `StringMsgReceiver`（字符串消息）和 `ProtoMsgReceiver`（Protobuf 消息）：

```csharp
// 字符串消息
var receiver = new StringMsgReceiver();
var client = Managements.Network.BuildTcpClient("127.0.0.1", 8080, receiver);

// 通过事件接收消息
Managements.Event.Register<UNetStringMsgEvent>(evt => {
    Debug.Log($"收到消息: {evt.Message}");
});
```

## 重要提示

1. TCP 客户端默认启用自动重连（3秒间隔）
2. Key 格式为 `"{IP}_{Port}"`，用于管理多个连接
3. 所有网络事件通过 `Managements.Event` 全局派发
