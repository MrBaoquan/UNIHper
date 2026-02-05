using System;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace UNIHper.UI
{
    /// <summary>
    /// UI Toolkit 全局配置
    /// 优先从 UNIHperSettings 读取配置，也支持运行时动态设置
    /// </summary>
    public static class UIToolkitConfig
    {
        // 运行时覆盖配置（优先级高于 UNIHperSettings）
        private static Font _runtimeFont;
        private static StyleSheet _runtimeStyleSheet;
        private static PanelSettings _runtimePanelSettings;
        private static bool? _runtimeAutoApplyFont;

        /// <summary>
        /// 默认字体（优先使用运行时设置，否则从 UNIHperSettings 读取）
        /// </summary>
        public static Font DefaultFont
        {
            get => _runtimeFont ?? UNIHperSettings.UIToolkitDefaultFont;
            set => _runtimeFont = value;
        }

        /// <summary>
        /// 默认样式表（优先使用运行时设置，否则从 UNIHperSettings 读取）
        /// </summary>
        public static StyleSheet DefaultStyleSheet
        {
            get => _runtimeStyleSheet ?? UNIHperSettings.UIToolkitDefaultStyleSheet;
            set => _runtimeStyleSheet = value;
        }

        /// <summary>
        /// 默认 PanelSettings（优先使用运行时设置，否则从 UNIHperSettings 读取）
        /// </summary>
        public static PanelSettings DefaultPanelSettings
        {
            get => _runtimePanelSettings ?? UNIHperSettings.UIToolkitPanelSettings;
            set => _runtimePanelSettings = value;
        }

        /// <summary>
        /// 是否自动应用默认字体到所有文本元素
        /// </summary>
        public static bool AutoApplyDefaultFont
        {
            get => _runtimeAutoApplyFont ?? UNIHperSettings.UIToolkitAutoApplyFont;
            set => _runtimeAutoApplyFont = value;
        }

        /// <summary>
        /// [已废弃] 使用 DefaultFont 属性代替
        /// </summary>
        [Obsolete("使用 DefaultFont 属性代替")]
        public static string DefaultFontName { get; set; } = string.Empty;

        // 自动加载的备用字体缓存
        private static Font _fallbackFont;

        /// <summary>
        /// 获取默认字体（带自动备用加载）
        /// </summary>
        public static Font GetDefaultFont()
        {
            var font = DefaultFont;

            // 如果 UNIHperSettings 中没有配置，尝试从 Resources 加载
            if (font == null)
            {
                if (_fallbackFont == null)
                {
                    // 尝试多个可能的字体路径
                    _fallbackFont = Managements.Resource.Get<Font>("Fonts/AlibabaPuHuiTi-2-55-Regular");
                }
                font = _fallbackFont;
            }

            if (font == null)
            {
                Debug.LogWarning("[UIToolkitConfig] 未找到默认字体，中文可能无法显示。请将字体放入 Resources/Fonts/ 目录或在 UNIHperSettings 中配置");
            }

            return font;
        }

        /// <summary>
        /// 获取默认样式表
        /// </summary>
        public static StyleSheet GetDefaultStyleSheet()
        {
            return DefaultStyleSheet;
        }

        /// <summary>
        /// 获取默认 PanelSettings
        /// </summary>
        public static PanelSettings GetDefaultPanelSettings()
        {
            return DefaultPanelSettings;
        }

        /// <summary>
        /// 清除运行时缓存（场景切换时调用）
        /// </summary>
        public static void ClearRuntimeCache()
        {
            _runtimeFont = null;
            _runtimeStyleSheet = null;
            _runtimePanelSettings = null;
            _runtimeAutoApplyFont = null;
            _fallbackFont = null;
        }

        /// <summary>
        /// [已废弃] 使用 ClearRuntimeCache 代替
        /// </summary>
        [Obsolete("使用 ClearRuntimeCache 代替")]
        public static void ClearFontCache() => ClearRuntimeCache();
    }

    /// <summary>
    /// UI Toolkit 特性标注
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class UIToolkitPage : Attribute
    {
        internal string UIKey;

        /// <summary>
        /// UXML 资源名称
        /// </summary>
        public string Asset;

        /// <summary>
        /// UI 类型
        /// </summary>
        public UIType Type = UIType.Normal;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order = -1;

        /// <summary>
        /// 实例 ID
        /// </summary>
        public int InstID = 0;

        /// <summary>
        /// 所属场景，默认 "Persistence"
        /// </summary>
        public string Scene = UIManager.PERSISTENCE_SCENE;

        /// <summary>
        /// Panel Settings 资源名称（可选）
        /// </summary>
        public string PanelSettings;

        public UIToolkitPage()
        {
            this.Asset = string.Empty;
            this.Type = UIType.Normal;
            this.Scene = UIManager.PERSISTENCE_SCENE;
        }
    }

    /// <summary>
    /// UI Toolkit 基类
    /// 类似 UIBase，但基于 UIDocument 和 VisualElement
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIToolkitBase : MonoBehaviour, IUIComponent
    {
        #region Internal Fields

        internal string __UIKey = string.Empty;
        internal UIType __Type = UIType.Normal;
        internal int __InstanceID = -1;

        #endregion

        #region Public Properties

        public UIType Type => __Type;
        public string Key => __UIKey;
        public int InstID => __InstanceID;

        /// <summary>
        /// UIDocument 组件
        /// </summary>
        public UIDocument Document { get; private set; }

        /// <summary>
        /// 根 VisualElement
        /// </summary>
        public VisualElement Root => Document?.rootVisualElement;

        /// <summary>
        /// 显示动画时长
        /// </summary>
        public float ShowDuration { get; protected set; } = 0.3f;

        /// <summary>
        /// 隐藏动画时长
        /// </summary>
        public float HideDuration { get; protected set; } = 0.3f;

        /// <summary>
        /// 是否正在显示
        /// </summary>
        public bool isShowing => _state == UIState.Showing || _state == UIState.Shown;

        /// <summary>
        /// 当前状态
        /// </summary>
        public UIState State => _state;

        #endregion

        #region State

        // UIState 已移至 IUIComponent.cs 中统一定义

        private UIState _state = UIState.None;

        #endregion

        #region Lifecycle Disposables

        /// <summary>
        /// 跟随 UI 显示/隐藏的 Disposable 集合
        /// </summary>
        public CompositeDisposable LifeCycleDisposables { get; private set; }

        #endregion

        #region Events

        private UnityEvent onShowingEvent = new UnityEvent();
        private UnityEvent onShownEvent = new UnityEvent();
        private UnityEvent onHidingEvent = new UnityEvent();
        private UnityEvent onHiddenEvent = new UnityEvent();

        public IObservable<Unit> OnShowingAsObservable() => onShowingEvent.AsObservable();

        public IObservable<Unit> OnShownAsObservable() => onShownEvent.AsObservable();

        public IObservable<Unit> OnHidingAsObservable() => onHidingEvent.AsObservable();

        public IObservable<Unit> OnHiddenAsObservable() => onHiddenEvent.AsObservable();

        public IObservable<Unit> WaitForTransitionComplete(float offset = -0.1f)
        {
            var delay = 0f;
            if (_state == UIState.Showing)
            {
                delay = ShowDuration + offset;
            }
            else if (_state == UIState.Hiding)
            {
                delay = HideDuration + offset;
            }

            delay = Mathf.Max(0, delay);
            return Observable.Timer(TimeSpan.FromSeconds(delay)).AsUnitObservable();
        }

        #endregion

        #region Font & Style Methods

        private bool _fontApplied = false;
        private bool _styleSheetApplied = false;

        /// <summary>
        /// 应用默认样式表（包含字体设置）
        /// </summary>
        protected virtual void ApplyDefaultStyleSheet()
        {
            if (_styleSheetApplied || Root == null)
                return;

            var styleSheet = UIToolkitConfig.GetDefaultStyleSheet();
            if (styleSheet != null)
            {
                Root.styleSheets.Add(styleSheet);
                _styleSheetApplied = true;
                if (UNIHperSettings.ShowDebugLog)
                {
                    Debug.Log($"[UIToolkitBase] {GetType().Name}: 应用默认样式表");
                }
            }
        }

        /// <summary>
        /// 应用默认字体到所有文本元素
        /// </summary>
        protected virtual void ApplyDefaultFont()
        {
            if (_fontApplied)
            {
                return;
            }

            if (!UIToolkitConfig.AutoApplyDefaultFont)
            {
                Debug.Log($"[UIToolkitBase] {GetType().Name}: AutoApplyDefaultFont 已禁用");
                return;
            }

            var font = UIToolkitConfig.GetDefaultFont();
            if (font == null)
            {
                // 尝试从 Resources 加载字体作为备选
                font = Resources.Load<Font>("Fonts/AlibabaPuHuiTi-2-55-Regular");
                if (font == null)
                {
                    Debug.LogWarning($"[UIToolkitBase] {GetType().Name}: 未找到默认字体，请在 UNIHperSettings 中配置或将字体放入 Resources/Fonts/ 目录");
                    return;
                }
                Debug.Log($"[UIToolkitBase] {GetType().Name}: 从 Resources 加载备用字体");
            }

            Debug.Log($"[UIToolkitBase] {GetType().Name}: 应用默认字体 {font.name}");
            ApplyFontToElement(Root, font);
            _fontApplied = true;
        }

        /// <summary>
        /// 递归应用字体到元素及其子元素
        /// </summary>
        protected void ApplyFontToElement(VisualElement element, Font font)
        {
            if (element == null || font == null)
                return;

            // UI Toolkit 推荐使用 FontDefinition 来设置字体
            var fontDefinition = FontDefinition.FromFont(font);

            // 同时设置 unityFont 和 unityFontDefinition 以确保兼容性
            element.style.unityFont = new StyleFont(font);
            element.style.unityFontDefinition = new StyleFontDefinition(fontDefinition);

            // 递归处理子元素
            foreach (var child in element.Children())
            {
                ApplyFontToElement(child, font);
            }
        }

        /// <summary>
        /// 手动设置自定义字体（覆盖默认字体）
        /// </summary>
        protected void SetCustomFont(Font font)
        {
            if (font == null || Root == null)
                return;

            ApplyFontToElement(Root, font);
            _fontApplied = true;
        }

        /// <summary>
        /// 使用资源名称设置自定义字体
        /// </summary>
        protected void SetCustomFont(string fontResourceName)
        {
            var font = Managements.Resource.Get<Font>(fontResourceName);
            if (font != null)
            {
                SetCustomFont(font);
            }
            else
            {
                Debug.LogWarning($"[UIToolkitBase] 无法加载字体资源: {fontResourceName}");
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// 查询单个元素（类似 document.querySelector）
        /// </summary>
        /// <typeparam name="T">VisualElement 类型</typeparam>
        /// <param name="name">元素名称（可选）</param>
        /// <param name="className">CSS 类名（可选）</param>
        /// <returns>找到的元素，或 null</returns>
        protected T Q<T>(string name = null, string className = null)
            where T : VisualElement
        {
            return Root?.Q<T>(name, className);
        }

        /// <summary>
        /// 查询单个元素（按名称）
        /// </summary>
        protected VisualElement Q(string name = null, string className = null)
        {
            return Root?.Q(name, className);
        }

        /// <summary>
        /// 查询所有匹配的元素（类似 document.querySelectorAll）
        /// </summary>
        protected UQueryBuilder<T> QAll<T>(string name = null, string className = null)
            where T : VisualElement
        {
            return Root.Query<T>(name, className);
        }

        /// <summary>
        /// 获取按钮并绑定点击事件
        /// </summary>
        protected Button BindButton(string name, Action onClick)
        {
            var button = Q<Button>(name);
            if (button != null && onClick != null)
            {
                button.clicked += onClick;
            }
            return button;
        }

        /// <summary>
        /// 获取 TextField 并绑定值变化事件
        /// </summary>
        protected TextField BindTextField(string name, Action<string> onValueChanged)
        {
            var textField = Q<TextField>(name);
            if (textField != null && onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }
            return textField;
        }

        /// <summary>
        /// 获取 Toggle 并绑定值变化事件
        /// </summary>
        protected Toggle BindToggle(string name, Action<bool> onValueChanged)
        {
            var toggle = Q<Toggle>(name);
            if (toggle != null && onValueChanged != null)
            {
                toggle.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }
            return toggle;
        }

        /// <summary>
        /// 获取 Slider 并绑定值变化事件
        /// </summary>
        protected Slider BindSlider(string name, Action<float> onValueChanged)
        {
            var slider = Q<Slider>(name);
            if (slider != null && onValueChanged != null)
            {
                slider.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }
            return slider;
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            Document = GetComponent<UIDocument>();
        }

        #endregion

        #region Show/Hide Implementation

        private CancellationTokenSource _transitionCts;

        /// <summary>
        /// 停止当前过渡动画
        /// </summary>
        public void StopTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }

        /// <summary>
        /// 内部方法：处理显示
        /// </summary>
        internal void HandleShow()
        {
            if (Root == null)
            {
                Debug.LogWarning($"[UIToolkitBase] {GetType().Name}: Root is null in HandleShow");
                return;
            }

            // 应用默认样式表（包含字体设置）
            ApplyDefaultStyleSheet();

            // 应用默认字体（作为备选方案）
            ApplyDefaultFont();

            // 确保可见
            Root.style.display = DisplayStyle.Flex;

            // 延迟再次应用字体，确保所有动态元素都已构建
            Root.schedule
                .Execute(() =>
                {
                    if (_fontApplied)
                    {
                        // 重新应用一次，确保所有元素都被覆盖
                        var font = UIToolkitConfig.GetDefaultFont() ?? Resources.Load<Font>("Fonts/AlibabaPuHuiTi-2-55-Regular");
                        if (font != null)
                        {
                            ApplyFontToElement(Root, font);
                        }
                    }
                })
                .ExecuteLater(50); // 延迟 50ms 确保 UI 完全构建

            handleShowEvents();
        }

        /// <summary>
        /// 内部方法：处理隐藏
        /// </summary>
        internal void HandleHide()
        {
            if (Root == null)
                return;

            handleHideEvents();
        }

        protected async void handleShowEvents()
        {
            StopTransition();

            LifeCycleDisposables?.Dispose();
            LifeCycleDisposables = new CompositeDisposable();

            _state = UIState.Showing;
            OnShowing();
            onShowingEvent.Invoke();

            // 触发全局事件
            UIToolkitManager.Instance?.NotifyUIShowing(this);

            try
            {
                _transitionCts = new CancellationTokenSource();
                await HandleShowAnimation(_transitionCts.Token);
                _transitionCts.Dispose();
                _transitionCts = null;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _state = UIState.Shown;
            OnShown();
            onShownEvent.Invoke();

            UIToolkitManager.Instance?.NotifyUIShown(this);
        }

        protected async void handleHideEvents()
        {
            StopTransition();

            _state = UIState.Hiding;
            OnHiding();
            onHidingEvent.Invoke();

            UIToolkitManager.Instance?.NotifyUIHiding(this);

            try
            {
                _transitionCts = new CancellationTokenSource();
                await HandleHideAnimation(_transitionCts.Token);
                _transitionCts.Dispose();
                _transitionCts = null;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 隐藏元素
            Root.style.display = DisplayStyle.None;

            _state = UIState.Hidden;
            OnHidden();
            onHiddenEvent.Invoke();

            UIToolkitManager.Instance?.NotifyUIHidden(this);

            LifeCycleDisposables?.Dispose();
        }

        #endregion

        #region Animation (Override in subclass)

        /// <summary>
        /// 显示动画（子类可重写）
        /// 默认使用淡入效果
        /// </summary>
        protected virtual async Task HandleShowAnimation(CancellationToken cancellationToken)
        {
            // 默认淡入动画
            Root.style.opacity = 0;

            var elapsed = 0f;
            while (elapsed < ShowDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / ShowDuration);
                Root.style.opacity = t;

                await Task.Yield();
            }

            Root.style.opacity = 1;
        }

        /// <summary>
        /// 隐藏动画（子类可重写）
        /// 默认使用淡出效果
        /// </summary>
        protected virtual async Task HandleHideAnimation(CancellationToken cancellationToken)
        {
            // 默认淡出动画
            Root.style.opacity = 1;

            var elapsed = 0f;
            while (elapsed < HideDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / HideDuration);
                Root.style.opacity = t;

                await Task.Yield();
            }

            Root.style.opacity = 0;
        }

        #endregion

        #region Lifecycle Callbacks (Override in subclass)

        /// <summary>
        /// UI 加载完成时调用（仅一次）
        /// </summary>
        protected virtual void OnLoaded() { }

        /// <summary>
        /// UI 开始显示时调用
        /// </summary>
        protected virtual void OnShowing() { }

        /// <summary>
        /// UI 完全显示后调用
        /// </summary>
        protected virtual void OnShown() { }

        /// <summary>
        /// UI 开始隐藏时调用
        /// </summary>
        protected virtual void OnHiding() { }

        /// <summary>
        /// UI 完全隐藏后调用
        /// </summary>
        protected virtual void OnHidden() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// 显示此 UI
        /// </summary>
        public UIToolkitBase Show()
        {
            UIToolkitManager.Instance?.Show(__UIKey);
            return this;
        }

        /// <summary>
        /// 隐藏此 UI
        /// </summary>
        public UIToolkitBase Hide()
        {
            UIToolkitManager.Instance?.Hide(__UIKey);
            return this;
        }

        /// <summary>
        /// 切换显示/隐藏
        /// </summary>
        public void Toggle()
        {
            if (isShowing)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        #endregion
    }
}
