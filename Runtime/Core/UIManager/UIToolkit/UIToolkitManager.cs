using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace UNIHper.UI
{
    /// <summary>
    /// UI Toolkit 管理器
    /// 管理基于 UIDocument 的 UI 页面
    /// </summary>
    public class UIToolkitManager : Singleton<UIToolkitManager>
    {
        #region Constants

        internal const string PERSISTENCE_SCENE = "Persistence";

        // UIDocument sortingOrder 基准值（按类型分层，与 UIManager 的 Transform 层级对应）
        private const int SORTING_ORDER_STANDALONE = 100;
        private const int SORTING_ORDER_NORMAL = 1000;
        private const int SORTING_ORDER_POPUP = 10000;

        #endregion

        #region Private Fields

        // 所有已注册的 UI 配置
        private readonly Dictionary<string, UIToolkitPageConfig> _registeredConfigs = new Dictionary<string, UIToolkitPageConfig>();

        // 所有已实例化的 UI
        private readonly Dictionary<string, UIToolkitBase> _spawnedUIs = new Dictionary<string, UIToolkitBase>();

        // 页面类型追踪器（与 UIManager 共享逻辑）
        private readonly UIPageTracker<UIToolkitBase> _pageTracker = new UIPageTracker<UIToolkitBase>();

        // 按类型分组的容器节点（对应 UIManager 的 StandaloneUIRoot / NormalUIRoot / PopupUIRoot）
        private Transform _standaloneRoot;
        private Transform _normalRoot;
        private Transform _popupRoot;

        // 根容器
        private Transform _uiContainer;

        #endregion

        #region Global Events - 使用 UIEventBus

        private readonly UIEventBus<UIToolkitBase> _eventBus = new UIEventBus<UIToolkitBase>();

        /// <summary>
        /// 全局 UI 开始显示事件
        /// </summary>
        public IObservable<UIToolkitBase> OnUIShowingAsObservable() => _eventBus.OnUIShowingAsObservable();

        /// <summary>
        /// 全局 UI 完全显示事件
        /// </summary>
        public IObservable<UIToolkitBase> OnUIShownAsObservable() => _eventBus.OnUIShownAsObservable();

        /// <summary>
        /// 全局 UI 开始隐藏事件
        /// </summary>
        public IObservable<UIToolkitBase> OnUIHidingAsObservable() => _eventBus.OnUIHidingAsObservable();

        /// <summary>
        /// 全局 UI 完全隐藏事件
        /// </summary>
        public IObservable<UIToolkitBase> OnUIHiddenAsObservable() => _eventBus.OnUIHiddenAsObservable();

        /// <summary>
        /// 监听指定类型 UI 的显示事件
        /// </summary>
        public IObservable<T> OnUIShowingAsObservable<T>()
            where T : UIToolkitBase => _eventBus.OnUIShowingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI 的显示完成事件
        /// </summary>
        public IObservable<T> OnUIShownAsObservable<T>()
            where T : UIToolkitBase => _eventBus.OnUIShownAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI 的隐藏事件
        /// </summary>
        public IObservable<T> OnUIHidingAsObservable<T>()
            where T : UIToolkitBase => _eventBus.OnUIHidingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI 的隐藏完成事件
        /// </summary>
        public IObservable<T> OnUIHiddenAsObservable<T>()
            where T : UIToolkitBase => _eventBus.OnUIHiddenAsObservable<T>();

        internal void NotifyUIShowing(UIToolkitBase ui) => _eventBus.NotifyShowing(ui);

        internal void NotifyUIShown(UIToolkitBase ui) => _eventBus.NotifyShown(ui);

        internal void NotifyUIHiding(UIToolkitBase ui) => _eventBus.NotifyHiding(ui);

        internal void NotifyUIHidden(UIToolkitBase ui) => _eventBus.NotifyHidden(ui);

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化 UI Toolkit 管理器
        /// </summary>
        public void Initialize()
        {
            // 创建 UI 容器（含类型分组子节点）
            EnsureUIContainer();

            // 扫描并注册所有 UI Toolkit 页面
            ScanAndRegisterUIPages();

            Debug.Log($"[UIToolkitManager] 初始化完成，注册了 {_registeredConfigs.Count} 个 UI 页面");
        }

        private void EnsureUIContainer()
        {
            var containerGO = GameObject.Find("[UIToolkit Container]");
            if (containerGO == null)
            {
                containerGO = new GameObject("[UIToolkit Container]");
                GameObject.DontDestroyOnLoad(containerGO);
            }
            _uiContainer = containerGO.transform;

            // 创建类型分组子节点（与 UIManager 的 StandaloneUIRoot/NormalUIRoot/PopupUIRoot 对应）
            _standaloneRoot = EnsureChildContainer("StandaloneUIRoot");
            _normalRoot = EnsureChildContainer("NormalUIRoot");
            _popupRoot = EnsureChildContainer("PopupUIRoot");
        }

        private Transform EnsureChildContainer(string name)
        {
            var child = _uiContainer.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(_uiContainer);
            }
            return child;
        }

        private void ScanAndRegisterUIPages()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(UIToolkitBase).IsAssignableFrom(t));

                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<UIToolkitPage>();
                        if (attr != null)
                        {
                            var config = new UIToolkitPageConfig
                            {
                                UIKey = type.FullName,
                                Asset = string.IsNullOrEmpty(attr.Asset) ? type.Name : attr.Asset,
                                ShowType = attr.Type,
                                Order = attr.Order,
                                InstID = attr.InstID,
                                Scene = attr.Scene,
                                PanelSettings = attr.PanelSettings,
                                ClassType = type
                            };

                            _registeredConfigs[config.UIKey] = config;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略无法加载的程序集
                }
            }
        }

        #endregion

        #region Scene Lifecycle

        /// <summary>
        /// 场景切换时调用（对应 UIManager.OnEnterScene）
        /// 清理非持久化 UI，创建新场景的 UI
        /// </summary>
        internal void OnEnterScene(string sceneName)
        {
            // 清理非持久化的已实例化 UI
            var allKeys = _spawnedUIs.Keys.ToList();
            foreach (var uiKey in allKeys)
            {
                if (IsPersistUI(uiKey))
                    continue;

                if (_spawnedUIs.TryGetValue(uiKey, out var ui))
                {
                    if (ui.isShowing)
                    {
                        ui.HandleHide();
                    }
                    _pageTracker.Untrack(uiKey, ui);
                    _spawnedUIs.Remove(uiKey);

                    if (ui != null && ui.gameObject != null)
                    {
                        GameObject.Destroy(ui.gameObject);
                    }
                }
            }

            // 预创建新场景配置的 UI（Scene 匹配的 UI）
            foreach (var kvp in _registeredConfigs)
            {
                if (kvp.Value.Scene == sceneName && !_spawnedUIs.ContainsKey(kvp.Key))
                {
                    CreateUI(kvp.Key);
                }
            }
        }

        /// <summary>
        /// 清理所有资源（应用退出时调用，对应 UIManager.CleanUp）
        /// </summary>
        internal void CleanUp()
        {
            HideAll();

            foreach (var ui in _spawnedUIs.Values.ToList())
            {
                if (ui != null && ui.gameObject != null)
                {
                    GameObject.Destroy(ui.gameObject);
                }
            }

            _spawnedUIs.Clear();
            _pageTracker.Clear();
            _eventBus.Dispose();
        }

        /// <summary>
        /// 判断是否为持久化 UI
        /// </summary>
        private bool IsPersistUI(string uiKey)
        {
            if (_registeredConfigs.TryGetValue(uiKey, out var config))
            {
                return string.IsNullOrEmpty(config.Scene) || config.Scene == PERSISTENCE_SCENE;
            }
            return true; // 未注册的 UI 默认为持久化（安全策略）
        }

        #endregion

        #region Show/Hide Methods

        /// <summary>
        /// 显示 UI（泛型版本）
        /// </summary>
        public T Show<T>(bool bForceNotify = false)
            where T : UIToolkitBase
        {
            return Show(typeof(T).FullName, bForceNotify) as T;
        }

        /// <summary>
        /// 显示 UI（按 Key）— 根据 UIType 执行不同的页面管理逻辑
        /// </summary>
        public UIToolkitBase Show(string uiKey, bool bForceNotify = false)
        {
            var ui = GetOrCreate(uiKey);
            if (ui == null)
                return null;

            // 防重入：已显示则不重复执行（与 UIManager 行为一致）
            if (ui.isShowing)
            {
                Debug.LogWarning($"[UIToolkitManager] UI {uiKey} is already showing.");
                if (bForceNotify)
                    ui.ForceInvokeOnShownEvent();
                return ui;
            }

            _pageTracker.TrackShow(
                uiKey,
                ui,
                handleShow: u => u.HandleShow(),
                handleHide: u => u.HandleHide(),
                onPopupSortOrder: (u, depth) =>
                {
                    var uiDoc = u.GetComponent<UIDocument>();
                    if (uiDoc != null)
                    {
                        uiDoc.sortingOrder = SORTING_ORDER_POPUP + depth;
                    }
                }
            );

            return ui;
        }

        /// <summary>
        /// 隐藏 UI（泛型版本）
        /// </summary>
        public T Hide<T>(bool bForceNotify = false)
            where T : UIToolkitBase
        {
            return Hide(typeof(T).FullName, bForceNotify) as T;
        }

        /// <summary>
        /// 隐藏 UI（按 Key）— 根据 UIType 执行不同的页面管理逻辑
        /// </summary>
        public UIToolkitBase Hide(string uiKey, bool bForceNotify = false)
        {
            if (!_spawnedUIs.TryGetValue(uiKey, out var ui))
            {
                Debug.LogWarning($"[UIToolkitManager] Hide ui {uiKey} failed. UI not exists.");
                return null;
            }

            // 防重入：已隐藏则不重复执行（与 UIManager 行为一致）
            if (!ui.isShowing)
            {
                Debug.LogWarning($"[UIToolkitManager] UI {uiKey} is already hidden.");
                if (bForceNotify)
                    ui.ForceInvokeOnHiddenEvent();
                return ui;
            }

            _pageTracker.TrackHide(uiKey, ui, handleHide: u => u.HandleHide(), handleShow: u => u.HandleShow());

            return ui;
        }

        /// <summary>
        /// 隐藏最顶层的 Popup
        /// </summary>
        public UIToolkitBase HideTopPopup()
        {
            return _pageTracker.HideTopPopup(u => u.HandleHide());
        }

        /// <summary>
        /// 隐藏所有 UI
        /// </summary>
        public void HideAll()
        {
            foreach (var ui in _spawnedUIs.Values.ToList())
            {
                if (ui != null && ui.isShowing)
                {
                    ui.HandleHide();
                }
            }
            _pageTracker.Clear();
        }

        /// <summary>
        /// 隐藏所有指定类型的 UI
        /// </summary>
        public void HideAll<T>()
            where T : UIToolkitBase
        {
            GetAll<T>().ForEach(ui => Hide(ui.Key));
        }

        /// <summary>
        /// 显示所有指定类型的 UI
        /// </summary>
        public void ShowAll<T>()
            where T : UIToolkitBase
        {
            GetAll<T>().ForEach(ui => Show(ui.Key));
        }

        /// <summary>
        /// 暂存当前所有活跃 UI
        /// </summary>
        public void StashActiveUI()
        {
            _pageTracker.StashActiveUI(uiKey => Hide(uiKey));
        }

        /// <summary>
        /// 恢复暂存的 UI
        /// </summary>
        public void PopStashedUI()
        {
            _pageTracker.PopStashedUI(uiKey => Show(uiKey));
        }

        /// <summary>
        /// 当前所有活跃（显示中）的 UI 列表
        /// </summary>
        public List<UIToolkitBase> ActiveUIs => _spawnedUIs.Values.Where(ui => ui != null && ui.isShowing).ToList();

        #endregion

        #region Get Methods

        /// <summary>
        /// 获取 UI 实例（泛型版本）
        /// </summary>
        public T Get<T>()
            where T : UIToolkitBase
        {
            return Get(typeof(T).FullName) as T;
        }

        /// <summary>
        /// 获取 UI 实例（按 Key）
        /// </summary>
        public UIToolkitBase Get(string uiKey)
        {
            _spawnedUIs.TryGetValue(uiKey, out var ui);
            return ui;
        }

        /// <summary>
        /// 获取所有指定类型的 UI 实例（对应 UIManager.GetAll）
        /// </summary>
        public List<T> GetAll<T>()
            where T : UIToolkitBase
        {
            return _spawnedUIs.Values.Where(ui => ui is T).Cast<T>().ToList();
        }

        /// <summary>
        /// 检查 UI 是否存在（对应 UIManager.Exists）
        /// </summary>
        public bool Exists<T>()
            where T : UIToolkitBase
        {
            return Get<T>() != null;
        }

        /// <summary>
        /// 获取或创建 UI 实例
        /// </summary>
        public UIToolkitBase GetOrCreate(string uiKey)
        {
            if (_spawnedUIs.TryGetValue(uiKey, out var existingUI))
            {
                return existingUI;
            }

            return CreateUI(uiKey);
        }

        /// <summary>
        /// 检查 UI 是否正在显示
        /// </summary>
        public bool IsShowing<T>()
            where T : UIToolkitBase
        {
            var ui = Get<T>();
            return ui != null && ui.isShowing;
        }

        /// <summary>
        /// 检查 UI 是否正在显示（按 Type）
        /// </summary>
        public bool IsShowing(Type uiType)
        {
            var ui = Get(uiType.FullName);
            return ui != null && ui.isShowing;
        }

        /// <summary>
        /// 显示 UI（按 Type）
        /// </summary>
        public UIToolkitBase Show(Type uiType, bool bForceNotify = false)
        {
            return Show(uiType.FullName, bForceNotify);
        }

        /// <summary>
        /// 隐藏 UI（按 Type）
        /// </summary>
        public UIToolkitBase Hide(Type uiType, bool bForceNotify = false)
        {
            return Hide(uiType.FullName, bForceNotify);
        }

        /// <summary>
        /// 获取 UI 实例（按 Type）
        /// </summary>
        public UIToolkitBase Get(Type uiType)
        {
            return Get(uiType.FullName);
        }

        /// <summary>
        /// 切换 UI 显示/隐藏
        /// </summary>
        public T Toggle<T>()
            where T : UIToolkitBase
        {
            return Toggle(typeof(T).FullName) as T;
        }

        /// <summary>
        /// 切换 UI 显示/隐藏（按 Key）
        /// </summary>
        public UIToolkitBase Toggle(string uiKey)
        {
            var ui = GetOrCreate(uiKey);
            if (ui == null)
                return null;

            // 直接判断并调用管理器级 Show/Hide，避免 ui.Toggle() 回调造成循环
            if (ui.isShowing)
            {
                Hide(uiKey);
            }
            else
            {
                Show(uiKey);
            }
            return ui;
        }

        #endregion

        #region Create/Destroy Methods

        private UIToolkitBase CreateUI(string uiKey)
        {
            if (!_registeredConfigs.TryGetValue(uiKey, out var config))
            {
                Debug.LogError($"[UIToolkitManager] 未找到 UI 配置: {uiKey}");
                return null;
            }

            // 加载 UXML 资源
            var uxmlAsset = ResourceManager.Instance.Get<VisualTreeAsset>(config.Asset);

            if (uxmlAsset == null)
            {
                uxmlAsset = Resources.Load<VisualTreeAsset>(config.Asset);
            }

            if (uxmlAsset == null)
            {
                uxmlAsset = Resources.Load<VisualTreeAsset>($"UIToolkit/{config.Asset}");
            }

            if (uxmlAsset == null)
            {
                Debug.LogError($"[UIToolkitManager] 无法加载 UXML 资源: {config.Asset}");
                return null;
            }

            // 根据 UIType 选择父容器
            var parentRoot = GetParentForType(config.ShowType);

            // 创建 GameObject 并挂到对应类型容器下
            var go = new GameObject(config.ClassType.Name);
            go.transform.SetParent(parentRoot);

            // 添加 UIDocument 组件
            var uiDocument = go.AddComponent<UIDocument>();

            // 设置 Panel Settings
            if (!string.IsNullOrEmpty(config.PanelSettings))
            {
                var panelSettings = ResourceManager.Instance.Get<PanelSettings>(config.PanelSettings);
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                }
            }
            else
            {
                uiDocument.panelSettings = GetOrCreateDefaultPanelSettings();
            }

            // 根据 UIType 设置基准 sortingOrder（类型分层）
            uiDocument.sortingOrder = GetBaseSortingOrder(config.ShowType) + config.Order;

            // 设置 UXML
            uiDocument.visualTreeAsset = uxmlAsset;

            // 添加 UI 脚本组件
            var uiComponent = go.AddComponent(config.ClassType) as UIToolkitBase;
            if (uiComponent == null)
            {
                Debug.LogError($"[UIToolkitManager] 无法创建 UI 组件: {config.ClassType.Name}");
                GameObject.Destroy(go);
                return null;
            }

            // 设置内部属性
            uiComponent.__UIKey = uiKey;
            uiComponent.__Type = config.ShowType;
            uiComponent.__InstanceID = config.InstID;

            // 初始状态隐藏
            if (uiComponent.Root != null)
            {
                uiComponent.Root.style.display = DisplayStyle.None;
            }

            // 缓存
            _spawnedUIs[uiKey] = uiComponent;

            // 调用 OnLoad 生命周期（对应 UIManager.SpawnUI 中的 OnLoad 调用）
            uiComponent.OnLoad();

            Debug.Log($"[UIToolkitManager] 创建 UI: {config.ClassType.Name} (Type={config.ShowType}, Parent={parentRoot.name})");

            return uiComponent;
        }

        /// <summary>
        /// 根据 UIType 获取父容器
        /// </summary>
        private Transform GetParentForType(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.Standalone:
                    return _standaloneRoot;
                case UIType.Normal:
                    return _normalRoot;
                case UIType.Popup:
                    return _popupRoot;
                default:
                    return _normalRoot;
            }
        }

        /// <summary>
        /// 根据 UIType 获取基准 sortingOrder
        /// </summary>
        private int GetBaseSortingOrder(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.Standalone:
                    return SORTING_ORDER_STANDALONE;
                case UIType.Normal:
                    return SORTING_ORDER_NORMAL;
                case UIType.Popup:
                    return SORTING_ORDER_POPUP;
                default:
                    return SORTING_ORDER_NORMAL;
            }
        }

        private PanelSettings _defaultPanelSettings;

        private PanelSettings GetOrCreateDefaultPanelSettings()
        {
            if (_defaultPanelSettings != null)
            {
                return _defaultPanelSettings;
            }

            // 1. 优先从 UIToolkitConfig（UNIHperSettings）获取
            _defaultPanelSettings = UIToolkitConfig.GetDefaultPanelSettings();
            if (_defaultPanelSettings != null)
            {
                if (UNIHperSettings.ShowDebugLog)
                {
                    Debug.Log("[UIToolkitManager] 使用 UNIHperSettings 中的默认 PanelSettings");
                }
                return _defaultPanelSettings;
            }

            // 2. 尝试从 ResourceManager 加载
            _defaultPanelSettings = ResourceManager.Instance.Get<PanelSettings>("DefaultPanelSettings");
            if (_defaultPanelSettings != null)
            {
                return _defaultPanelSettings;
            }

            Debug.LogWarning("[UIToolkitManager] 未找到默认 PanelSettings，请运行 UNIHper/Initialize 初始化框架资源。");

            // 3. 运行时创建（功能受限）
            _defaultPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _defaultPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _defaultPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
            _defaultPanelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _defaultPanelSettings.match = 0.5f;
            _defaultPanelSettings.sortingOrder = 1000;

            return _defaultPanelSettings;
        }

        /// <summary>
        /// 销毁 UI 实例
        /// </summary>
        public void Destroy<T>(bool immediate = false)
            where T : UIToolkitBase
        {
            Destroy(typeof(T).FullName, immediate);
        }

        /// <summary>
        /// 销毁 UI 实例（按 Key），支持等待过渡完成（对应 UIManager.Destroy）
        /// </summary>
        public async void Destroy(string uiKey, bool immediate = false)
        {
            if (_spawnedUIs.TryGetValue(uiKey, out var ui))
            {
                if (ui.isShowing)
                {
                    Hide(uiKey);
                }

                _pageTracker.Untrack(uiKey, ui);
                _spawnedUIs.Remove(uiKey);

                if (!immediate && ui != null)
                {
                    await ui.WaitForTransitionComplete(0);
                }

                if (ui != null && ui.gameObject != null)
                {
                    GameObject.Destroy(ui.gameObject);
                }
            }
        }

        /// <summary>
        /// 销毁所有指定类型的 UI（对应 UIManager.DestroyAll）
        /// </summary>
        public void DestroyAll<T>(bool immediate = false)
            where T : UIToolkitBase
        {
            GetAll<T>().ForEach(ui => Destroy(ui.Key, immediate));
        }

        /// <summary>
        /// 销毁所有 UI
        /// </summary>
        public void DestroyAll()
        {
            foreach (var ui in _spawnedUIs.Values.ToList())
            {
                if (ui != null && ui.gameObject != null)
                {
                    GameObject.Destroy(ui.gameObject);
                }
            }
            _spawnedUIs.Clear();
            _pageTracker.Clear();
        }

        #endregion
    }

    /// <summary>
    /// UI Toolkit 页面配置（内部使用）
    /// </summary>
    internal class UIToolkitPageConfig
    {
        public string UIKey;
        public string Asset;
        public UIType ShowType;
        public int Order;
        public int InstID;
        public string Scene;
        public string PanelSettings;
        public Type ClassType;
    }
}
