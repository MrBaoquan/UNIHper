using System;
using UniRx;

namespace UNIHper.UI
{
    /// <summary>
    /// UI 组件公共接口
    /// 统一 UGUI (UIBase) 和 UI Toolkit (UIToolkitBase) 的访问方式
    /// </summary>
    public interface IUIComponent
    {
        /// <summary>
        /// UI 唯一键
        /// </summary>
        string Key { get; }

        /// <summary>
        /// UI 类型 (Normal/Fixed/Popup)
        /// </summary>
        UIType Type { get; }

        /// <summary>
        /// 实例 ID（用于多实例场景）
        /// </summary>
        int InstID { get; }

        /// <summary>
        /// 是否正在显示（Showing 或 Shown 状态）
        /// </summary>
        bool isShowing { get; }

        /// <summary>
        /// 当前 UI 状态
        /// </summary>
        UIState State { get; }

        /// <summary>
        /// 显示动画时长
        /// </summary>
        float ShowDuration { get; }

        /// <summary>
        /// 隐藏动画时长
        /// </summary>
        float HideDuration { get; }

        /// <summary>
        /// 跟随 UI 生命周期的 Disposable 集合
        /// </summary>
        CompositeDisposable LifeCycleDisposables { get; }

        /// <summary>
        /// UI 开始显示时的 Observable
        /// </summary>
        IObservable<Unit> OnShowingAsObservable();

        /// <summary>
        /// UI 完全显示后的 Observable
        /// </summary>
        IObservable<Unit> OnShownAsObservable();

        /// <summary>
        /// UI 开始隐藏时的 Observable
        /// </summary>
        IObservable<Unit> OnHidingAsObservable();

        /// <summary>
        /// UI 完全隐藏后的 Observable
        /// </summary>
        IObservable<Unit> OnHiddenAsObservable();

        /// <summary>
        /// 等待过渡动画完成
        /// </summary>
        /// <param name="offset">时间偏移量（负值表示提前触发）</param>
        IObservable<Unit> WaitForTransitionComplete(float offset = -0.1f);

        /// <summary>
        /// 切换显示/隐藏状态
        /// </summary>
        void Toggle();

        /// <summary>
        /// 停止当前过渡动画
        /// </summary>
        void StopTransition();
    }

    /// <summary>
    /// UI 状态枚举
    /// </summary>
    public enum UIState
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None,

        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 正在显示（播放显示动画中）
        /// </summary>
        Showing,

        /// <summary>
        /// 已显示
        /// </summary>
        Shown,

        /// <summary>
        /// 正在隐藏（播放隐藏动画中）
        /// </summary>
        Hiding,

        /// <summary>
        /// 已隐藏
        /// </summary>
        Hidden
    }

    // UIType 枚举已在 UIRootLayout.cs 中定义
}
