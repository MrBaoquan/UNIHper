using System;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UNIHper.UI;

namespace UNIHper
{
    /// <summary>
    /// AVProLiveCamera 全屏调试面板 (UI Toolkit 版本)
    /// 左侧大画面实时预览 + 右侧控制面板
    /// 直接共享 LiveCameraService.OutputTexture，不会重新打开摄像头
    /// </summary>
    [UIToolkitPage(Asset = "UTK Pages/LiveCameraPanel", Type = UIType.Popup)]
    public class LiveCameraToolkitUI : UIToolkitBase
    {
        #region UI Elements

        // 顶部状态信息
        private Label _cameraStatusLabel;
        private Label _cameraDeviceLabel;
        private Label _cameraResolutionLabel;
        private Label _cameraFrameRateLabel;

        // 控制按钮
        private Button _initCameraBtn;
        private Button _startCameraBtn;
        private Button _stopCameraBtn;
        private Button _restartCameraBtn;

        // 设备选择
        private DropdownField _deviceDropdown;
        private Label _deviceCountLabel;
        private Button _refreshDevicesBtn;
        private Button _switchDeviceBtn;

        // 画面设置
        private Toggle _flipXToggle;
        private Toggle _flipYToggle;

        // 截图
        private Label _captureStatusLabel;
        private Button _captureFrameBtn;
        private Button _saveCaptureBtn;

        // 实时预览
        private VisualElement _previewImage;
        private Label _previewPlaceholder;

        // 配置信息
        private Label _configDeviceModeLabel;
        private Label _configResolutionModeLabel;
        private Label _configPreferredDevicesLabel;
        private Label _configPreferredResolutionsLabel;
        private Label _configFrameRateLabel;
        private Label _configFlipXLabel;
        private Label _configFlipYLabel;
        private Label _configPlayOnStartLabel;

        // 状态栏
        private Label _versionLabel;
        private Label _appNameLabel;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private Texture _lastCapturedTexture;

        #endregion

        #region Lifecycle

        protected override void OnLoaded()
        {
            Debug.Log("[LiveCameraToolkitUI] Live Camera 调试面板已加载");
        }

        protected override void OnShowing()
        {
            ApplyInlineStyles();
            InitializeUIElements();
            BindEvents();
            RefreshCameraStatus();
            RefreshDeviceList();
            UpdateConfigInfo();

            // 如果摄像头已在运行，立即共享纹理显示预览
            if (LiveCameraService.HasInstance && LiveCameraService.Instance.IsRunning)
            {
                var texture = LiveCameraService.Instance.OutputTexture;
                if (texture != null)
                {
                    ShowPreview(texture);
                }
                UpdateButtonStates(true);
            }
        }

        protected override void OnShown()
        {
            // 实时更新预览纹理（共享已有纹理，不重新打开摄像头）
            Observable.EveryUpdate().Subscribe(_ => UpdateRuntimeStatus()).AddTo(_disposables);

            // 监听摄像头运行状态变化
            if (LiveCameraService.HasInstance)
            {
                LiveCameraService.Instance.OnRunningStateChanged
                    .Subscribe(isRunning =>
                    {
                        RefreshCameraStatus();
                        UpdateButtonStates(isRunning);
                        if (!isRunning)
                            HidePreview();
                    })
                    .AddTo(_disposables);

                LiveCameraService.Instance.OnTextureReady
                    .Subscribe(texture =>
                    {
                        RefreshCameraStatus();
                        ShowPreview(texture);
                    })
                    .AddTo(_disposables);
            }
        }

        protected override void OnHidden()
        {
            _disposables.Clear();
        }

        #endregion

        #region Initialization — Inline Styles (全屏布局)

        private void ApplyInlineStyles()
        {
            var root = Q<VisualElement>("Root");
            if (root == null)
                return;

            // Root: 全屏，垂直布局（标题栏 + 主体 + 状态栏）
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.098f, 0.098f, 0.098f); // #191919

