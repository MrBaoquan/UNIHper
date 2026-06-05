using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UNIHper.UI;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using DNHper;
#endif

namespace UNIHper
{
    /// <summary>
    /// 调试工具入口定义
    /// </summary>
    public class DebuggerToolEntry
    {
        /// <summary>唯一标识符</summary>
        public string Id;

        /// <summary>显示名称</summary>
        public string Name;

        /// <summary>图标 (Emoji / Unicode)</summary>
        public string Icon;

        /// <summary>工具提示</summary>
        public string Tooltip;

        /// <summary>排序权重 (越小越靠前)</summary>
        public int Order;

        /// <summary>点击回调</summary>
        public Action OnClick;
    }

    /// <summary>
    /// UNIHper 调试中心 (UI Toolkit)
    /// 提供显示设置、可扩展工具箱、快捷操作、系统信息、运行时监控
    /// </summary>
    [UIToolkitPage(Asset = "UTK Pages/UNIDebuggerPanel", Type = UIType.Popup)]
    public class UNIDebuggerToolkitUI : UIToolkitBase
    {
        #region Static Tool Registry

        private static readonly List<DebuggerToolEntry> _registeredTools = new List<DebuggerToolEntry>();
        private static event Action OnToolsChanged;

        /// <summary>
        /// 注册一个调试工具到工具箱
        /// </summary>
        public static void RegisterTool(DebuggerToolEntry tool)
        {
            if (tool == null || string.IsNullOrEmpty(tool.Id))
            {
                Debug.LogWarning("[UNIDebugger] RegisterTool: Id 不能为空");
                return;
            }
            _registeredTools.RemoveAll(t => t.Id == tool.Id);
            _registeredTools.Add(tool);
            OnToolsChanged?.Invoke();
        }

