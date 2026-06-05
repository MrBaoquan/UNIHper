using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UniRx;
using RenderHeads.Media.AVProLiveCamera;

namespace UNIHper
{
    /// <summary>
    /// 摄像头服务持久化配置（继承 UConfig，由 ConfigManager 自动管理序列化）
    /// <para>配置文件默认保存在 StreamingAssets/Configs/LiveCameraConfig.xml</para>
    /// </summary>
    [SerializedAt(AppPath.StreamingDir)]
    public class LiveCameraConfig : UConfig
    {
        /// <summary>
        /// 设备选择方式（默认按索引选择第一个摄像头）
        /// </summary>
        public AVProLiveCamera.SelectDeviceBy DeviceSelection = AVProLiveCamera.SelectDeviceBy.Index;

        /// <summary>
        /// 首选设备名称列表（按优先级排序）
        /// </summary>
        [XmlArray("PreferredDeviceNames")]
        [XmlArrayItem("Name")]
        public List<string> PreferredDeviceNames = new List<string>();

        /// <summary>
        /// 首选设备索引
        /// </summary>
        public int PreferredDeviceIndex = 0;

        /// <summary>
        /// 分辨率选择方式
        /// </summary>
        public AVProLiveCamera.SelectModeBy ModeSelection = AVProLiveCamera.SelectModeBy.Resolution;

        /// <summary>
        /// 首选分辨率列表（按优先级排序）
        /// </summary>
        [XmlArray("PreferredResolutions")]
        [XmlArrayItem("Resolution")]
        public List<SerializableVector2> PreferredResolutions = new List<SerializableVector2>();

        /// <summary>
        /// 期望帧率（0 表示自动）
        /// </summary>
        public float DesiredFrameRate = 30f;

        /// <summary>
        /// 是否水平翻转
        /// </summary>
        public bool FlipX = false;

        /// <summary>
        /// 是否垂直翻转
        /// </summary>
        public bool FlipY = false;

        /// <summary>
        /// 是否自动开始
        /// </summary>
        public bool PlayOnStart = true;

        /// <summary>
        /// 首次加载 / XML 文件不存在时填充默认值
        /// </summary>
        protected override void OnLoaded()
        {
            if (PreferredDeviceNames == null || PreferredDeviceNames.Count == 0)
            {
                PreferredDeviceNames = new List<string>
                {
                    "Logitech BRIO",
                    "Logitech HD Pro Webcam C922",
                    "Logitech HD Pro Webcam C920",
                    "HD Pro Webcam C922",
                    "HD Pro Webcam C920",
                    "Integrated Webcam"
                };
            }

            if (PreferredResolutions == null || PreferredResolutions.Count == 0)
            {
                PreferredResolutions = new List<SerializableVector2>
                {
                    new SerializableVector2(1920, 1080),
                    new SerializableVector2(1280, 720),
                    new SerializableVector2(640, 480)
                };
            }
        }

        protected override string Comment()
        {
            return "LiveCameraConfig - 摄像头服务配置（自动生成，可手动编辑）";
        }
    }

    /// <summary>
    /// 实时摄像头服务（单例）
    /// 封装 AVProLiveCamera，提供简化的摄像头访问接口
    /// </summary>
    public sealed class LiveCameraService : IDisposable
    {
        #region Singleton

        private static LiveCameraService _instance;
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, LiveCameraService> _namedInstances = new Dictionary<string, LiveCameraService>();

        /// <summary>
        /// 获取 LiveCameraService 单例实例
        /// </summary>
        public static LiveCameraService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LiveCameraService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 单例是否已创建
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// 销毁单例实例
        /// </summary>
        public static void DestroyInstance()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        #endregion

        #region Private Fields

        private AVProLiveCameraManager _manager;
        private AVProLiveCamera _camera;
        private GameObject _cameraObject;
        private LiveCameraConfig _config;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private string _instanceKey;

        // 纹理更新暂停标志
        private bool _isPaused = false;

        // 用于取消上一次 RestartCamera 的延迟启动，防止多次切换设备导致重叠重启
        private readonly SerialDisposable _restartDisposable = new SerialDisposable();
        private bool _isSwitching = false;

        // Reactive Subjects
        private readonly BehaviorSubject<bool> _isRunningSubject = new BehaviorSubject<bool>(false);

        // BehaviorSubject 保证晚订阅的调用方也能立即收到已就绪的纹理
        private readonly BehaviorSubject<Texture> _textureReadySubject = new BehaviorSubject<Texture>(null);
        private readonly Subject<Unit> _frameUpdatedSubject = new Subject<Unit>();

        #endregion

        #region Public Properties