            // ── 顶部标题栏 ──
            var titleBar = Q<VisualElement>("TitleBar");
            if (titleBar != null)
            {
                titleBar.style.flexDirection = FlexDirection.Row;
                titleBar.style.justifyContent = Justify.SpaceBetween;
                titleBar.style.alignItems = Align.Center;
                titleBar.style.height = 40;
                titleBar.style.paddingLeft = 16;
                titleBar.style.paddingRight = 16;
                titleBar.style.backgroundColor = new Color(0.176f, 0.176f, 0.176f); // #2d2d2d
                titleBar.style.borderBottomWidth = 1;
                titleBar.style.borderBottomColor = new Color(0f, 0.478f, 0.8f); // #007acc
                titleBar.style.flexShrink = 0;
            }

            var title = Q<Label>("Title");
            if (title != null)
            {
                title.style.fontSize = 14;
                title.style.color = new Color(0.8f, 0.8f, 0.8f);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.flexShrink = 0;
            }

            // 标题栏中间 — 状态信息
            var titleCenter = Q<VisualElement>("TitleBarCenter");
            if (titleCenter != null)
            {
                titleCenter.style.flexDirection = FlexDirection.Row;
                titleCenter.style.flexGrow = 1;
                titleCenter.style.justifyContent = Justify.Center;
                titleCenter.style.alignItems = Align.Center;
            }

            // 状态信息标签样式
            foreach (var name in new[] { "CameraStatus", "CameraDevice", "CameraResolution", "CameraFrameRate" })
            {
                var label = Q<Label>(name);
                if (label != null)
                {
                    label.style.fontSize = 11;
                    label.style.color = new Color(0.733f, 0.733f, 0.733f);
                    label.style.marginLeft = 12;
                    label.style.marginRight = 12;
                }
            }

            var closeBtn = Q<Button>("CloseBtn");
            if (closeBtn != null)
            {
                closeBtn.style.width = 28;
                closeBtn.style.height = 28;
                closeBtn.style.fontSize = 14;
                closeBtn.style.color = new Color(0.6f, 0.6f, 0.6f);
                closeBtn.style.backgroundColor = Color.clear;
                closeBtn.style.borderTopWidth = 0;
                closeBtn.style.borderBottomWidth = 0;
                closeBtn.style.borderLeftWidth = 0;
                closeBtn.style.borderRightWidth = 0;
                closeBtn.style.borderTopLeftRadius = 4;
                closeBtn.style.borderTopRightRadius = 4;
                closeBtn.style.borderBottomLeftRadius = 4;
                closeBtn.style.borderBottomRightRadius = 4;
                closeBtn.style.flexShrink = 0;
            }

            // ── 主体区域：左侧预览 + 右侧控制 ──
            var mainBody = Q<VisualElement>("MainBody");
            if (mainBody != null)
            {
                mainBody.style.flexDirection = FlexDirection.Row;
                mainBody.style.flexGrow = 1;
            }

            // 左侧预览区
            var previewArea = Q<VisualElement>("PreviewArea");
            if (previewArea != null)
            {
                previewArea.style.flexGrow = 1;
                previewArea.style.backgroundColor = Color.black;
                previewArea.style.alignItems = Align.Center;
                previewArea.style.justifyContent = Justify.Center;
                previewArea.style.overflow = Overflow.Hidden;
            }

            var previewPlaceholder = Q<Label>("PreviewPlaceholder");
            if (previewPlaceholder != null)
            {
                previewPlaceholder.style.fontSize = 16;
                previewPlaceholder.style.color = new Color(0.4f, 0.4f, 0.4f);
                previewPlaceholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                previewPlaceholder.style.whiteSpace = WhiteSpace.Normal;
            }

            var previewImage = Q<VisualElement>("PreviewImage");
            if (previewImage != null)
            {
                previewImage.style.position = Position.Absolute;
                previewImage.style.left = 0;
                previewImage.style.top = 0;
                previewImage.style.right = 0;
                previewImage.style.bottom = 0;
                previewImage.style.display = DisplayStyle.None;
            }