        /// <summary>
        /// 注销一个调试工具
        /// </summary>
        public static void UnregisterTool(string id)
        {
            _registeredTools.RemoveAll(t => t.Id == id);
            OnToolsChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有注册的工具
        /// </summary>
        public static void ClearTools()
        {
            _registeredTools.Clear();
            OnToolsChanged?.Invoke();
        }

        #endregion

        #region UI Elements

        // 运行时状态栏
        private Label _fpsLabel;
        private Label _frameTimeLabel;
        private Label _usedMemoryLabel;
        private Label _runTimeLabel;

        // 显示设置
        private IntegerField _widthField;
        private IntegerField _heightField;
        private IntegerField _posXField;
        private IntegerField _posYField;
        private Toggle _fullscreenToggle;
        private Toggle _useTitleBarToggle;
        private Toggle _keepTopToggle;
        private Label _screenResolutionLabel;
        private Label _screenRefreshRateLabel;
        private Button _applyWindowSettingsBtn;
        private Button _resetWindowSettingsBtn;

        // 应用设置
        private FloatField _longTimeNoOperationTimeoutField;
        private FloatField _checkDesktopResolutionIntervalField;
        private FloatField _resetPrimaryScreenIntervalField;
        private Button _applyAppSettingsBtn;
        private Button _resetAppSettingsBtn;

        // 工具箱
        private VisualElement _toolGrid;

        // 快捷操作
        private Button _openPersistentDataBtn;
        private Button _openStreamingAssetsBtn;
        private Button _openDataPathBtn;
        private Button _clearPlayerPrefsBtn;

        // 系统信息
        private Label _deviceModelLabel;
        private Label _deviceTypeLabel;
        private Label _osLabel;
        private Label _cpuLabel;
        private Label _gpuLabel;
        private Label _memoryLabel;
        private Label _graphicsMemoryLabel;

        // 状态栏
        private Label _versionLabel;
        private Label _appNameLabel;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private float _fpsUpdateInterval = 0.5f;
        private float _lastFpsUpdateTime;
        private int _frameCount;
        private float _currentFps;

        // 内置工具定义
        private static readonly DebuggerToolEntry[] _builtinTools = new[]
        {
            new DebuggerToolEntry
            {
                Id = "__builtin_livecamera",
                Name = "摄像头调试",
                Icon = "📷",
                Tooltip = "打开 AVProLiveCamera 调试面板",
                Order = 100,
                OnClick = () =>
                {
                    Managements.UIToolkit.Hide<UNIDebuggerToolkitUI>();
                    Managements.UIToolkit.Show<LiveCameraToolkitUI>();
                }
            }
        };

        #endregion

        #region Lifecycle

        protected override void OnLoaded()
        {
            Debug.Log("[UNIDebuggerToolkitUI] 调试中心已加载");
        }

        protected override void OnShowing()
        {
            ApplyInlineStyles();
            InitializeUIElements();
            BindEvents();
            LoadWindowSettings();
            LoadAppSettings();
            UpdateScreenInfo();
            UpdateSystemInfo();
            RebuildToolGrid();

            OnToolsChanged += RebuildToolGrid;
        }

        protected override void OnShown()
        {
            Observable.EveryUpdate().Subscribe(_ => UpdateRuntimeInfo()).AddTo(_disposables);
        }

        protected override void OnHiding()
        {
            Managements.Config.Get<AppConfig>()?.ResetPrimaryScreen();
        }

        protected override void OnHidden()
        {
            _disposables.Clear();
            OnToolsChanged -= RebuildToolGrid;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 通过内联样式确保核心布局正确（USS 可能因路径问题加载失败）
        /// </summary>
        private void ApplyInlineStyles()
        {
            var root = Q<VisualElement>("Root");
            if (root == null)
                return;

            // Root: 全屏绝对定位，水平布局
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Stretch;

            // Panel: 左侧固定宽度
            var panel = Q<VisualElement>("Panel");
            if (panel != null)
            {
                panel.style.width = 400;
                panel.style.flexShrink = 0;
                panel.style.backgroundColor = new Color(0.118f, 0.118f, 0.118f);
                panel.style.borderRightWidth = 1;
                panel.style.borderRightColor = new Color(0f, 0.478f, 0.8f);
            }

            // Backdrop: 右侧半透明遮罩
            var backdrop = Q<VisualElement>("Backdrop");
            if (backdrop != null)
            {
                backdrop.style.flexGrow = 1;
                backdrop.style.backgroundColor = new Color(0, 0, 0, 0.45f);
            }

            // Title Bar
            var titleBar = Q<VisualElement>("TitleBar");
            if (titleBar != null)
            {
                titleBar.style.flexDirection = FlexDirection.Row;
                titleBar.style.justifyContent = Justify.SpaceBetween;
                titleBar.style.alignItems = Align.Center;
                titleBar.style.height = 40;
                titleBar.style.paddingLeft = 16;
                titleBar.style.paddingRight = 16;
                titleBar.style.backgroundColor = new Color(0.176f, 0.176f, 0.176f);
                titleBar.style.borderBottomWidth = 1;
                titleBar.style.borderBottomColor = new Color(0f, 0.478f, 0.8f);
                titleBar.style.flexShrink = 0;
            }

            // Title Label
            var title = Q<Label>("Title");
            if (title != null)
            {
                title.style.fontSize = 13;
                title.style.color = new Color(0.8f, 0.8f, 0.8f);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            // Close Button
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
            }

            // Content ScrollView
            var content = Q<ScrollView>("Content");
            if (content != null)
            {
                content.style.flexGrow = 1;
                content.style.backgroundColor = new Color(0.118f, 0.118f, 0.118f);
            }

            // Runtime Bar
            var runtimeBar = Q<VisualElement>("RuntimeBar");
            if (runtimeBar != null)
            {
                runtimeBar.style.flexDirection = FlexDirection.Row;
                runtimeBar.style.justifyContent = Justify.SpaceAround;
                runtimeBar.style.alignItems = Align.Center;
                runtimeBar.style.height = 28;
                runtimeBar.style.paddingLeft = 12;
                runtimeBar.style.paddingRight = 12;
                runtimeBar.style.backgroundColor = new Color(0.145f, 0.145f, 0.149f);
                runtimeBar.style.borderBottomWidth = 1;
                runtimeBar.style.borderBottomColor = new Color(0.176f, 0.176f, 0.176f);
                runtimeBar.style.flexShrink = 0;
            }

            Root.Query(className: "runtime-item")
                .ForEach(label =>
                {
                    label.style.fontSize = 10;
                    label.style.color = new Color(0.533f, 0.533f, 0.533f);
                    label.style.paddingLeft = 6;
                    label.style.paddingRight = 6;
                });

            // Status Bar
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

            // Style all section foldouts
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

            // Style all action buttons
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

            // Style danger buttons
            Root.Query(className: "danger")
                .ForEach(btn =>
                {
                    btn.style.color = new Color(0.957f, 0.529f, 0.443f); // #f48771
                    btn.style.borderTopColor = new Color(0.353f, 0.125f, 0.125f);
                    btn.style.borderBottomColor = new Color(0.353f, 0.125f, 0.125f);
                    btn.style.borderLeftColor = new Color(0.353f, 0.125f, 0.125f);
                    btn.style.borderRightColor = new Color(0.353f, 0.125f, 0.125f);
                });

            // Style primary button
            Root.Query(className: "primary-btn")
                .ForEach(btn =>
                {
                    btn.style.height = 26;
                    btn.style.paddingLeft = 16;
                    btn.style.paddingRight = 16;
                    btn.style.fontSize = 11;
                    btn.style.color = Color.white;
                    btn.style.backgroundColor = new Color(0.055f, 0.388f, 0.612f); // #0e639c
                    btn.style.borderTopWidth = 0;
                    btn.style.borderBottomWidth = 0;
                    btn.style.borderLeftWidth = 0;
                    btn.style.borderRightWidth = 0;
                    btn.style.borderTopLeftRadius = 2;
                    btn.style.borderTopRightRadius = 2;
                    btn.style.borderBottomLeftRadius = 2;
                    btn.style.borderBottomRightRadius = 2;
                });

            // Style secondary button
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

            // Style button rows
            Root.Query(className: "button-row")
                .ForEach(row =>
                {
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 4;
                });

            // Style info labels
            Root.Query(className: "info-label")
                .ForEach(label =>
                {
                    label.style.paddingTop = 2;
                    label.style.paddingBottom = 2;
                    label.style.fontSize = 11;
                    label.style.color = new Color(0.733f, 0.733f, 0.733f);
                });

            // Highlight info label
            Root.Query(className: "highlight")
                .ForEach(label =>
                {
                    label.style.color = new Color(0.306f, 0.788f, 0.69f); // #4ec9b0
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                });

            // Group labels
            Root.Query(className: "group-label")
                .ForEach(label =>
                {
                    label.style.fontSize = 11;
                    label.style.color = new Color(0.533f, 0.533f, 0.533f);
                    label.style.marginBottom = 6;
                    label.style.marginTop = 4;
                });

            // Settings group
            Root.Query(className: "settings-group")
                .ForEach(g =>
                {
                    g.style.marginBottom = 8;
                });

            // Info grid
            Root.Query(className: "info-grid")
                .ForEach(g =>
                {
                    g.style.flexDirection = FlexDirection.Column;
                });
        }

        private void InitializeUIElements()
        {
            // 关闭
            Q<Button>("CloseBtn")
                ?.RegisterCallback<ClickEvent>(_ => Hide());
            Q<VisualElement>("Backdrop")?.RegisterCallback<ClickEvent>(_ => Hide());

            // 运行时状态栏
            _fpsLabel = Q<Label>("FPS");
            _frameTimeLabel = Q<Label>("FrameTime");
            _usedMemoryLabel = Q<Label>("UsedMemory");
            _runTimeLabel = Q<Label>("RunTime");

            // 显示设置
            _widthField = Q<IntegerField>("Width");
            _heightField = Q<IntegerField>("Height");
            _posXField = Q<IntegerField>("PosX");
            _posYField = Q<IntegerField>("PosY");
            _fullscreenToggle = Q<Toggle>("Fullscreen");
            _useTitleBarToggle = Q<Toggle>("UseTitleBar");
            _keepTopToggle = Q<Toggle>("KeepTop");
            _screenResolutionLabel = Q<Label>("ScreenResolution");
            _screenRefreshRateLabel = Q<Label>("ScreenRefreshRate");
            _applyWindowSettingsBtn = Q<Button>("ApplyWindowSettings");
            _resetWindowSettingsBtn = Q<Button>("ResetWindowSettings");

            // 应用设置
            _longTimeNoOperationTimeoutField = Q<FloatField>("LongTimeNoOperationTimeout");
            _checkDesktopResolutionIntervalField = Q<FloatField>("CheckDesktopResolutionInterval");
            _resetPrimaryScreenIntervalField = Q<FloatField>("ResetPrimaryScreenInterval");
            _applyAppSettingsBtn = Q<Button>("ApplyAppSettings");
            _resetAppSettingsBtn = Q<Button>("ResetAppSettings");

            // 工具箱
            _toolGrid = Q<VisualElement>("ToolGrid");

            // 快捷操作
            _openPersistentDataBtn = Q<Button>("OpenPersistentData");
            _openStreamingAssetsBtn = Q<Button>("OpenStreamingAssets");
            _openDataPathBtn = Q<Button>("OpenDataPath");
            _clearPlayerPrefsBtn = Q<Button>("ClearPlayerPrefs");

            // 系统信息
            _deviceModelLabel = Q<Label>("DeviceModel");
            _deviceTypeLabel = Q<Label>("DeviceType");
            _osLabel = Q<Label>("OS");
            _cpuLabel = Q<Label>("CPU");
            _gpuLabel = Q<Label>("GPU");
            _memoryLabel = Q<Label>("Memory");
            _graphicsMemoryLabel = Q<Label>("GraphicsMemory");

            // 状态栏
            _versionLabel = Q<Label>("Version");
            _appNameLabel = Q<Label>("AppName");

            _versionLabel.text = $"UNIHper v{GetUNIHperVersion()}";
            _appNameLabel.text = Application.productName;

            UpdateWindowModeVisibility(_fullscreenToggle?.value ?? false);
        }

        private void BindEvents()
        {
            // 显示设置
            _fullscreenToggle?.RegisterValueChangedCallback(evt => UpdateWindowModeVisibility(evt.newValue));
            _applyWindowSettingsBtn?.RegisterCallback<ClickEvent>(_ => ApplyWindowSettings());
            _resetWindowSettingsBtn?.RegisterCallback<ClickEvent>(_ => LoadWindowSettings());

            // 应用设置
            _applyAppSettingsBtn?.RegisterCallback<ClickEvent>(_ => ApplyAppSettings());
            _resetAppSettingsBtn?.RegisterCallback<ClickEvent>(_ => LoadAppSettings());

            // 快捷操作
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _openPersistentDataBtn?.RegisterCallback<ClickEvent>(_ =>
            {
                WinAPI.OpenProcess("explorer.exe", Application.persistentDataPath.Replace("/", "\\") + "\\", true);
            });

            _openStreamingAssetsBtn?.RegisterCallback<ClickEvent>(_ =>
            {
                WinAPI.OpenProcess("explorer.exe", Application.streamingAssetsPath.Replace("/", "\\") + "\\", true);
            });

            _openDataPathBtn?.RegisterCallback<ClickEvent>(_ =>
            {
                WinAPI.OpenProcess("explorer.exe", Application.dataPath.Replace("/", "\\") + "\\", true);
            });
#else
            _openPersistentDataBtn?.SetEnabled(false);
            _openStreamingAssetsBtn?.SetEnabled(false);
            _openDataPathBtn?.SetEnabled(false);
#endif

            _clearPlayerPrefsBtn?.RegisterCallback<ClickEvent>(_ =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[UNIDebugger] PlayerPrefs 已清除");
            });
        }

        #endregion

        #region Tool Grid (Extensible)

        private void RebuildToolGrid()
        {
            if (_toolGrid == null)
                return;

            _toolGrid.Clear();

            var allTools = _builtinTools.Concat(_registeredTools).OrderBy(t => t.Order).ToList();

            foreach (var tool in allTools)
            {
                _toolGrid.Add(CreateToolCard(tool));
            }

            if (allTools.Count == 0)
            {
                var emptyLabel = new Label("暂无可用工具");
                emptyLabel.style.fontSize = 11;
                emptyLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.paddingTop = 16;
                emptyLabel.style.paddingBottom = 16;
                _toolGrid.Add(emptyLabel);
            }

            _toolGrid.style.flexDirection = FlexDirection.Row;
            _toolGrid.style.flexWrap = Wrap.Wrap;
            _toolGrid.style.justifyContent = Justify.FlexStart;
            _toolGrid.style.marginLeft = -3;
            _toolGrid.style.marginRight = -3;
            _toolGrid.style.marginTop = -3;
            _toolGrid.style.marginBottom = -3;
        }

        private VisualElement CreateToolCard(DebuggerToolEntry tool)
        {
            var card = new VisualElement();
            card.name = $"Tool_{tool.Id}";
            card.tooltip = tool.Tooltip ?? tool.Name;

            card.style.width = 108;
            card.style.height = 76;
            card.style.marginLeft = 3;
            card.style.marginRight = 3;
            card.style.marginTop = 3;
            card.style.marginBottom = 3;
            card.style.backgroundColor = new Color(0.176f, 0.176f, 0.176f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new Color(0.235f, 0.235f, 0.235f);
            card.style.borderBottomColor = new Color(0.235f, 0.235f, 0.235f);
            card.style.borderLeftColor = new Color(0.235f, 0.235f, 0.235f);
            card.style.borderRightColor = new Color(0.235f, 0.235f, 0.235f);
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;
            card.style.alignItems = Align.Center;
            card.style.justifyContent = Justify.Center;
            card.style.flexDirection = FlexDirection.Column;

            var icon = new Label(tool.Icon ?? "🔧");
            icon.style.fontSize = 24;
            icon.style.color = new Color(0.8f, 0.8f, 0.8f);
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.marginBottom = 4;
            card.Add(icon);

            var name = new Label(tool.Name ?? "工具");
            name.style.fontSize = 10;
            name.style.color = new Color(0.733f, 0.733f, 0.733f);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.whiteSpace = WhiteSpace.NoWrap;
            name.style.overflow = Overflow.Hidden;
            name.style.textOverflow = TextOverflow.Ellipsis;
            name.style.maxWidth = 96;
            card.Add(name);

            card.RegisterCallback<MouseEnterEvent>(_ =>
            {
                card.style.backgroundColor = new Color(0.216f, 0.216f, 0.24f);
                card.style.borderTopColor = new Color(0f, 0.478f, 0.8f);
                card.style.borderBottomColor = new Color(0f, 0.478f, 0.8f);
                card.style.borderLeftColor = new Color(0f, 0.478f, 0.8f);
                card.style.borderRightColor = new Color(0f, 0.478f, 0.8f);
            });

            card.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                card.style.backgroundColor = new Color(0.176f, 0.176f, 0.176f);
                card.style.borderTopColor = new Color(0.235f, 0.235f, 0.235f);
                card.style.borderBottomColor = new Color(0.235f, 0.235f, 0.235f);
                card.style.borderLeftColor = new Color(0.235f, 0.235f, 0.235f);
                card.style.borderRightColor = new Color(0.235f, 0.235f, 0.235f);
            });

            card.RegisterCallback<ClickEvent>(_ =>
            {
                try
                {
                    tool.OnClick?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UNIDebugger] 工具 '{tool.Name}' 执行出错: {ex.Message}");
                }
            });

