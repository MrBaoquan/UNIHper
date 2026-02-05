using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace UNIHper
{
    /// <summary>
    /// 实时摄像头 UI 显示组件
    /// 将 LiveCameraService 的画面显示到 RawImage 上
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("UNIHper/Live Camera Display")]
    public class LiveCameraDisplay : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField]
        private bool _autoStart = true;

        [SerializeField]
        private bool _maintainAspectRatio = true;

        [Header("配置")]
        [SerializeField]
        private int _preferredWidth = 1920;

        [SerializeField]
        private int _preferredHeight = 1080;

        [SerializeField]
        private float _preferredFrameRate = 30f;

        [SerializeField]
        private bool _flipX = false;

        [SerializeField]
        private bool _flipY = false;

        private RawImage _rawImage;
        private AspectRatioFitter _aspectRatioFitter;
        private CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// 摄像头是否正在运行
        /// </summary>
        public bool IsRunning => LiveCameraService.Instance.IsRunning;

        /// <summary>
        /// 输出纹理
        /// </summary>
        public Texture OutputTexture => LiveCameraService.Instance.OutputTexture;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            if (_maintainAspectRatio)
            {
                _aspectRatioFitter = GetComponent<AspectRatioFitter>();
                if (_aspectRatioFitter == null)
                {
                    _aspectRatioFitter = gameObject.AddComponent<AspectRatioFitter>();
                    _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                }
            }
        }

        private void Start()
        {
            if (_autoStart)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 初始化并启动摄像头
        /// </summary>
        public void Initialize()
        {
            var config = new LiveCameraConfig
            {
                ModeSelection = RenderHeads.Media.AVProLiveCamera.AVProLiveCamera.SelectModeBy.Resolution,
                PreferredResolutions = new System.Collections.Generic.List<Vector2>
                {
                    new Vector2(_preferredWidth, _preferredHeight),
                    new Vector2(1280, 720),
                    new Vector2(640, 480)
                },
                DesiredFrameRate = _preferredFrameRate,
                FlipX = _flipX,
                FlipY = _flipY,
                PlayOnStart = true
            };

            var service = LiveCameraService.Instance;

            // 订阅纹理就绪事件
            service.OnTextureReady
                .Subscribe(texture =>
                {
                    _rawImage.texture = texture;
                    UpdateAspectRatio();
                    Debug.Log($"[LiveCameraDisplay] 纹理已绑定: {texture.width}x{texture.height}");
                })
                .AddTo(_disposables);

            // 订阅帧更新事件，持续更新纹理引用
            service.OnFrameUpdated
                .Subscribe(_ =>
                {
                    if (_rawImage.texture != service.OutputTexture && service.OutputTexture != null)
                    {
                        _rawImage.texture = service.OutputTexture;
                        UpdateAspectRatio();
                    }
                })
                .AddTo(_disposables);

            // 初始化服务
            service
                .Initialize(config)
                .Subscribe(success =>
                {
                    if (success)
                    {
                        Debug.Log("[LiveCameraDisplay] 摄像头初始化成功");
                    }
                    else
                    {
                        Debug.LogError("[LiveCameraDisplay] 摄像头初始化失败");
                    }
                })
                .AddTo(_disposables);
        }

        private void UpdateAspectRatio()
        {
            if (_maintainAspectRatio && _aspectRatioFitter != null && _rawImage.texture != null)
            {
                float aspect = (float)_rawImage.texture.width / _rawImage.texture.height;
                _aspectRatioFitter.aspectRatio = aspect;
            }
        }

        /// <summary>
        /// 截取当前帧
        /// </summary>
        public Texture2D CaptureFrame()
        {
            return LiveCameraService.Instance.CaptureFrame();
        }

        /// <summary>
        /// 截取并保存到文件
        /// </summary>
        public bool CaptureAndSave(string filePath)
        {
            return LiveCameraService.Instance.CaptureAndSave(filePath);
        }

        /// <summary>
        /// 停止摄像头
        /// </summary>
        public void StopCamera()
        {
            LiveCameraService.Instance.StopCamera();
            _rawImage.texture = null;
        }

        /// <summary>
        /// 重启摄像头
        /// </summary>
        public void RestartCamera()
        {
            LiveCameraService.Instance.RestartCamera();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