            // 右侧控制面板
            var controlPanel = Q<ScrollView>("ControlPanel");
            if (controlPanel != null)
            {
                controlPanel.style.width = 320;
                controlPanel.style.flexShrink = 0;
                controlPanel.style.backgroundColor = new Color(0.118f, 0.118f, 0.118f); // #1e1e1e
                controlPanel.style.borderLeftWidth = 1;
                controlPanel.style.borderLeftColor = new Color(0.235f, 0.235f, 0.235f); // #3c3c3c
            }

            // ── 底部状态栏 ──
            var statusBar = Q<VisualElement>("StatusBar");
            if (statusBar != null)
            {
                statusBar.style.flexDirection = FlexDirection.Row;
                statusBar.style.justifyContent = Justify.SpaceBetween;
                statusBar.style.alignItems = Align.Center;
                statusBar.style.height = 22;
                statusBar.style.paddingLeft = 16;
                statusBar.style.paddingRight = 16;
                statusBar.style.backgroundColor = new Color(0f, 0.478f, 0.8f); // #007acc
                statusBar.style.flexShrink = 0;
            }

            var versionLabel = Q<Label>("Version");
            if (versionLabel != null)
            {
                versionLabel.style.fontSize = 10;
                versionLabel.style.color = new Color(1, 1, 1, 0.85f);
            }

            var appNameLabel = Q<Label>("AppName");
            if (appNameLabel != null)
            {
                appNameLabel.style.fontSize = 10;
                appNameLabel.style.color = Color.white;
                appNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            // ── 通用样式 ──
            StyleFoldouts();
            StyleButtons();
            StyleLabels();
        }

        private void StyleFoldouts()
        {
            Root.Query(className: "section-foldout")
                .ForEach(foldout =>
                {
                    foldout.style.marginBottom = 0;
                    foldout.style.marginTop = 0;
                    foldout.style.marginLeft = 0;
                    foldout.style.marginRight = 0;
                    foldout.style.paddingBottom = 0;
                    foldout.style.paddingTop = 0;
                    foldout.style.paddingLeft = 0;
                    foldout.style.paddingRight = 0;
                    foldout.style.backgroundColor = new Color(0.118f, 0.118f, 0.118f);
                    foldout.style.borderBottomWidth = 1;
                    foldout.style.borderBottomColor = new Color(0.176f, 0.176f, 0.176f);
                    foldout.style.borderTopLeftRadius = 0;
                    foldout.style.borderTopRightRadius = 0;
                    foldout.style.borderBottomLeftRadius = 0;
                    foldout.style.borderBottomRightRadius = 0;
                });
        }

        private void StyleButtons()
        {
            Root.Query(className: "action-btn")
                .ForEach(btn =>
                {
                    btn.style.flexGrow = 1;
                    btn.style.height = 26;
                    btn.style.marginTop = 2;
                    btn.style.marginBottom = 2;
                    btn.style.marginLeft = 2;
                    btn.style.marginRight = 2;
                    btn.style.fontSize = 11;
                    btn.style.color = new Color(0.8f, 0.8f, 0.8f);
                    btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    btn.style.borderTopWidth = 1;
                    btn.style.borderBottomWidth = 1;
                    btn.style.borderLeftWidth = 1;
                    btn.style.borderRightWidth = 1;
                    btn.style.borderTopColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderBottomColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderLeftColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderRightColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderTopLeftRadius = 2;
                    btn.style.borderTopRightRadius = 2;
                    btn.style.borderBottomLeftRadius = 2;
                    btn.style.borderBottomRightRadius = 2;
                    btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                });

            Root.Query(className: "primary-btn")
                .ForEach(btn =>
                {
                    btn.style.height = 26;
                    btn.style.paddingLeft = 16;
                    btn.style.paddingRight = 16;
                    btn.style.fontSize = 11;
                    btn.style.color = Color.white;
                    btn.style.backgroundColor = new Color(0.055f, 0.388f, 0.612f);
                    btn.style.borderTopWidth = 0;
                    btn.style.borderBottomWidth = 0;
                    btn.style.borderLeftWidth = 0;
                    btn.style.borderRightWidth = 0;
                    btn.style.borderTopLeftRadius = 2;
                    btn.style.borderTopRightRadius = 2;
                    btn.style.borderBottomLeftRadius = 2;
                    btn.style.borderBottomRightRadius = 2;
                });

            Root.Query(className: "secondary-btn")
                .ForEach(btn =>
                {
                    btn.style.height = 26;
                    btn.style.paddingLeft = 14;
                    btn.style.paddingRight = 14;
                    btn.style.fontSize = 11;
                    btn.style.color = new Color(0.8f, 0.8f, 0.8f);
                    btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    btn.style.borderTopWidth = 1;
                    btn.style.borderBottomWidth = 1;
                    btn.style.borderLeftWidth = 1;
                    btn.style.borderRightWidth = 1;
                    btn.style.borderTopColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderBottomColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderLeftColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderRightColor = new Color(0.267f, 0.267f, 0.267f);
                    btn.style.borderTopLeftRadius = 2;
                    btn.style.borderTopRightRadius = 2;
                    btn.style.borderBottomLeftRadius = 2;
                    btn.style.borderBottomRightRadius = 2;
                });

            Root.Query(className: "button-row")
                .ForEach(row =>
                {
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 4;
                });
        }