            return card;
        }

        #endregion

        #region Display Settings

        private void LoadWindowSettings()
        {
            var appConfig = Managements.Config.Get<AppConfig>();
            if (appConfig == null)
                return;

            var screen = appConfig.PrimaryScreen;

            _widthField?.SetValueWithoutNotify(screen.Width);
            _heightField?.SetValueWithoutNotify(screen.Height);
            _posXField?.SetValueWithoutNotify(screen.PosX);
            _posYField?.SetValueWithoutNotify(screen.PosY);
            _fullscreenToggle?.SetValueWithoutNotify(screen.Mode == FullScreenMode.FullScreenWindow);
            _useTitleBarToggle?.SetValueWithoutNotify(screen.UseTitleBar);
            _keepTopToggle?.SetValueWithoutNotify(screen.KeepTop);

            UpdateWindowModeVisibility(screen.Mode == FullScreenMode.FullScreenWindow);
        }

        private void ApplyWindowSettings()
        {
            var appConfig = Managements.Config.Get<AppConfig>();
            if (appConfig == null)
                return;

            var screen = appConfig.PrimaryScreen;

            screen.Width = _widthField?.value ?? 1920;
            screen.Height = _heightField?.value ?? 1080;
            screen.PosX = _posXField?.value ?? 0;
            screen.PosY = _posYField?.value ?? 0;
            screen.Mode = (_fullscreenToggle?.value ?? false) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            screen.UseTitleBar = _useTitleBarToggle?.value ?? true;
            screen.KeepTop = _keepTopToggle?.value ?? false;

            appConfig.Save();
            RefreshScreen();
            UpdateScreenInfo();

            Debug.Log("[UNIDebugger] 显示设置已应用");
        }

