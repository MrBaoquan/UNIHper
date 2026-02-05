using System;
using UniRx;

namespace UNIHper.UI
{
    /// <summary>
    /// UI 事件总线 - 通用的 UI 生命周期事件系统
    /// 可被 UIManager 和 UIToolkitManager 共用
    /// </summary>
    /// <typeparam name="TUI">UI 组件类型</typeparam>
    public class UIEventBus<TUI>
        where TUI : class, IUIComponent
    {
        #region Subjects

        private readonly Subject<TUI> _onShowingSubject = new Subject<TUI>();
        private readonly Subject<TUI> _onShownSubject = new Subject<TUI>();
        private readonly Subject<TUI> _onHidingSubject = new Subject<TUI>();
        private readonly Subject<TUI> _onHiddenSubject = new Subject<TUI>();

        #endregion

        #region Observable - 全局事件

        /// <summary>
        /// 全局 UI 开始显示事件
        /// </summary>
        public IObservable<TUI> OnUIShowingAsObservable() => _onShowingSubject.AsObservable();

        /// <summary>
        /// 全局 UI 完全显示事件
        /// </summary>
        public IObservable<TUI> OnUIShownAsObservable() => _onShownSubject.AsObservable();

        /// <summary>
        /// 全局 UI 开始隐藏事件
        /// </summary>
        public IObservable<TUI> OnUIHidingAsObservable() => _onHidingSubject.AsObservable();

        /// <summary>
        /// 全局 UI 完全隐藏事件
        /// </summary>
        public IObservable<TUI> OnUIHiddenAsObservable() => _onHiddenSubject.AsObservable();

        #endregion

        #region Observable - 泛型过滤版本

        /// <summary>
        /// 监听指定类型 UI 的显示事件
        /// </summary>
        public IObservable<T> OnUIShowingAsObservable<T>()
            where T : class, TUI => _onShowingSubject.Where(ui => ui is T).Select(ui => ui as T);

        /// <summary>
        /// 监听指定类型 UI 的显示完成事件
        /// </summary>
        public IObservable<T> OnUIShownAsObservable<T>()
            where T : class, TUI => _onShownSubject.Where(ui => ui is T).Select(ui => ui as T);

        /// <summary>
        /// 监听指定类型 UI 的隐藏事件
        /// </summary>
        public IObservable<T> OnUIHidingAsObservable<T>()
            where T : class, TUI => _onHidingSubject.Where(ui => ui is T).Select(ui => ui as T);

        /// <summary>
        /// 监听指定类型 UI 的隐藏完成事件
        /// </summary>
        public IObservable<T> OnUIHiddenAsObservable<T>()
            where T : class, TUI => _onHiddenSubject.Where(ui => ui is T).Select(ui => ui as T);

        #endregion

        #region Notify Methods

        /// <summary>
        /// 触发 UI 开始显示事件
        /// </summary>
        public void NotifyShowing(TUI ui) => _onShowingSubject.OnNext(ui);

        /// <summary>
        /// 触发 UI 完全显示事件
        /// </summary>
        public void NotifyShown(TUI ui) => _onShownSubject.OnNext(ui);

        /// <summary>
        /// 触发 UI 开始隐藏事件
        /// </summary>
        public void NotifyHiding(TUI ui) => _onHidingSubject.OnNext(ui);

        /// <summary>
        /// 触发 UI 完全隐藏事件
        /// </summary>
        public void NotifyHidden(TUI ui) => _onHiddenSubject.OnNext(ui);

        #endregion

        #region Dispose

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            _onShowingSubject.Dispose();
            _onShownSubject.Dispose();
            _onHidingSubject.Dispose();
            _onHiddenSubject.Dispose();
        }

        #endregion
    }
}
