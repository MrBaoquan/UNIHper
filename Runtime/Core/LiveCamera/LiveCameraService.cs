using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using RenderHeads.Media.AVProLiveCamera;

namespace UNIHper
{
    /// <summary>
    /// 摄像头服务配置
    /// </summary>
    [Serializable]
    public class LiveCameraConfig
    {
        /// <summary>
        /// 设备选择方式（默认按索引选择第一个摄像头）
        /// </summary>
        public AVProLiveCamera.SelectDeviceBy DeviceSelection = AVProLiveCamera.SelectDeviceBy.Index;

        /// <summary>
        /// 首选设备名称列表（按优先级排序）
        /// </summary>
        public List<string> PreferredDeviceNames = new List<string>
        {
            "Logitech BRIO",
            "Logitech HD Pro Webcam C922",
            "Logitech HD Pro Webcam C920",
            "HD Pro Webcam C922",
            "HD Pro Webcam C920",
            "Integrated Webcam"
        };

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
        public List<Vector2> PreferredResolutions = new List<Vector2>
        {
            new Vector2(1920, 1080),
            new Vector2(1280, 720),
            new Vector2(640, 480)
        };

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

        // Reactive Subjects
        private readonly BehaviorSubject<bool> _isRunningSubject = new BehaviorSubject<bool>(false);
        private readonly Subject<Texture> _textureReadySubject = new Subject<Texture>();
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

        #endregion

        #region Events (Observables)

        /// <summary>
        /// 运行状态变化事件
        /// </summary>
        public IObservable<bool> OnRunningStateChanged => _isRunningSubject.DistinctUntilChanged();

        /// <summary>
        /// 纹理就绪事件（摄像头启动后触发）
        /// </summary>
        public IObservable<Texture> OnTextureReady => _textureReadySubject.AsObservable();

        /// <summary>
        /// 帧更新事件
        /// </summary>
        public IObservable<Unit> OnFrameUpdated => _frameUpdatedSubject.AsObservable();

        #endregion

        #region Constructor

        private LiveCameraService()
        {
            _config = new LiveCameraConfig();
            Debug.Log("[LiveCameraService] 服务已创建");
        }

        /// <summary>
        /// 创建新实例（非单例）
        /// </summary>
        public static LiveCameraService Create()
        {
            return new LiveCameraService();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化摄像头服务
        /// </summary>
        /// <param name="config">可选配置</param>
        /// <returns>初始化是否成功</returns>
        public IObservable<bool> Initialize(LiveCameraConfig config = null)
        {
            if (config != null)
            {
                _config = config;
            }

            return Observable.Create<bool>(observer =>
            {
                try
                {
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

                Debug.Log("[LiveCameraService] 创建 AVProLiveCameraManager");
            }
        }

        private void CreateCameraObject()
        {
            if (_cameraObject != null)
            {
                GameObject.Destroy(_cameraObject);
            }

            _cameraObject = new GameObject("[LiveCamera]");
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
            _camera._desiredResolutions = new List<Vector2>(_config.PreferredResolutions);
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
                .Where(_ => _camera?.Device != null && _camera.Device.IsRunning)
                .Subscribe(_ =>
                {
                    _frameUpdatedSubject.OnNext(Unit.Default);

                    // 检查纹理是否就绪
                    if (OutputTexture != null && !_isRunningSubject.Value)
                    {
                        _isRunningSubject.OnNext(true);
                        _textureReadySubject.OnNext(OutputTexture);
                        Debug.Log($"[LiveCameraService] 摄像头已启动: {DeviceName} ({Width}x{Height}@{FrameRate:F1}fps)");
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
                _isRunningSubject.OnNext(false);
                Debug.Log("[LiveCameraService] 摄像头已停止");
            }
        }

        /// <summary>
        /// 重启摄像头
        /// </summary>
        public void RestartCamera()
        {
            StopCamera();

            Observable.Timer(TimeSpan.FromMilliseconds(500)).Subscribe(_ => StartCamera()).AddTo(_disposables);
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
        /// 切换到指定设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        public void SwitchDevice(string deviceName)
        {
            _config.DeviceSelection = AVProLiveCamera.SelectDeviceBy.Name;
            _config.PreferredDeviceNames = new List<string> { deviceName };

            RestartCamera();
        }

        /// <summary>
        /// 切换到指定设备（按索引）
        /// </summary>
        /// <param name="deviceIndex">设备索引</param>
        public void SwitchDevice(int deviceIndex)
        {
            _config.DeviceSelection = AVProLiveCamera.SelectDeviceBy.Index;
            _config.PreferredDeviceIndex = deviceIndex;

            ApplyConfig();
            RestartCamera();
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            StopCamera();
            _disposables.Dispose();

            if (_cameraObject != null)
            {
                GameObject.Destroy(_cameraObject);
                _cameraObject = null;
            }

            _isRunningSubject.OnCompleted();
            _isRunningSubject.Dispose();
            _textureReadySubject.OnCompleted();
            _textureReadySubject.Dispose();
            _frameUpdatedSubject.OnCompleted();
            _frameUpdatedSubject.Dispose();

            Debug.Log("[LiveCameraService] 服务已销毁");
        }

        #endregion
    }
}