        private void UpdateWindowModeVisibility(bool isFullscreen)
        {
            _useTitleBarToggle?.SetEnabled(!isFullscreen);
            _keepTopToggle?.SetEnabled(!isFullscreen);
            _posXField?.SetEnabled(!isFullscreen);
            _posYField?.SetEnabled(!isFullscreen);

            if (_useTitleBarToggle != null)
                _useTitleBarToggle.style.opacity = isFullscreen ? 0.5f : 1f;
            if (_keepTopToggle != null)
                _keepTopToggle.style.opacity = isFullscreen ? 0.5f : 1f;
        }

        private void UpdateScreenInfo()
        {
            var res = Screen.currentResolution;
            if (_screenResolutionLabel != null)
                _screenResolutionLabel.text = $"当前分辨率: {res.width}x{res.height}";
            if (_screenRefreshRateLabel != null)
                _screenRefreshRateLabel.text = $"刷新率: {res.refreshRateRatio.value:F0} Hz";
        }

        private void LoadAppSettings()
        {
            var appConfig = Managements.Config.Get<AppConfig>();
            if (appConfig == null)
                return;

            _longTimeNoOperationTimeoutField?.SetValueWithoutNotify(appConfig.LongTimeNoOperationTimeout);
            _checkDesktopResolutionIntervalField?.SetValueWithoutNotify(appConfig.CheckDesktopResolutionInterval);
            _resetPrimaryScreenIntervalField?.SetValueWithoutNotify(appConfig.ResetPrimaryScreenInterval);
        }