        /// <summary>
        /// 摄像头是否正在运行
        /// </summary>
        public bool IsRunning => _camera?.Device?.IsRunning ?? false;

        /// <summary>
        /// 摄像头输出纹理
        /// </summary>
        public Texture OutputTexture => _camera?.OutputTexture;

        /// <summary>
        /// 当前设备名称
        /// </summary>
        public string DeviceName => _camera?.Device?.Name ?? "";

        /// <summary>
        /// 当前分辨率宽度
        /// </summary>
        public int Width => _camera?.Device?.CurrentWidth ?? 0;

        /// <summary>
        /// 当前分辨率高度
        /// </summary>
        public int Height => _camera?.Device?.CurrentHeight ?? 0;

        /// <summary>
        /// 当前帧率
        /// </summary>
        public float FrameRate => _camera?.Device?.CurrentFrameRate ?? 0;

        /// <summary>
        /// 可用设备数量
        /// </summary>
        public int DeviceCount => _manager?.NumDevices ?? 0;

        /// <summary>
        /// 配置
        /// </summary>
        public LiveCameraConfig Config => _config;

        /// <summary>
        /// 纹理更新是否已暂停（摄像头底层继续采集，仅冻结向订阅者推送的纹理帧）
        /// </summary>
        public bool IsPaused => _isPaused;

        #endregion

        #region Events (Observables)

        /// <summary>
        /// 运行状态变化事件
        /// </summary>
        public IObservable<bool> OnRunningStateChanged => _isRunningSubject.DistinctUntilChanged();

        /// <summary>
        /// 纹理就绪事件（摄像头启动后触发；晚订阅时若已就绪则立即推送）
        /// </summary>
        public IObservable<Texture> OnTextureReady => _textureReadySubject.Where(t => t != null).AsObservable();

        /// <summary>
        /// 帧更新事件
        /// </summary>
        public IObservable<Unit> OnFrameUpdated => _frameUpdatedSubject.AsObservable();

        /// <summary>
        /// 实时纹理流：每帧推送当前 OutputTexture（摄像头运行期间持续有效）
        /// </summary>
        public IObservable<Texture> TextureStream => _frameUpdatedSubject.Where(_ => OutputTexture != null).Select(_ => OutputTexture);

        #endregion

        #region Constructor

        private LiveCameraService(string key = null)
        {
            _instanceKey = key;
            // _config 由 Initialize() 从 ConfigManager 加载或外部传入，此处不再 new
            Debug.Log($"[LiveCameraService{(key != null ? $"({key})" : "")}] 服务已创建");
        }

        /// <summary>
        /// 创建独立实例（非单例、非命名管理）
        /// </summary>
        public static LiveCameraService Create() => new LiveCameraService();

        /// <summary>
        /// 按 key 获取已存在的命名实例，不存在则返回 null
        /// </summary>
        public static LiveCameraService Get(string key)
        {
            lock (_lock)
            {
                _namedInstances.TryGetValue(key, out var inst);
                return inst;
            }
        }

        /// <summary>
        /// 按 key 获取或创建命名实例，支持多摄像头并行场景
        /// <para>示例：LiveCameraService.GetOrCreate("cam0") / GetOrCreate("cam1")</para>
        /// </summary>
        public static LiveCameraService GetOrCreate(string key)
        {
            lock (_lock)
            {
                if (!_namedInstances.TryGetValue(key, out var inst) || inst._isDisposed)
                {
                    inst = new LiveCameraService(key);
                    _namedInstances[key] = inst;
                }
                return inst;
            }
        }

