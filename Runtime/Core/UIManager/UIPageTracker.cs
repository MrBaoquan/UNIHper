using System;
using System.Collections.Generic;
using System.Linq;

namespace UNIHper.UI
{
    /// <summary>
    /// UI 页面类型追踪器
    /// 提供 Normal/Standalone/Popup 三种类型的统一管理逻辑
    /// 供 UIManager 和 UIToolkitManager 共享
    /// </summary>
    /// <typeparam name="T">UI 组件类型，必须实现 IUIComponent</typeparam>
    internal class UIPageTracker<T>
        where T : class, IUIComponent
    {
        // 按类型分类的已激活 UI
        private readonly Dictionary<string, T> _activatedNormalUIs = new Dictionary<string, T>();
        private readonly Dictionary<string, T> _activatedStandaloneUIs = new Dictionary<string, T>();
        private readonly List<T> _activatedPopupUIs = new List<T>();

        // UI 暂存
        private readonly List<T> _stashedUIs = new List<T>();
        private bool _isStashing = false;

        /// <summary>
        /// 是否正在暂存状态
        /// </summary>
        public bool IsStashing => _isStashing;

        /// <summary>
        /// 已激活的 Normal UI
        /// </summary>
        public IReadOnlyDictionary<string, T> ActivatedNormalUIs => _activatedNormalUIs;

        /// <summary>
        /// 已激活的 Standalone UI
        /// </summary>
        public IReadOnlyDictionary<string, T> ActivatedStandaloneUIs => _activatedStandaloneUIs;

        /// <summary>
        /// 已激活的 Popup UI（栈）
        /// </summary>
        public IReadOnlyList<T> ActivatedPopupUIs => _activatedPopupUIs;

        /// <summary>
        /// 根据 UIType 执行对应的 Show 追踪逻辑
        /// </summary>
        /// <param name="uiKey">UI 键</param>
        /// <param name="ui">UI 实例</param>
        /// <param name="handleShow">执行实际显示的回调</param>
        /// <param name="handleHide">执行实际隐藏的回调</param>
        /// <param name="onPopupSortOrder">Popup 设置层级的回调（传入栈深度）</param>
        /// <param name="standaloneGroupKey">Standalone 分组键（如 CanvasKey），为 null 则全局分组</param>
        public void TrackShow(
            string uiKey,
            T ui,
            Action<T> handleShow,
            Action<T> handleHide,
            Action<T, int> onPopupSortOrder = null,
            string standaloneGroupKey = null
        )
        {
            switch (ui.Type)
            {
                case UIType.Normal:
                    ShowNormal(uiKey, ui, handleShow);
                    break;
                case UIType.Standalone:
                    ShowStandalone(uiKey, ui, handleShow, handleHide, standaloneGroupKey);
                    break;
                case UIType.Popup:
                    ShowPopup(uiKey, ui, handleShow, onPopupSortOrder);
                    break;
                default:
                    handleShow(ui);
                    break;
            }
        }

        /// <summary>
        /// 根据 UIType 执行对应的 Hide 追踪逻辑
        /// </summary>
        /// <param name="uiKey">UI 键</param>
        /// <param name="ui">UI 实例</param>
        /// <param name="handleHide">执行实际隐藏的回调</param>
        /// <param name="handleShow">执行实际显示的回调（用于 Standalone 恢复）</param>
        /// <param name="standaloneGroupKey">Standalone 分组键（如 CanvasKey），为 null 则全局分组</param>
        public void TrackHide(string uiKey, T ui, Action<T> handleHide, Action<T> handleShow = null, string standaloneGroupKey = null)
        {
            switch (ui.Type)
            {
                case UIType.Normal:
                    HideNormal(uiKey, ui, handleHide);
                    break;
                case UIType.Standalone:
                    HideStandalone(uiKey, ui, handleHide, handleShow, standaloneGroupKey);
                    break;
                case UIType.Popup:
                    HidePopup(uiKey, ui, handleHide);
                    break;
                default:
                    handleHide(ui);
                    break;
            }
        }

        #region Normal

        private void ShowNormal(string uiKey, T ui, Action<T> handleShow)
        {
            handleShow(ui);
            if (!_activatedNormalUIs.ContainsKey(uiKey))
            {
                _activatedNormalUIs[uiKey] = ui;
            }
        }

        private void HideNormal(string uiKey, T ui, Action<T> handleHide)
        {
            handleHide(ui);
            _activatedNormalUIs.Remove(uiKey);
        }

        #endregion

        #region Standalone

        /// <summary>
        /// Standalone 分组键获取委托
        /// 允许外部注入分组逻辑（如 UIManager 按 CanvasKey 分组）
        /// </summary>
        public Func<T, string> GetStandaloneGroupKey { get; set; }

        private string ResolveGroupKey(T ui, string explicitGroupKey)
        {
            if (explicitGroupKey != null)
                return explicitGroupKey;
            if (GetStandaloneGroupKey != null)
                return GetStandaloneGroupKey(ui);
            return "__default"; // 无分组键则全局分组
        }

        private void ShowStandalone(string uiKey, T ui, Action<T> handleShow, Action<T> handleHide, string groupKey)
        {
            var resolvedGroupKey = ResolveGroupKey(ui, groupKey);

            // 隐藏同分组的其他已激活 Standalone UI
            foreach (var kvp in _activatedStandaloneUIs.ToList())
            {
                if (kvp.Key != uiKey && kvp.Value.isShowing)
                {
                    var otherGroupKey = ResolveGroupKey(kvp.Value, null);
                    if (otherGroupKey == resolvedGroupKey)
                    {
                        handleHide(kvp.Value);
                    }
                }
            }

            handleShow(ui);

            if (!_activatedStandaloneUIs.ContainsKey(uiKey))
            {
                _activatedStandaloneUIs[uiKey] = ui;
            }
        }

        private void HideStandalone(string uiKey, T ui, Action<T> handleHide, Action<T> handleShow, string groupKey)
        {
            var resolvedGroupKey = ResolveGroupKey(ui, groupKey);

            handleHide(ui);
            _activatedStandaloneUIs.Remove(uiKey);

            // 恢复同分组的上一个 Standalone UI
            if (handleShow != null)
            {
                var last = _activatedStandaloneUIs.Values
                    .Where(u => u != ui && ResolveGroupKey(u, null) == resolvedGroupKey)
                    .LastOrDefault();
                if (last != null && !last.isShowing)
                {
                    handleShow(last);
                }
            }
        }

        #endregion

        #region Popup

        private void ShowPopup(string uiKey, T ui, Action<T> handleShow, Action<T, int> onPopupSortOrder)
        {
            // 避免重复入栈，移到栈顶
            _activatedPopupUIs.Remove(ui);
            _activatedPopupUIs.Add(ui);

            // 设置层级
            onPopupSortOrder?.Invoke(ui, _activatedPopupUIs.Count);

            handleShow(ui);
        }

        private void HidePopup(string uiKey, T ui, Action<T> handleHide)
        {
            handleHide(ui);
            _activatedPopupUIs.Remove(ui);
        }

        /// <summary>
        /// 隐藏最顶层的 Popup
        /// </summary>
        public T HideTopPopup(Action<T> handleHide)
        {
            if (_activatedPopupUIs.Count == 0)
                return null;

            var top = _activatedPopupUIs[_activatedPopupUIs.Count - 1];
            _activatedPopupUIs.RemoveAt(_activatedPopupUIs.Count - 1);
            handleHide(top);
            return top;
        }

        #endregion

        #region Stash / Pop

        /// <summary>
        /// 暂存当前所有活跃 UI
        /// </summary>
        /// <param name="hideAction">用于隐藏每个 UI 的回调（应调用管理器的 Hide 方法以走完整流程）</param>
        public void StashActiveUI(Action<string> hideAction)
        {
            if (_isStashing)
                return;
            _isStashing = true;

            _stashedUIs.Clear();

            var normalUIs = _activatedNormalUIs.Values.Where(ui => ui.isShowing).ToList();
            var standaloneUIs = _activatedStandaloneUIs.Values.Where(ui => ui.isShowing).ToList();
            var popupUIs = _activatedPopupUIs.Where(ui => ui.isShowing).ToList();

            _stashedUIs.AddRange(normalUIs);
            _stashedUIs.AddRange(standaloneUIs);
            _stashedUIs.AddRange(popupUIs);

            foreach (var ui in _stashedUIs)
            {
                hideAction(ui.Key);
            }
        }

        /// <summary>
        /// 恢复暂存的 UI
        /// </summary>
        /// <param name="showAction">用于显示每个 UI 的回调（应调用管理器的 Show 方法以走完整流程）</param>
        public void PopStashedUI(Action<string> showAction)
        {
            if (!_isStashing)
                return;

            foreach (var ui in _stashedUIs)
            {
                showAction(ui.Key);
            }

            _stashedUIs.Clear();
            _isStashing = false;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清除所有追踪数据
        /// </summary>
        public void Clear()
        {
            _activatedNormalUIs.Clear();
            _activatedStandaloneUIs.Clear();
            _activatedPopupUIs.Clear();
            _stashedUIs.Clear();
            _isStashing = false;
        }

        /// <summary>
        /// 从所有追踪列表中移除指定 UI
        /// </summary>
        public void Untrack(string uiKey, T ui)
        {
            _activatedNormalUIs.Remove(uiKey);
            _activatedStandaloneUIs.Remove(uiKey);
            _activatedPopupUIs.Remove(ui);
        }

        #endregion
    }
}
