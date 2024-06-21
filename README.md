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
  
