using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using DNHper;

namespace UNIHper.UI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class UIPage : Attribute
    {
        internal string UIKey;

        /// <summary>
        /// The name of the ui asset.
        /// </summary>
        public string Asset;

        /// <summary>
        /// The type of the ui to show.
        /// </summary>
        public UIType Type;

        /// <summary>
        /// The order of the ui in the canvas.
        /// </summary>
        public int Order = -1;

        /// <summary>
        /// Instance ID of the ui.
        /// </summary>
        public int InstID = 0;

        /// <summary>
        /// The name of the canvas to render the ui.
        /// </summary>
        public string Canvas;

        /// <summary>
        /// The name of the scene to load the ui, default is "Persistence".
        /// </summary>
        public string Scene;

        public UIPage()
        {
            this.Asset = string.Empty;
            this.Canvas = UIManager.CANVAS_DEFAULT;
            this.Type = UIType.Normal;
            this.Scene = UIManager.PERSISTENCE_SCENE;
        }
    }

    public abstract partial class UIBase : MonoBehaviour
    {
        internal string __CanvasKey = string.Empty;
        internal string __UIKey = string.Empty;
        internal UIType __Type = UIType.Normal;
        internal int __InstanceID = -1;

        public UIType Type => __Type;
        public string Key => __UIKey;
        public int InstID => __InstanceID;
        public float ShowDuration { get; protected set; } = 0.0f;

        public IObservable<Unit> WaitForTransitionComplete(float offset = -0.1f)
        {
            var _delay = 0f;
            if (_state == UIState.Showing)
            {
                _delay = ShowDuration + offset;
            }
            else if (_state == UIState.Hiding)
            {
                _delay = HideDuration + offset;
            }

            _delay = Mathf.Max(0, _delay);
            return Observable.Timer(TimeSpan.FromSeconds(_delay)).AsUnitObservable();
        }

        public float HideDuration { get; protected set; } = 0.0f;

        // 控制animator是否自动复位
        public bool RebindAnimator { get; set; } = true;

        /// <summary>
        /// 跟随UI显示/隐藏的Disposable集合
        /// </summary>
        public CompositeDisposable LifeCycleDisposables { get; private set; } = null;

        private UnityEvent onShowingEvent = new UnityEvent();

        private UnityEvent onShownEvent = new UnityEvent();

        internal void ForceInvokeOnShownEvent()
        {
            if (_state == UIState.Shown)
                onShownEvent.Invoke();
        }

        public IObservable<Unit> OnShowingAsObservable()
        {
            return onShowingEvent.AsObservable();
        }

        public IObservable<Unit> OnShownAsObservable()
        {
            return onShownEvent.AsObservable();
        }

        private UnityEvent onHidingEvent = new UnityEvent();

        private UnityEvent onHiddenEvent = new UnityEvent();

        internal void ForceInvokeOnHiddenEvent()
        {
            if (_state == UIState.Hidden)
                onHiddenEvent.Invoke();
        }

        public IObservable<Unit> OnHidingAsObservable()
        {
            return onHidingEvent.AsObservable();
        }

        public IObservable<Unit> OnHiddenAsObservable()
        {
            return onHiddenEvent.AsObservable();
        }

        public enum UIState
        {
            None,
            Loading,
            Loaded,
            Showing,
            Shown,
            Hiding,
            Hidden
        }

        private UIState _state = UIState.None;
        public bool isShowing => _state == UIState.Showing || _state == UIState.Shown;
        public UIState State => _state;

        public int LastShowFrame { get; private set; } = int.MaxValue;

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

        public bool CheckState(UIState state)
        {
            return _state == state;
        }

        public bool IsShown()
        {
            return _state == UIState.Shown;
        }

        public bool IsHidden()
        {
            return _state == UIState.Hidden;
        }

        private UIAnimationBase uiAnimComponent
        {
            get => GetComponent<UIAnimationBase>();
        }

        // Called when the ui is loaded
        internal void OnLoad()
        {
            uiAnimComponent?.OnUIAttached();
            _state = UIState.Loaded;
            OnLoaded();
        }

        // Called when the ui is being requested to show
        internal void HandleShow()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                this.gameObject.SetActive(true);
            }

            handleShowEvents();
        }

        CancellationTokenSource showOrHideCancellationTokenSource = null;

        public void StopTransition()
        {
            clearShowOrHideCancellationTokenSource();
        }

        private void clearShowOrHideCancellationTokenSource()
        {
            if (showOrHideCancellationTokenSource != null)
            {
                showOrHideCancellationTokenSource.Cancel();
                showOrHideCancellationTokenSource.Dispose();
                showOrHideCancellationTokenSource = null;
            }
        }

        protected async void handleShowEvents()
        {
            clearShowOrHideCancellationTokenSource();

            LifeCycleDisposables?.Dispose();
            LifeCycleDisposables = new CompositeDisposable();

            _state = UIState.Showing;
            this.OnShowing();
            onShowingEvent.Invoke();
            try
            {
                showOrHideCancellationTokenSource = new CancellationTokenSource();
                await handleShowAction(showOrHideCancellationTokenSource.Token);
                showOrHideCancellationTokenSource.Dispose();
                showOrHideCancellationTokenSource = null;
            }
            catch (System.Exception)
            {
                return;
            }

            _state = UIState.Shown;
            this.OnShown();
            onShownEvent.Invoke();
        }

        // Called when the ui is being requested to hide
        internal void HandleHide()
        {
            this.handleHideEvents();
        }

        protected async void handleHideEvents()
        {
            clearShowOrHideCancellationTokenSource();

            _state = UIState.Hiding;
            this.OnHiding();
            onHidingEvent.Invoke();
            try
            {
                showOrHideCancellationTokenSource = new CancellationTokenSource();
                await handleHideAction(showOrHideCancellationTokenSource.Token);
                showOrHideCancellationTokenSource.Dispose();
                showOrHideCancellationTokenSource = null;
            }
            catch (System.Exception)
            {
                return;
            }

            if (RebindAnimator)
            {
                transform.GetComponentsInChildren<Animator>().ForEach(_animator => _animator.Rebind());
            }

            _state = UIState.Hidden;
            this.gameObject.SetActive(false);
            this.OnHidden();
            onHiddenEvent.Invoke();
            LifeCycleDisposables?.Dispose();
        }

        protected async virtual Task handleShowAction(CancellationToken cancellationToken)
        {
            if (uiAnimComponent != null)
            {
                ShowDuration = uiAnimComponent.ShowDuration;
                await uiAnimComponent.BuildShowTask(cancellationToken);
            }
            else
            {
                ShowDuration = 0.0f;
                await Task.CompletedTask;
            }
        }

        protected async virtual Task handleHideAction(CancellationToken cancellationToken)
        {
            if (uiAnimComponent != null)
            {
                HideDuration = uiAnimComponent.HideDuration;
                await uiAnimComponent.BuildHideTask(cancellationToken);
            }
            else
            {
                HideDuration = 0.0f;
                await Task.CompletedTask;
            }
        }

        protected virtual void OnLoaded() { }

        protected virtual void OnShowing() { }

        protected virtual void OnShown() { }

        protected virtual void OnHiding() { }

        protected virtual void OnHidden() { }
    }
}
