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
-   UniRx
-   Rx.net
-   Google.Protobuf
-   gRPC
-   AVPro
-   Newtonsoft.Json
-   MessagePack

### 版本更新日志
v1.25.xxxx
- 资源加载优化，修复编辑器模式下加载外部图片偶发的异常
- UI单脚本多实例功能优化
- AnimatedUI相关功能优化
- LongTimeNoOperation增加禁用自动重置功能
- 增加TextureTransition过渡组件
- 增加RangeIndexer索引器
- 优化配置管理器/UI管理器
- 优化AVPro播放器

v1.25.811
- 优化UIManager,ResourceManager部分接口
- 优化LongTimeNoOperation组件
- 公开UI状态接口

v1.25.806
- 二进制序列化改用messagepack包
- SphereLayout优化
- Indexer优化，增加MinStepForValue接口, OnPrev & OnNext接口, 修复LastSet的bug
- AppConfig增加鼠标控制
- 增加TextureTransition过渡组件, 拓展出FadeTo接口
- UIManager 增加单个脚本多个实例功能
- Resource GetMany接口增加对外部文件全路径的支持
- 增加RangeIndexer索引器

v1.25.715
- 修复Linker文件不存在时的错误
- 动画拓展PlayThenNext功能
- 修复MultipleAVProPlayer 博放视频时 startTime精度问题导致的bug
- 增加动画隐藏播放时的等待任务逻辑

v1.25.617
- 拓展Texture相关接口
- 增加配置自定义文件名及其他优化项

v1.25.0414
- 增加link编辑器管理类
- 更新DNHper
  
v1.25.0409
- 更新GRPC模块依赖
- 优化AVPro且视频闪屏

v1.25.0327
- 增加页面隐藏时的动画重置逻辑
- 增加优化动画相关接口
  - PlayThenHide        播放指定动画，结束后自动隐藏
  - Seek/SeekToFrame    将动画跳转到指定帧/时间
  - Switch              切换指定动画
- 增加屏幕截图快捷脚本
- 视频&网络&动画优化 Texture实用拓展
  
v1.25.0121
- 优化AVPro模块
- 对模块配置程序集、UI、资源进行编辑器脚本拓展
- 增加Texture等功能性拓展及部分UGUI Image相关shader
- DOTweenText 拓展
- Animation 增加Seek/SeekToFrame/Play/Pause/Stop/Rewind等实用拓展接口
- 域重载优化，运行时按需重载程序集

v1.24.1117
 - 资源查找优先全字符匹配
 - 资源加载优化、修复编辑器下外部图片加载时的异常问题
 - 优化追加外部资源时，相对路径的转化问题
 - 优化网络模块
 - 优化调试触点的层级问题，低于调试窗口
  
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
  