        private void ApplyAppSettings()
        {
            var appConfig = Managements.Config.Get<AppConfig>();
            if (appConfig == null)
                return;

            appConfig.LongTimeNoOperationTimeout = _longTimeNoOperationTimeoutField?.value ?? 300f;
            appConfig.CheckDesktopResolutionInterval = _checkDesktopResolutionIntervalField?.value ?? 5f;
            appConfig.ResetPrimaryScreenInterval = _resetPrimaryScreenIntervalField?.value ?? 0f;

            appConfig.Save();
            Debug.Log("[UNIDebugger] 应用设置已保存");
        }

        private void RefreshScreen()
        {
            var appConfig = Managements.Config.Get<AppConfig>();
            if (appConfig == null)
                return;

            var primaryScreen = appConfig.PrimaryScreen.ShallowCopy();
            var minWindowSize = new Vector2(800, 600);

            if (primaryScreen.Width < minWindowSize.x || primaryScreen.Height < minWindowSize.y)
            {
                var mainWidth = Display.main.systemWidth;
                var mainHeight = Display.main.systemHeight;
                primaryScreen.Width = Mathf.Max((int)minWindowSize.x, primaryScreen.Width);
                primaryScreen.Height = Mathf.Max((int)minWindowSize.y, primaryScreen.Height);

                if ((mainWidth - primaryScreen.PosX) < primaryScreen.Width)
                    primaryScreen.PosX = 0;
                if ((mainHeight - primaryScreen.PosY) < primaryScreen.Height)
                    primaryScreen.PosY = 0;
            }

            appConfig.SetScreen(primaryScreen);
        }