        /// <summary>
        /// 销毁指定 key 的命名实例
        /// </summary>
        public static void DestroyInstance(string key)
        {
            lock (_lock)
            {
                if (_namedInstances.TryGetValue(key, out var inst))
                {
                    inst.Dispose();
                    // Dispose 内部会调用 _namedInstances.Remove，此处无需重复
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化摄像头服务
        /// </summary>
        /// <param name="config">可选配置，为 null 时自动读取 ConfigManager 中持久化的 LiveCameraConfig</param>
        /// <returns>初始化是否成功</returns>
        public IObservable<bool> Initialize(LiveCameraConfig config = null)
        {
            // 未传入配置 → 读取 ConfigManager 管理的持久化实例
            _config = config ?? Managements.Config.Get<LiveCameraConfig>();

            return Observable.Create<bool>(observer =>
            {
                try
                {
                    // 清理旧的订阅（防止重复 Initialize 导致 EveryUpdate 泄漏）
                    _disposables.Clear();

                    // 确保 Manager 存在
                    EnsureManager();

                    // 创建摄像头对象
                    CreateCameraObject();

                    // 应用配置
                    ApplyConfig();

                    // 如果配置了自动开始，则开始
                    if (_config.PlayOnStart)
                    {
                        StartCamera();
                    }

                    // 启动帧更新监听
                    StartFrameUpdateMonitor();

                    observer.OnNext(true);
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LiveCameraService] 初始化失败: {ex.Message}");
                    observer.OnNext(false);
                    observer.OnCompleted();
                }

                return Disposable.Empty;
            });
        }

        private void EnsureManager()
        {
            _manager = GameObject.FindObjectOfType<AVProLiveCameraManager>();
            if (_manager == null)
            {
                var managerGO = new GameObject("[AVProLiveCameraManager]");
                GameObject.DontDestroyOnLoad(managerGO);
                _manager = managerGO.AddComponent<AVProLiveCameraManager>();

                // 设置 Shader 引用（使用正确的 Hidden/ 前缀）
                _manager._shaderBGRA32 = Shader.Find("Hidden/AVProLiveCamera/CompositeBGRA_2_RGBA");
                _manager._shaderMONO8 = Shader.Find("Hidden/AVProLiveCamera/CompositeMono8_2_RGBA");
                _manager._shaderYUY2 = Shader.Find("Hidden/AVProLiveCamera/CompositeYUY2_2_RGBA");
                _manager._shaderUYVY = Shader.Find("Hidden/AVProLiveCamera/CompositeUYVY_2_RGBA");
                _manager._shaderYVYU = Shader.Find("Hidden/AVProLiveCamera/CompositeYVYU_2_RGBA");
                _manager._shaderHDYC = Shader.Find("Hidden/AVProLiveCamera/CompositeHDYC_2_RGBA");
                _manager._shaderI420 = Shader.Find("Hidden/AVProLiveCamera/CompositeYUV_I420");
                _manager._shaderYV12 = Shader.Find("Hidden/AVProLiveCamera/CompositeYUV_YV12");
                _manager._shaderDeinterlace = Shader.Find("Hidden/AVProLiveCamera/Deinterlace");

                // 打包后 Hidden/ 前缀的 Shader 可能被 Unity 剥离，检测并给出明确提示
                if (_manager._shaderBGRA32 == null || _manager._shaderYUY2 == null || _manager._shaderMONO8 == null)
                {
                    Debug.LogError(
                        "[LiveCameraService] AVProLiveCamera 转换 Shader 未找到！"
                            + "\n请在 Unity 编辑器中执行菜单: UNIHper > LiveCamera > Include Shaders in Build"
                            + "\n或确认 Project Settings > Graphics > Always Included Shaders 中包含 AVProLiveCamera 的 Hidden Shader。"
                    );
                }

                Debug.Log("[LiveCameraService] 创建 AVProLiveCameraManager");
            }
        }

        private void CreateCameraObject()
        {
            if (_cameraObject != null)
            {
                GameObject.Destroy(_cameraObject);
            }

            _cameraObject = new GameObject(_instanceKey != null ? $"[LiveCamera:{_instanceKey}]" : "[LiveCamera]");
            GameObject.DontDestroyOnLoad(_cameraObject);
            _camera = _cameraObject.AddComponent<AVProLiveCamera>();
            _camera._playOnStart = false; // 我们手动控制

            Debug.Log("[LiveCameraService] 创建摄像头对象");
        }

        private void ApplyConfig()
        {
            if (_camera == null)
                return;

            // 设备选择
            _camera._deviceSelection = _config.DeviceSelection;
            _camera._desiredDeviceNames = new List<string>(_config.PreferredDeviceNames);
            _camera._desiredDeviceIndex = _config.PreferredDeviceIndex;

            // 分辨率选择
            _camera._modeSelection = _config.ModeSelection;
            _camera._desiredResolutions = _config.PreferredResolutions.ConvertAll(r => (Vector2)r);
            _camera._desiredFrameRate = _config.DesiredFrameRate;
            _camera._desiredAnyResolution = _config.ModeSelection == AVProLiveCamera.SelectModeBy.Default;

            // 显示设置
            _camera._flipX = _config.FlipX;
            _camera._flipY = _config.FlipY;

            Debug.Log($"[LiveCameraService] 配置已应用: 设备={_config.DeviceSelection}, 分辨率={_config.ModeSelection}");
        }

        private void StartFrameUpdateMonitor()
        {
            Observable
                .EveryUpdate()
                .Where(_ => _camera?.Device != null)
                .Subscribe(_ =>
                {
                    var deviceRunning = _camera.Device.IsRunning;
                    var subjectValue = _isRunningSubject.Value;

                    if (deviceRunning)
                    {
                        if (!_isPaused)
                        {
                            _frameUpdatedSubject.OnNext(Unit.Default);
                        }

                        // 设备正在运行且 Subject 尚未同步 → 纹理就绪（暂停状态下也要完成初始就绪通知）
                        if (OutputTexture != null && !subjectValue)
                        {
                            _isRunningSubject.OnNext(true);
                            if (!_isPaused)
                            {
                                _textureReadySubject.OnNext(OutputTexture);
                            }
                            Debug.Log($"[LiveCameraService] 摄像头已启动: {DeviceName} ({Width}x{Height}@{FrameRate:F1}fps)");
                        }
                    }
                    else if (subjectValue)
                    {
                        // 设备已停止但 Subject 仍为 true → 同步状态
                        _isRunningSubject.OnNext(false);
                        Debug.Log("[LiveCameraService] 检测到摄像头已外部停止");
                    }
                })
                .AddTo(_disposables);
        }

        #endregion

        #region Camera Control

        /// <summary>
        /// 启动摄像头
        /// </summary>
        public void StartCamera()
        {
            if (_camera == null)
            {
                Debug.LogWarning("[LiveCameraService] 请先调用 Initialize()");
                return;
            }

            _camera.Begin();
            Debug.Log("[LiveCameraService] 正在启动摄像头...");
        }

        /// <summary>
        /// 停止摄像头
        /// </summary>
        public void StopCamera()
        {
            if (_camera?.Device != null)
            {
                _camera.Device.Close();
                _isPaused = false; // 停止时重置暂停标志，避免重启后仍处于冻结状态
                _isRunningSubject.OnNext(false);
                _textureReadySubject.OnNext(null); // 清除已就绪标记，避免晚订阅者收到过期纹理
                Debug.Log("[LiveCameraService] 摄像头已停止");
            }
        }

        /// <summary>
        /// 重启摄像头（取消之前未完成的重启序列）
        /// </summary>
        public void RestartCamera()
        {
            StopCamera();

            // 用 SerialDisposable 确保上一次延迟启动被取消，防止重叠重启
            _restartDisposable.Disposable = Observable
                .EveryUpdate()
                .Select(_ => _camera?.Device == null || !_camera.Device.IsRunning)
                .Where(closed => closed)
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(2))
                .CatchIgnore()
                .Subscribe(_ => StartCamera());
        }

        /// <summary>
        /// 设置水平翻转
        /// </summary>
        public void SetFlipX(bool flip)
        {
            _config.FlipX = flip;
            if (_camera != null)
            {
                _camera._flipX = flip;
            }
        }

        /// <summary>
        /// 设置垂直翻转
        /// </summary>
        public void SetFlipY(bool flip)
        {
            _config.FlipY = flip;
            if (_camera != null)
            {
                _camera._flipY = flip;
            }
        }

        /// <summary>
        /// 暂停摄像头帧抓取（从源头禁止 AVProLiveCamera 组件 Update，输出纹理内容就地冻结）
        /// </summary>
        public void PauseTextureUpdate()
        {
            if (_isPaused)
                return;
            _isPaused = true;
            if (_camera != null)
                _camera.enabled = false; // 禁止组件 Update，母线驱动停止写入 OutputTexture
            Debug.Log("[LiveCameraService] 摄像头帧抓取已暂停");
        }

        /// <summary>
        /// 恢复摄像头帧抓取
        /// </summary>
        public void ResumeTextureUpdate()
        {
            if (!_isPaused)
                return;
            _isPaused = false;
            if (_camera != null)
                _camera.enabled = true; // 重新开启 Update，正常采集恢复
            // 恢复后推送一次当前纹理，让 BehaviorSubject 同步最新状态
            if (OutputTexture != null)
            {
                _textureReadySubject.OnNext(OutputTexture);
            }
            Debug.Log("[LiveCameraService] 摄像头帧抓取已恢复");
        }

        #endregion

        #region Texture Access

        /// <summary>
        /// 同步获取当前摄像头原始纹理，未运行时返回 null
        /// </summary>
        public Texture GetTexture() => OutputTexture;

        /// <summary>
        /// 异步等待纹理就绪后返回（已就绪时立即推送，否则等待下次就绪事件）
        /// </summary>
        public IObservable<Texture> WaitForTexture() =>
            IsRunning && OutputTexture != null ? Observable.Return(OutputTexture) : OnTextureReady.Take(1);

        #endregion

        #region Capture

        /// <summary>
        /// 截取当前帧为 Texture2D
        /// </summary>
        /// <returns>截取的纹理（调用方需要负责销毁）</returns>
        public Texture2D CaptureFrame()
        {
            var sourceTexture = OutputTexture;
            if (sourceTexture == null)
            {
                Debug.LogWarning("[LiveCameraService] 无可用纹理");
                return null;
            }

            // 创建 RenderTexture 来读取
            var rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(sourceTexture, rt);

            // 读取到 Texture2D
            var prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            var texture2D = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            texture2D.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            Debug.Log($"[LiveCameraService] 截取帧: {texture2D.width}x{texture2D.height}");
            return texture2D;
        }

        /// <summary>
        /// 截取当前帧并保存为 PNG
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否成功</returns>
        public bool CaptureAndSave(string filePath)
        {
            var texture = CaptureFrame();
            if (texture == null)
                return false;

            try
            {
                var bytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log($"[LiveCameraService] 图片已保存: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LiveCameraService] 保存失败: {ex.Message}");
                return false;
            }
            finally
            {
                GameObject.Destroy(texture);
            }
        }

        #endregion

        #region Device Info

        /// <summary>
        /// 获取所有可用设备名称
        /// </summary>
        public List<string> GetAvailableDevices()
        {
            var devices = new List<string>();
            if (_manager == null)
                return devices;

            for (int i = 0; i < _manager.NumDevices; i++)
            {
                var device = _manager.GetDevice(i);
                if (device != null)
                {
                    devices.Add(device.Name);
                }
            }

            return devices;
        }

        /// <summary>
        /// 切换到指定设备（自带防抖，连续调用时仅最后一次生效）
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        public void SwitchDevice(string deviceName)
        {
            if (_isSwitching)
            {
                Debug.LogWarning($"[LiveCameraService] 设备切换进行中，忽略重复请求: {deviceName}");
                return;
            }
            _isSwitching = true;

            _config.DeviceSelection = AVProLiveCamera.SelectDeviceBy.Name;
            _config.PreferredDeviceNames = new List<string> { deviceName };

            ApplyConfig();
            StopCamera();

            // 取消之前的重启序列，等设备关闭后再启动
            _restartDisposable.Disposable = Observable
                .EveryUpdate()
                .Select(_ => _camera?.Device == null || !_camera.Device.IsRunning)
                .Where(closed => closed)
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(2))
                .CatchIgnore()
                .Subscribe(
                    _ =>
                    {
                        StartCamera();
                        _isSwitching = false;
                    },
                    _ => _isSwitching = false
                );
        }