        private void StyleLabels()
        {
            Root.Query(className: "info-label")
                .ForEach(label =>
                {
                    label.style.paddingTop = 2;
                    label.style.paddingBottom = 2;
                    label.style.fontSize = 11;
                    label.style.color = new Color(0.733f, 0.733f, 0.733f);
                });

            Root.Query(className: "group-label")
                .ForEach(label =>
                {
                    label.style.fontSize = 11;
                    label.style.color = new Color(0.533f, 0.533f, 0.533f);
                    label.style.marginBottom = 6;
                    label.style.marginTop = 4;
                });

            Root.Query(className: "settings-group")
                .ForEach(g =>
                {
                    g.style.marginBottom = 8;
                });

            Root.Query(className: "info-grid")
                .ForEach(g =>
                {
                    g.style.flexDirection = FlexDirection.Column;
                });
        }

        #endregion

        #region Initialization — Elements & Events

        private void InitializeUIElements()
        {
            // 关闭按钮
            Q<Button>("CloseBtn")
                ?.RegisterCallback<ClickEvent>(_ => Hide());

            // 顶部状态
            _cameraStatusLabel = Q<Label>("CameraStatus");
            _cameraDeviceLabel = Q<Label>("CameraDevice");
            _cameraResolutionLabel = Q<Label>("CameraResolution");
            _cameraFrameRateLabel = Q<Label>("CameraFrameRate");

            // 控制按钮
            _initCameraBtn = Q<Button>("InitCamera");
            _startCameraBtn = Q<Button>("StartCamera");
            _stopCameraBtn = Q<Button>("StopCamera");
            _restartCameraBtn = Q<Button>("RestartCamera");

            // 设备选择
            _deviceDropdown = Q<DropdownField>("DeviceDropdown");
            _deviceCountLabel = Q<Label>("DeviceCount");
            _refreshDevicesBtn = Q<Button>("RefreshDevices");
            _switchDeviceBtn = Q<Button>("SwitchDevice");

            // 画面设置
            _flipXToggle = Q<Toggle>("FlipX");
            _flipYToggle = Q<Toggle>("FlipY");

            // 截图
            _captureStatusLabel = Q<Label>("CaptureStatus");
            _captureFrameBtn = Q<Button>("CaptureFrame");
            _saveCaptureBtn = Q<Button>("SaveCapture");

            // 实时预览
            _previewImage = Q<VisualElement>("PreviewImage");
            _previewPlaceholder = Q<Label>("PreviewPlaceholder");

            // 配置信息
            _configDeviceModeLabel = Q<Label>("ConfigDeviceMode");
            _configResolutionModeLabel = Q<Label>("ConfigResolutionMode");
            _configPreferredDevicesLabel = Q<Label>("ConfigPreferredDevices");
            _configPreferredResolutionsLabel = Q<Label>("ConfigPreferredResolutions");
            _configFrameRateLabel = Q<Label>("ConfigFrameRate");
            _configFlipXLabel = Q<Label>("ConfigFlipX");
            _configFlipYLabel = Q<Label>("ConfigFlipY");
            _configPlayOnStartLabel = Q<Label>("ConfigPlayOnStart");

            // 状态栏
            _versionLabel = Q<Label>("Version");
            _appNameLabel = Q<Label>("AppName");

            if (_appNameLabel != null)
                _appNameLabel.text = Application.productName;

            // 初始化翻转 Toggle 状态
            if (LiveCameraService.HasInstance)
            {
                var config = LiveCameraService.Instance.Config;
                _flipXToggle?.SetValueWithoutNotify(config.FlipX);
                _flipYToggle?.SetValueWithoutNotify(config.FlipY);
            }
        }

