using System;
using System.Collections.Generic;
using System.Reflection;
using UniRx;
using UnityEngine;

namespace UNIHper.UI
{
    /// <summary>
    /// 统一 UI 门面类
    /// 自动检测 UI 类型（UGUI 或 UI Toolkit）并路由到对应管理器
    /// </summary>
    public class UnifiedUIFacade
    {
        private static UnifiedUIFacade _instance;
        public static UnifiedUIFacade Instance => _instance ??= new UnifiedUIFacade();

        private UIManager _uguiManager => UIManager.Instance;
        private UIToolkitManager _toolkitManager => UIToolkitManager.Instance;

        #region Reflection Cache (性能优化)

        // 缓存泛型方法，避免重复反射
        private static readonly Dictionary<Type, MethodInfo> _showMethodCache = new();
        private static readonly Dictionary<Type, MethodInfo> _hideMethodCache = new();
        private static readonly Dictionary<Type, MethodInfo> _getMethodCache = new();
        private static readonly Dictionary<Type, MethodInfo> _toggleMethodCache = new();
        private static readonly Dictionary<Type, MethodInfo> _destroyMethodCache = new();

        // 基础方法信息（只获取一次）
        private static MethodInfo _showBaseMethod;
        private static MethodInfo _hideBaseMethod;
        private static MethodInfo _getBaseMethod;
        private static MethodInfo _toggleBaseMethod;
        private static MethodInfo _destroyBaseMethod;

        private static MethodInfo GetShowMethod(Type uiType)
        {
            if (!_showMethodCache.TryGetValue(uiType, out var method))
            {
                _showBaseMethod ??= typeof(UIManager).GetMethod("Show", new[] { typeof(bool) });
                method = _showBaseMethod.MakeGenericMethod(uiType);
                _showMethodCache[uiType] = method;
            }
            return method;
        }

        private static MethodInfo GetHideMethod(Type uiType)
        {
            if (!_hideMethodCache.TryGetValue(uiType, out var method))
            {
                _hideBaseMethod ??= typeof(UIManager).GetMethod("Hide", new[] { typeof(bool) });
                method = _hideBaseMethod.MakeGenericMethod(uiType);
                _hideMethodCache[uiType] = method;
            }
            return method;
        }

        private static MethodInfo GetGetMethod(Type uiType)
        {
            if (!_getMethodCache.TryGetValue(uiType, out var method))
            {
                _getBaseMethod ??= typeof(UIManager).GetMethod("Get", new[] { typeof(int) });
                method = _getBaseMethod.MakeGenericMethod(uiType);
                _getMethodCache[uiType] = method;
            }
            return method;
        }

        private static MethodInfo GetToggleMethod(Type uiType)
        {
            if (!_toggleMethodCache.TryGetValue(uiType, out var method))
            {
                _toggleBaseMethod ??= typeof(UIManager).GetMethod("Toggle", Type.EmptyTypes);
                method = _toggleBaseMethod.MakeGenericMethod(uiType);
                _toggleMethodCache[uiType] = method;
            }
            return method;
        }

        private static MethodInfo GetDestroyMethod(Type uiType)
        {
            if (!_destroyMethodCache.TryGetValue(uiType, out var method))
            {
                _destroyBaseMethod ??= typeof(UIManager).GetMethod("Destroy", new[] { typeof(int), typeof(bool) });
                method = _destroyBaseMethod.MakeGenericMethod(uiType);
                _destroyMethodCache[uiType] = method;
            }
            return method;
        }

        #endregion

        #region Show Methods

        /// <summary>
        /// 显示 UI - 自动检测类型（UGUI 或 UI Toolkit）
        /// </summary>
        public T Show<T>(bool bForceNotify = false)
            where T : class, IUIComponent
        {
            var type = typeof(T);

            if (typeof(UIBase).IsAssignableFrom(type))
            {
                return GetShowMethod(type).Invoke(_uguiManager, new object[] { bForceNotify }) as T;
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                return _toolkitManager.Show(type.FullName, bForceNotify) as T;
            }

            Debug.LogWarning($"[UnifiedUIFacade] 未知的 UI 类型: {type.Name}");
            return null;
        }

        /// <summary>
        /// 显示 UI（带实例 ID）- 仅支持 UGUI
        /// </summary>
        public T Show<T>(int instanceID, bool bForceNotify = false)
            where T : UIBase
        {
            return _uguiManager.Show<T>(instanceID, bForceNotify);
        }

        /// <summary>
        /// 显示 UI（按 Key）- 仅支持 UGUI
        /// </summary>
        public UIBase Show(string uiKey, bool bForceNotify = false)
        {
            return _uguiManager.Show(uiKey, bForceNotify);
        }

        #endregion

        #region Hide Methods

        /// <summary>
        /// 隐藏 UI - 自动检测类型（UGUI 或 UI Toolkit）
        /// </summary>
        public T Hide<T>(bool bForceNotify = false)
            where T : class, IUIComponent
        {
            var type = typeof(T);

            if (typeof(UIBase).IsAssignableFrom(type))
            {
                return GetHideMethod(type).Invoke(_uguiManager, new object[] { bForceNotify }) as T;
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                return _toolkitManager.Hide(type.FullName, bForceNotify) as T;
            }

            Debug.LogWarning($"[UnifiedUIFacade] 未知的 UI 类型: {type.Name}");
            return null;
        }