        /// <summary>
        /// 切换到指定设备（按索引，自带防抖）
        /// </summary>
        /// <param name="deviceIndex">设备索引</param>
        public void SwitchDevice(int deviceIndex)
        {
            if (_isSwitching)
            {
                Debug.LogWarning($"[LiveCameraService] 设备切换进行中，忽略重复请求: index={deviceIndex}");
                return;
            }
            _isSwitching = true;

            _config.DeviceSelection = AVProLiveCamera.SelectDeviceBy.Index;
            _config.PreferredDeviceIndex = deviceIndex;

            ApplyConfig();
            StopCamera();

            _restartDisposable.Disposable = Observable
                .EveryUpdate()
                .Select(_ => _camera?.Device == null || !_camera.Device.IsRunning)
                .Where(closed => closed)
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(2))
                .CatchIgnore()
                .Subscribe(
                    _ =>
                    {
                        StartCamera();
                        _isSwitching = false;
                    },
                    _ => _isSwitching = false
                );
        }

        #endregion

        #region Cleanup

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // 从命名实例字典中移除（命名实例才有 key）
            if (_instanceKey != null)
            {
                lock (_lock)
                {
                    _namedInstances.Remove(_instanceKey);
                }
            }

            // 1. 停止摄像头
            StopCamera();

            // 1.5 取消未完成的重启/切换
            _restartDisposable.Dispose();
            _isSwitching = false;

            // 2. 先清理订阅（EveryUpdate 观察者引用了 _camera，必须先停）
            _disposables.Dispose();

            // 3. 完成并释放 Subjects（通知所有订阅者流已结束）
            _isRunningSubject.OnCompleted();
            _isRunningSubject.Dispose();
            _textureReadySubject.OnCompleted();
            _textureReadySubject.Dispose();
            _frameUpdatedSubject.OnCompleted();
            _frameUpdatedSubject.Dispose();

            // 4. 最后销毁 GameObject（此时已无观察者引用它）
            if (_cameraObject != null)
            {
                GameObject.Destroy(_cameraObject);
                _cameraObject = null;
            }

            Debug.Log("[LiveCameraService] 服务已销毁");
        }

        #endregion
    }
}