        private void BindEvents()
        {
            // 摄像头控制按钮
            _initCameraBtn?.RegisterCallback<ClickEvent>(_ => OnInitCamera());
            _startCameraBtn?.RegisterCallback<ClickEvent>(_ => OnStartCamera());
            _stopCameraBtn?.RegisterCallback<ClickEvent>(_ => OnStopCamera());
            _restartCameraBtn?.RegisterCallback<ClickEvent>(_ => OnRestartCamera());

            // 设备选择
            _refreshDevicesBtn?.RegisterCallback<ClickEvent>(_ => RefreshDeviceList());
            _switchDeviceBtn?.RegisterCallback<ClickEvent>(_ => OnSwitchDevice());

            // 画面设置
            _flipXToggle?.RegisterValueChangedCallback(evt =>
            {
                if (LiveCameraService.HasInstance)
                {
                    LiveCameraService.Instance.SetFlipX(evt.newValue);
                    LiveCameraService.Instance.Config?.Save();
                    UpdateConfigInfo();
                    Debug.Log($"[LiveCamera Debug] 水平翻转: {evt.newValue}（已保存）");
                }
            });

            _flipYToggle?.RegisterValueChangedCallback(evt =>
            {
                if (LiveCameraService.HasInstance)
                {
                    LiveCameraService.Instance.SetFlipY(evt.newValue);
                    LiveCameraService.Instance.Config?.Save();
                    UpdateConfigInfo();
                    Debug.Log($"[LiveCamera Debug] 垂直翻转: {evt.newValue}（已保存）");
                }
            });

            // 截图
            _captureFrameBtn?.RegisterCallback<ClickEvent>(_ => OnCaptureFrame());
            _saveCaptureBtn?.RegisterCallback<ClickEvent>(_ => OnSaveCapture());
        }

        #endregion

        #region Camera Control

        private void OnInitCamera()
        {
            SetCaptureStatus("初始化中...");
            LiveCameraService.Instance
                .Initialize()
                .Subscribe(success =>
                {
                    if (success)
                    {
                        SetCaptureStatus("初始化成功");
                        RefreshCameraStatus();
                        RefreshDeviceList();
                        UpdateConfigInfo();
                    }
                    else
                    {
                        SetCaptureStatus("初始化失败");
                    }
                    Debug.Log($"[LiveCamera Debug] 初始化: {(success ? "成功" : "失败")}");
                })
                .AddTo(_disposables);
        }

        private void OnStartCamera()
        {
            if (!LiveCameraService.HasInstance)
                return;
            LiveCameraService.Instance.StartCamera();
            Debug.Log("[LiveCamera Debug] 启动摄像头");
        }

        private void OnStopCamera()
        {
            if (!LiveCameraService.HasInstance)
                return;
            LiveCameraService.Instance.StopCamera();
            HidePreview();
            Debug.Log("[LiveCamera Debug] 停止摄像头");
        }

        private void OnRestartCamera()
        {
            if (!LiveCameraService.HasInstance)
                return;
            LiveCameraService.Instance.RestartCamera();
            Debug.Log("[LiveCamera Debug] 重启摄像头");
        }

        #endregion

        #region Device Selection

