# UNIHper 框架

## [点击查看文档](https://parful.gitbook.io/unihper-docs)

### 核心功能模块

1. 资源管理
2. 场景管理
3. UI 管理
4. 事件管理
5. 网络模块
6. 串口模块
7. 配置系统
8. 编辑器拓展

### 核心插件集成

-   DOTween
-   Unirx
-   Rx.net
-   Google.Protobuf
-   gRPC
-   AVPro


### 版本更新日志
v1.24.1010
 - Indexer 增加LastSet变量
 - 更新Odin Inspector
 - 消除新工程警告

v1.24.926
 - Resource增加GetMany接口
 - SequenceFramePlayer增加多个控制项及事件
 - Indexer优化部分接口名称
 - 提炼UIMgr、ResMgr、CfgMgr、TimerMgr、EventMgr、AudioMgr、SceneMgr等静态接口
 - 优化初始化流程
 - 优化分辨率设置逻辑
 - 优化调试模式按钮的点击穿透逻辑
  
v1.24.912
 - 修复TapEffect小白点bug
 - GhostManager与UNIArt同步更新
  
v1.24.909
 - Managements.Audio 增加获取AudioPlayer、AudioSource相关接口
 - string.ToHexString 拓展增加自定义分隔符
 - F12 调整分辨率 (调试界面分辩率与实际分辨率自动调整)
 - 修复 中文名.exe WINAPI 设置窗口样式无效bug
 - this.Get(path)  支持直接根据对象名称查找物体

v1.24.817
 - 优化UIPage，增加Order属性
 - 优化日志文件格式，日志归档启动时执行
 - 优化设置项默认选项
 - 优化Addressable右键菜单子目录验证

v1.24.815
 - 将所有插件包含的示例移至框架示例文件夹中
 - 增加UI Particle插件
 - 增加滑动视觉反馈PanEffect
 - 增加UI页面代码注册机制
 - 工作流可以默认不使用GameMain程序集
 - Timer管理类增加Countdown功能
  
v1.24.809
 - 更新Fingers & MPUIKIT
 - 优化工程初始化工作流
 - 修复MPUIKIT导致的初始工程报错
 - 兼容Unity2023.1版本

v1.24.802
 - Unity日志调整输出至Logs文件夹
 - 优化Workflow,增加 Clean Excluded Files 菜单
  
v1.24.728
- 增加SVN仓库实用选项

v1.24.726
- ConfigManager增加调试日志
- AssemblyConfig移除无用引用导致的打包问题
- SerialPort独立出来，仅对Windows平台生效
- 增加使用shader
- Enable Odin Editor Only Mode
- 移除System.Reactive&Linq自动引用
- 增加Clean Temporary Files菜单
- 修复Network消息空字符导致的字符串校验失败的问题

v1.24.719
- 增加UNIHper/Documention菜单
- 修复debugTrigger在非Overlay模式下的bug
- Config脚本 各平台保存位置策略调整

v1.24.717
- 更新默认选项
- 兼容性测试

v1.24.716
- 兼容Unity .NET Framework
- 消除2023.1版本API警告

v1.24.712
- 修复配置文件系统bug
- 更新dotween

v1.24.621
- UI增加LifeCycleDisposables变量, 跟随UI显示/隐藏自动释放
- 修复与Sentis包Google.ProtoBuf包冲突问题

v1.24.614
- 资源可通过路径获取，系统内资源可重名
- 调试逻辑拓展到windows平台
- UInput_Slider组件优化

v1.24.607
- 移除AVPro Player极少可能用到的IOS TV平台依赖库
- 移除 Modern UI Pack 组件
- 调整Plugins层次结构

v1.24.601
- 修复LongTimeNoOperation安卓平台bug
- 拓展Linq至DNHper
- 增加Managements.Framework框架实用接口
- 增加屏幕右上角三连击事件，启用/禁用调试模式

v1.24.529
- 工作流默认自动重载域
- 优化项目名自动化逻辑

v1.24.525
- 修复GhostManager.cs文件NonBuiltInComponents接口bug
- 兼容Unity2023
  