        #endregion

        #region System Info

        private void UpdateSystemInfo()
        {
            if (_deviceModelLabel != null)
                _deviceModelLabel.text = $"设备型号: {SystemInfo.deviceModel}";
            if (_deviceTypeLabel != null)
                _deviceTypeLabel.text = $"设备类型: {SystemInfo.deviceType}";
            if (_osLabel != null)
                _osLabel.text = $"操作系统: {SystemInfo.operatingSystem}";
            if (_cpuLabel != null)
                _cpuLabel.text = $"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} 核)";
            if (_gpuLabel != null)
                _gpuLabel.text = $"GPU: {SystemInfo.graphicsDeviceName}";
            if (_memoryLabel != null)
                _memoryLabel.text = $"系统内存: {SystemInfo.systemMemorySize} MB";
            if (_graphicsMemoryLabel != null)
                _graphicsMemoryLabel.text = $"显存: {SystemInfo.graphicsMemorySize} MB";
        }

        #endregion

        #region Runtime Info

        private void UpdateRuntimeInfo()
        {
            _frameCount++;
            float timeNow = Time.realtimeSinceStartup;

            if (timeNow > _lastFpsUpdateTime + _fpsUpdateInterval)
            {
                _currentFps = _frameCount / (timeNow - _lastFpsUpdateTime);
                _frameCount = 0;
                _lastFpsUpdateTime = timeNow;

                if (_fpsLabel != null)
                    _fpsLabel.text = $"FPS: {_currentFps:F0}";
                if (_frameTimeLabel != null)
                    _frameTimeLabel.text = $"帧时间: {(1000f / _currentFps):F1}ms";
            }

            if (Time.frameCount % 30 == 0)
            {
                var totalMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
                var usedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);

                if (_usedMemoryLabel != null)
                    _usedMemoryLabel.text = $"内存: {usedMemory}/{totalMemory}MB";
            }

            var runTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
            if (_runTimeLabel != null)
                _runTimeLabel.text = $"运行: {runTime:hh\\:mm\\:ss}";
        }

        #endregion

        #region Helpers

        private string GetUNIHperVersion()
        {
            // 尝试从 package.json 读取版本号
            try
            {
                var packagePath = "Packages/com.parful.unihper/package.json";
                var packageJson = Resources.Load<TextAsset>(packagePath);
                if (packageJson != null)
                {
                    // 简单解析版本号
                    var text = packageJson.text;
                    var versionIndex = text.IndexOf("\"version\"");
                    if (versionIndex > 0)
                    {
                        var start = text.IndexOf("\"", versionIndex + 10) + 1;
                        var end = text.IndexOf("\"", start);
                        return text.Substring(start, end - start);
                    }
                }
            }
            catch { }

            return "1.0.0";
        }

        #endregion
    }
}