        private void RefreshDeviceList()
        {
            if (!LiveCameraService.HasInstance)
            {
                if (_deviceDropdown != null)
                    _deviceDropdown.choices = new List<string> { "(未初始化)" };
                if (_deviceCountLabel != null)
                    _deviceCountLabel.text = "设备数: 0";
                return;
            }

            var devices = LiveCameraService.Instance.GetAvailableDevices();
            var deviceCount = LiveCameraService.Instance.DeviceCount;

            if (_deviceDropdown != null)
            {
                if (devices != null && devices.Count > 0)
                {
                    _deviceDropdown.choices = devices;
                    var currentDevice = LiveCameraService.Instance.DeviceName;
                    if (!string.IsNullOrEmpty(currentDevice) && devices.Contains(currentDevice))
                    {
                        _deviceDropdown.SetValueWithoutNotify(currentDevice);
                    }
                    else
                    {
                        _deviceDropdown.SetValueWithoutNotify(devices[0]);
                    }
                }
                else
                {
                    _deviceDropdown.choices = new List<string> { "(未检测到设备)" };
                }
            }

            if (_deviceCountLabel != null)
                _deviceCountLabel.text = $"设备数: {deviceCount}";
        }

        private void OnSwitchDevice()
        {
            if (!LiveCameraService.HasInstance || _deviceDropdown == null)
                return;

            var selectedDevice = _deviceDropdown.value;
            if (string.IsNullOrEmpty(selectedDevice) || selectedDevice.StartsWith("("))
                return;

            LiveCameraService.Instance.SwitchDevice(selectedDevice);
            LiveCameraService.Instance.Config?.Save();
            Debug.Log($"[LiveCamera Debug] 切换设备: {selectedDevice}（已保存）");
        }

        #endregion

        #region Capture

        private void OnCaptureFrame()
        {
            if (!LiveCameraService.HasInstance || !LiveCameraService.Instance.IsRunning)
            {
                SetCaptureStatus("截图失败: 摄像头未运行");
                return;
            }

            _lastCapturedTexture = LiveCameraService.Instance.CaptureFrame();
            if (_lastCapturedTexture != null)
            {
                SetCaptureStatus($"已截取 ({_lastCapturedTexture.width}x{_lastCapturedTexture.height})");
            }
            else
            {
                SetCaptureStatus("截图失败: 无法获取纹理");
            }
        }