        /// <summary>
        /// 隐藏 UI（带实例 ID）- 仅支持 UGUI
        /// </summary>
        public T Hide<T>(int instanceID, bool bForceNotify = false)
            where T : UIBase
        {
            return _uguiManager.Hide<T>(instanceID, bForceNotify);
        }

        /// <summary>
        /// 隐藏 UI（按 Key）- 仅支持 UGUI
        /// </summary>
        public UIBase Hide(string uiKey, bool bForceNotify = false)
        {
            return _uguiManager.Hide(uiKey, bForceNotify);
        }

        /// <summary>
        /// 隐藏所有 UI（UGUI 和 UI Toolkit）
        /// </summary>
        public void HideAll()
        {
            _uguiManager.HideAll();
            _toolkitManager?.HideAll();
        }

        /// <summary>
        /// 隐藏所有指定类型的 UI
        /// </summary>
        public void HideAll<T>()
            where T : class, IUIComponent
        {
            var type = typeof(T);

            if (typeof(UIBase).IsAssignableFrom(type))
            {
                _uguiManager.HideAll<UIBase>();
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                _toolkitManager?.HideAll();
            }
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// 获取 UI 实例 - 自动检测类型（UGUI 或 UI Toolkit）
        /// </summary>
        public T Get<T>(int instanceID = 0)
            where T : class, IUIComponent
        {
            var type = typeof(T);

            if (typeof(UIBase).IsAssignableFrom(type))
            {
                return GetGetMethod(type).Invoke(_uguiManager, new object[] { instanceID }) as T;
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                return _toolkitManager?.Get(type.FullName) as T;
            }

            Debug.LogWarning($"[UnifiedUIFacade] 未知的 UI 类型: {type.Name}");
            return null;
        }

        /// <summary>
        /// 获取 UI 实例（按 Key）- 仅支持 UGUI
        /// </summary>
        public UIBase Get(string uiKey)
        {
            return _uguiManager.Get(uiKey);
        }

        /// <summary>
        /// 检查 UI 是否存在
        /// </summary>
        public bool Exists<T>(int instanceID = 0)
            where T : class, IUIComponent
        {
            return Get<T>(instanceID) != null;
        }

        #endregion

        #region Toggle/IsShowing Methods

        /// <summary>
        /// 切换 UI 显示/隐藏 - 自动检测类型
        /// </summary>
        public T Toggle<T>()
            where T : class, IUIComponent
        {
            var type = typeof(T);

            if (typeof(UIBase).IsAssignableFrom(type))
            {
                return GetToggleMethod(type).Invoke(_uguiManager, null) as T;
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                return _toolkitManager?.Toggle(type.FullName) as T;
            }

            return null;
        }

        /// <summary>
        /// 检查 UI 是否正在显示 - 自动检测类型
        /// </summary>
        public bool IsShowing<T>(int instanceID = 0)
            where T : class, IUIComponent
        {
            var ui = Get<T>(instanceID);
            return ui != null && ui.isShowing;
        }

        #endregion

        #region UGUI Specific Methods

        /// <summary>
        /// 创建 UI（仅 UGUI）
        /// </summary>
        public T Create<T>(int instanceID = 0)
            where T : UIBase
        {
            return _uguiManager.Create<T>(instanceID);
        }

        /// <summary>
        /// 创建 UI（指定 assetName，仅 UGUI）
        /// </summary>
        public T Create<T>(string assetName, int instanceID = 0, UIType uiType = UIType.Normal)
            where T : UIBase
        {
            return _uguiManager.Create<T>(assetName, instanceID, uiType);
        }

        /// <summary>
        /// 销毁所有指定类型的 UI（仅 UGUI）
        /// </summary>
        public void DestroyAll<T>(bool immediate = false)
            where T : UIBase
        {
            _uguiManager.DestroyAll<T>(immediate);
        }

        /// <summary>
        /// 显示所有指定类型的 UI（仅 UGUI）
        /// </summary>
        public void ShowAll<T>()
            where T : UIBase
        {
            _uguiManager.ShowAll<T>();
        }

        /// <summary>
        /// 销毁 UI - 自动检测类型（UGUI 或 UI Toolkit）
        /// </summary>
        public void Destroy<T>(int instanceID = 0, bool immediate = false)
            where T : class, IUIComponent
        {
            var type = typeof(T);
            if (typeof(UIBase).IsAssignableFrom(type))
            {
                GetDestroyMethod(type).Invoke(_uguiManager, new object[] { instanceID, immediate });
            }
            else if (typeof(UIToolkitBase).IsAssignableFrom(type))
            {
                _toolkitManager?.Destroy(type.FullName, immediate);
            }
        }

        /// <summary>
        /// 暂存当前活动的 UI（UGUI + UIToolkit）
        /// </summary>
        public void StashActiveUI()
        {
            _uguiManager.StashActiveUI();
            _toolkitManager?.StashActiveUI();
        }

        /// <summary>
        /// 恢复暂存的 UI（UGUI + UIToolkit）
        /// </summary>
        public void PopStashedUI()
        {
            _uguiManager.PopStashedUI();
            _toolkitManager?.PopStashedUI();
        }

        /// <summary>
        /// 设置渲染模式（仅 UGUI）
        /// </summary>
        public void SetRenderMode(RenderMode renderMode, string canvasKey = UIManager.CANVAS_DEFAULT)
        {
            _uguiManager.SetRenderMode(renderMode, canvasKey);
        }

        #endregion

        #region Global Events - UGUI

        /// <summary>
        /// 全局 UGUI 开始显示事件
        /// </summary>
        public IObservable<UIBase> OnUIShowingAsObservable() => _uguiManager.OnUIShowingAsObservable();

        /// <summary>
        /// 全局 UGUI 完全显示事件
        /// </summary>
        public IObservable<UIBase> OnUIShownAsObservable() => _uguiManager.OnUIShownAsObservable();

        /// <summary>
        /// 全局 UGUI 开始隐藏事件
        /// </summary>
        public IObservable<UIBase> OnUIHidingAsObservable() => _uguiManager.OnUIHidingAsObservable();

        /// <summary>
        /// 全局 UGUI 完全隐藏事件
        /// </summary>
        public IObservable<UIBase> OnUIHiddenAsObservable() => _uguiManager.OnUIHiddenAsObservable();

        /// <summary>
        /// 监听指定类型 UGUI 的显示事件
        /// </summary>
        public IObservable<T> OnUIShowingAsObservable<T>()
            where T : UIBase => _uguiManager.OnUIShowingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UGUI 的显示完成事件
        /// </summary>
        public IObservable<T> OnUIShownAsObservable<T>()
            where T : UIBase => _uguiManager.OnUIShownAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UGUI 的隐藏事件
        /// </summary>
        public IObservable<T> OnUIHidingAsObservable<T>()
            where T : UIBase => _uguiManager.OnUIHidingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UGUI 的隐藏完成事件
        /// </summary>
        public IObservable<T> OnUIHiddenAsObservable<T>()
            where T : UIBase => _uguiManager.OnUIHiddenAsObservable<T>();

        #endregion

        #region Global Events - UI Toolkit

        /// <summary>
        /// 全局 UI Toolkit 开始显示事件
        /// </summary>
        public IObservable<UIToolkitBase> OnToolkitUIShowingAsObservable() => _toolkitManager?.OnUIShowingAsObservable();

        /// <summary>
        /// 全局 UI Toolkit 完全显示事件
        /// </summary>
        public IObservable<UIToolkitBase> OnToolkitUIShownAsObservable() => _toolkitManager?.OnUIShownAsObservable();

        /// <summary>
        /// 全局 UI Toolkit 开始隐藏事件
        /// </summary>
        public IObservable<UIToolkitBase> OnToolkitUIHidingAsObservable() => _toolkitManager?.OnUIHidingAsObservable();

        /// <summary>
        /// 全局 UI Toolkit 完全隐藏事件
        /// </summary>
        public IObservable<UIToolkitBase> OnToolkitUIHiddenAsObservable() => _toolkitManager?.OnUIHiddenAsObservable();

        /// <summary>
        /// 监听指定类型 UI Toolkit 的显示事件
        /// </summary>
        public IObservable<T> OnToolkitUIShowingAsObservable<T>()
            where T : UIToolkitBase => _toolkitManager?.OnUIShowingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI Toolkit 的显示完成事件
        /// </summary>
        public IObservable<T> OnToolkitUIShownAsObservable<T>()
            where T : UIToolkitBase => _toolkitManager?.OnUIShownAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI Toolkit 的隐藏事件
        /// </summary>
        public IObservable<T> OnToolkitUIHidingAsObservable<T>()
            where T : UIToolkitBase => _toolkitManager?.OnUIHidingAsObservable<T>();

        /// <summary>
        /// 监听指定类型 UI Toolkit 的隐藏完成事件
        /// </summary>
        public IObservable<T> OnToolkitUIHiddenAsObservable<T>()
            where T : UIToolkitBase => _toolkitManager?.OnUIHiddenAsObservable<T>();

        #endregion

        #region Dialog Methods (UGUI)

        /// <summary>
        /// 显示提示框
        /// </summary>
        public void ShowAlert(string content, Action onConfirm = null)
        {
            _uguiManager.ShowAlert(content, onConfirm);
        }

        /// <summary>
        /// 显示确认框
        /// </summary>
        public void ShowConfirmPanel(string content, Action onConfirm = null, Action onCancel = null)
        {
            _uguiManager.ShowConfirmPanel(content, onConfirm, onCancel);
        }

        /// <summary>
        /// 隐藏确认框
        /// </summary>
        public void HideConfirmPanel()
        {
            _uguiManager.HideConfirmPanel();
        }

        #endregion
    }
}
