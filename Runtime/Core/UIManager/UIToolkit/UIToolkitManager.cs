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

        #endregion

        #region Private Fields

        // 所有已注册的 UI 配置
        private Dictionary<string, UIToolkitPageConfig> _registeredConfigs = new Dictionary<string, UIToolkitPageConfig>();

        // 所有已实例化的 UI
        private Dictionary<string, UIToolkitBase> _spawnedUIs = new Dictionary<string, UIToolkitBase>();

        // UI 容器
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
            // 创建 UI 容器
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
        }

        private void ScanAndRegisterUIPages()
        {
            // 获取所有程序集中继承自 UIToolkitBase 的类型
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

        #region Show/Hide Methods

        /// <summary>
        /// 显示 UI（泛型版本）
        /// </summary>
        public T Show<T>()
            where T : UIToolkitBase
        {
            return Show(typeof(T).FullName) as T;
        }

        /// <summary>
        /// 显示 UI（按 Key）
        /// </summary>
        public UIToolkitBase Show(string uiKey)
        {
            var ui = GetOrCreate(uiKey);
            if (ui != null)
            {
                ui.HandleShow();
            }
            return ui;
        }

        /// <summary>
        /// 隐藏 UI（泛型版本）
        /// </summary>
        public T Hide<T>()
            where T : UIToolkitBase
        {
            return Hide(typeof(T).FullName) as T;
        }

        /// <summary>
        /// 隐藏 UI（按 Key）
        /// </summary>
        public UIToolkitBase Hide(string uiKey)
        {
            if (_spawnedUIs.TryGetValue(uiKey, out var ui))
            {
                ui.HandleHide();
                return ui;
            }
            return null;
        }

        /// <summary>
        /// 隐藏所有 UI
        /// </summary>
        public void HideAll()
        {
            foreach (var ui in _spawnedUIs.Values.ToList())
            {
                if (ui.isShowing)
                {
                    ui.HandleHide();
                }
            }
        }

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
        public bool IsShowing<T>(Type uiType)
            where T : UIToolkitBase
        {
            var ui = Get<T>(uiType);
            return ui != null && ui.isShowing;
        }

        /// <summary>
        /// 显示 UI（按 Type）
        /// </summary>
        public T Show<T>(Type uiType)
            where T : UIToolkitBase
        {
            return Show(uiType.FullName) as T;
        }

        /// <summary>
        /// 隐藏 UI（按 Type）
        /// </summary>
        public T Hide<T>(Type uiType)
            where T : UIToolkitBase
        {
            return Hide(uiType.FullName) as T;
        }

        /// <summary>
        /// 获取 UI 实例（按 Type）
        /// </summary>
        public T Get<T>(Type uiType)
            where T : UIToolkitBase
        {
            return Get(uiType.FullName) as T;
        }

        /// <summary>
        /// 切换 UI 显示/隐藏
        /// </summary>
        public T Toggle<T>()
            where T : UIToolkitBase
        {
            var ui = Get<T>();
            if (ui != null)
            {
                ui.Toggle();
            }
            return ui;
        }

        /// <summary>
        /// 切换 UI 显示/隐藏（按 Type）
        /// </summary>
        public T Toggle<T>(Type uiType)
            where T : UIToolkitBase
        {
            var ui = Get<T>(uiType);
            if (ui != null)
            {
                ui.Toggle();
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
                Debug.LogError($"[UIToolkitManager] 无法加载 UXML 资源: {config.Asset}");
                return null;
            }

            // 创建 GameObject
            var go = new GameObject(config.ClassType.Name);
            go.transform.SetParent(_uiContainer);

            // 添加 UIDocument 组件
            var uiDocument = go.AddComponent<UIDocument>();

            // 设置 Panel Settings（如果指定）
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
                // 使用默认 Panel Settings
                uiDocument.panelSettings = GetOrCreateDefaultPanelSettings();
            }

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

            Debug.Log($"[UIToolkitManager] 创建 UI: {config.ClassType.Name}");

            return uiComponent;
        }

        private PanelSettings _defaultPanelSettings;

        private PanelSettings GetOrCreateDefaultPanelSettings()
        {
            if (_defaultPanelSettings != null)
            {
                return _defaultPanelSettings;
            }

            // 尝试从资源加载
            _defaultPanelSettings = ResourceManager.Instance.Get<PanelSettings>("DefaultPanelSettings");

            if (_defaultPanelSettings == null)
            {
                Debug.LogWarning("[UIToolkitManager] 未找到 DefaultPanelSettings 资源，UI Toolkit 可能无法正确显示。请在 Resources 目录创建 PanelSettings 资源。");

                // 运行时创建（功能受限）
                _defaultPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _defaultPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _defaultPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _defaultPanelSettings.match = 0.5f; // Width/Height 平衡

                // 尝试加载默认 Theme（Unity 内置）
                var defaultTheme = Resources.Load<ThemeStyleSheet>("UIPackageResources/UnityThemes/UnityDefaultRuntimeTheme");
                if (defaultTheme != null)
                {
                    _defaultPanelSettings.themeStyleSheet = defaultTheme;
                    Debug.Log("[UIToolkitManager] 已加载 Unity 默认主题");
                }
                else
                {
                    Debug.LogError("[UIToolkitManager] 无法加载默认主题，UI 可能显示异常");
                }
            }

            return _defaultPanelSettings;
        }

        /// <summary>
        /// 销毁 UI 实例
        /// </summary>
        public void Destroy<T>()
            where T : UIToolkitBase
        {
            Destroy(typeof(T).FullName);
        }

        /// <summary>
        /// 销毁 UI 实例（按 Key）
        /// </summary>
        public void Destroy(string uiKey)
        {
            if (_spawnedUIs.TryGetValue(uiKey, out var ui))
            {
                _spawnedUIs.Remove(uiKey);

                if (ui != null && ui.gameObject != null)
                {
                    GameObject.Destroy(ui.gameObject);
                }
            }
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