        private void OnSaveCapture()
        {
            if (!LiveCameraService.HasInstance || !LiveCameraService.Instance.IsRunning)
            {
                SetCaptureStatus("保存失败: 摄像头未运行");
                return;
            }

            var savePath = Path.Combine(Application.persistentDataPath, "Captures", $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            var dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var success = LiveCameraService.Instance.CaptureAndSave(savePath);
            if (success)
            {
                SetCaptureStatus($"已保存: {Path.GetFileName(savePath)}");
                Debug.Log($"[LiveCamera Debug] 截图已保存: {savePath}");
            }
            else
            {
                SetCaptureStatus("保存失败");
            }
        }

        private void SetCaptureStatus(string status)
        {
            if (_captureStatusLabel != null)
                _captureStatusLabel.text = $"截图状态: {status}";
        }

        #endregion

        #region Preview (纹理共享)

        /// <summary>
        /// 显示预览 — 直接引用 LiveCameraService 的 OutputTexture，零拷贝
        /// </summary>
        private void ShowPreview(Texture texture)
        {
            if (_previewImage == null || texture == null)
                return;

            _previewImage.style.display = DisplayStyle.Flex;

            // 尝试作为 RenderTexture 绑定，否则用 Texture2D
            if (texture is RenderTexture rt)
            {
                _previewImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            }
            else if (texture is Texture2D t2d)
            {
                _previewImage.style.backgroundImage = new StyleBackground(Background.FromTexture2D(t2d));
            }

            if (_previewPlaceholder != null)
                _previewPlaceholder.style.display = DisplayStyle.None;
        }

        private void HidePreview()
        {
            if (_previewImage != null)
            {
                _previewImage.style.display = DisplayStyle.None;
                _previewImage.style.backgroundImage = StyleKeyword.None;
            }

            if (_previewPlaceholder != null)
            {
                _previewPlaceholder.style.display = DisplayStyle.Flex;
                _previewPlaceholder.text = "摄像头未启动\n\n点击右侧 [初始化] 开始";
            }
        }

        #endregion

        #region Status Updates

        private void RefreshCameraStatus()
        {
            if (!LiveCameraService.HasInstance)
            {
                SetStatusLabels("未初始化", "--", "--", "--", false);
                return;
            }

            var service = LiveCameraService.Instance;
            var statusText = service.IsRunning ? "● 运行中" : "○ 已停止";
            var deviceText = string.IsNullOrEmpty(service.DeviceName) ? "--" : service.DeviceName;
            var resText = service.Width > 0 ? $"{service.Width}x{service.Height}" : "--";
            var fpsText = service.FrameRate > 0 ? $"{service.FrameRate:F1} fps" : "--";

            SetStatusLabels(statusText, deviceText, resText, fpsText, service.IsRunning);
        }

        private void SetStatusLabels(string status, string device, string resolution, string fps, bool isRunning)
        {
            if (_cameraStatusLabel != null)
            {
                _cameraStatusLabel.text = $"状态: {status}";
                _cameraStatusLabel.style.color = isRunning
                    ? new Color(0.306f, 0.788f, 0.69f) // 绿色
                    : new Color(0.957f, 0.529f, 0.443f); // 红色
            }

            if (_cameraDeviceLabel != null)
                _cameraDeviceLabel.text = $"设备: {device}";
            if (_cameraResolutionLabel != null)
                _cameraResolutionLabel.text = $"分辨率: {resolution}";
            if (_cameraFrameRateLabel != null)
                _cameraFrameRateLabel.text = $"帧率: {fps}";
        }

        private void UpdateButtonStates(bool isRunning)
        {
            _startCameraBtn?.SetEnabled(!isRunning);
            _stopCameraBtn?.SetEnabled(isRunning);
            _restartCameraBtn?.SetEnabled(isRunning);
            _captureFrameBtn?.SetEnabled(isRunning);
            _saveCaptureBtn?.SetEnabled(isRunning);
        }

        private void UpdateRuntimeStatus()
        {
            if (!LiveCameraService.HasInstance)
                return;

            var service = LiveCameraService.Instance;

            // 共享纹理：如果摄像头已在运行且预览未显示，直接绑定
            if (service.IsRunning && service.OutputTexture != null && _previewImage != null)
            {
                if (_previewImage.style.display == DisplayStyle.None)
                {
                    ShowPreview(service.OutputTexture);
                    RefreshCameraStatus();
                    UpdateButtonStates(true);
                }
            }
        }

        private void UpdateConfigInfo()
        {
            if (!LiveCameraService.HasInstance)
                return;

            var config = LiveCameraService.Instance.Config;
            if (config == null)
                return;

            if (_configDeviceModeLabel != null)
                _configDeviceModeLabel.text = $"设备选择: {config.DeviceSelection}";
            if (_configResolutionModeLabel != null)
                _configResolutionModeLabel.text = $"分辨率选择: {config.ModeSelection}";
            if (_configPreferredDevicesLabel != null)
            {
                var devices = config.PreferredDeviceNames != null ? string.Join(", ", config.PreferredDeviceNames) : "--";
                _configPreferredDevicesLabel.text = $"首选设备: {devices}";
            }
            if (_configPreferredResolutionsLabel != null)
            {
                var resolutions = "--";
                if (config.PreferredResolutions != null && config.PreferredResolutions.Count > 0)
                {
                    var resList = new List<string>();
                    foreach (SerializableVector2 res in config.PreferredResolutions)
                        resList.Add($"{res.x}x{res.y}");
                    resolutions = string.Join(", ", resList);
                }
                _configPreferredResolutionsLabel.text = $"首选分辨率: {resolutions}";
            }
            if (_configFrameRateLabel != null)
                _configFrameRateLabel.text = $"目标帧率: {config.DesiredFrameRate} fps";
            if (_configFlipXLabel != null)
                _configFlipXLabel.text = $"水平翻转: {config.FlipX}";
            if (_configFlipYLabel != null)
                _configFlipYLabel.text = $"垂直翻转: {config.FlipY}";
            if (_configPlayOnStartLabel != null)
                _configPlayOnStartLabel.text = $"自动启动: {config.PlayOnStart}";
        }

        #endregion
    }
}
