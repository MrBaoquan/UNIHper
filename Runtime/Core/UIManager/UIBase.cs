using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UNIHper.UI;
using System.Threading;

namespace UNIHper
{
    public abstract partial class UIBase : MonoBehaviour
    {
        internal string __CanvasKey = string.Empty;
        internal string __UIKey = string.Empty;
        internal UIType __Type = UIType.Normal;
        public UIType Type => __Type;
        public string Key => __UIKey;

        private UnityEvent onShownEvent = new UnityEvent();

        public IObservable<Unit> OnShowAsObservable()
        {
            return onShownEvent.AsObservable();
        }

        private UnityEvent onHiddenEvent = new UnityEvent();

        public IObservable<Unit> OnHideAsObservable()
        {
            return onHiddenEvent.AsObservable();
        }

        private enum UIStatus
        {
            None,
            Loading,
            Loaded,
            Showing,
            Shown,
            Hiding,
            Hidden
        }

        private UIStatus _status = UIStatus.None;
        public bool isShowing => _status == UIStatus.Showing || _status == UIStatus.Shown;

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

        private UIAnimationBase uiAnimComponent
        {
            get => GetComponent<UIAnimationBase>();
        }

        // Called when the ui is loaded
        internal void OnLoad()
        {
            if (uiAnimComponent != null)
                UReflection.CallPrivateMethod(uiAnimComponent, "OnUIAttached");
            _status = UIStatus.Loaded;
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

        CancellationTokenSource showCancellationTokenSource = null;
        CancellationTokenSource hideCancellationTokenSource = null;

        private void clearShowOrHideCancellationTokenSource()
        {
            if (showCancellationTokenSource != null)
            {
                showCancellationTokenSource.Cancel();
                showCancellationTokenSource.Dispose();
                showCancellationTokenSource = null;
            }
            if (hideCancellationTokenSource != null)
            {
                hideCancellationTokenSource.Cancel();
                hideCancellationTokenSource.Dispose();
                hideCancellationTokenSource = null;
            }
        }

        protected async void handleShowEvents()
        {
            clearShowOrHideCancellationTokenSource();

            _status = UIStatus.Showing;
            this.OnShowing();
            try
            {
                showCancellationTokenSource = new CancellationTokenSource();
                await handleShowAction(showCancellationTokenSource.Token);
                showCancellationTokenSource.Dispose();
                showCancellationTokenSource = null;
            }
            catch (System.Exception)
            {
                return;
            }

            _status = UIStatus.Shown;
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

            _status = UIStatus.Hiding;
            this.OnHiding();
            try
            {
                hideCancellationTokenSource = new CancellationTokenSource();
                await handleHideAction(hideCancellationTokenSource.Token);
                hideCancellationTokenSource.Dispose();
                hideCancellationTokenSource = null;
            }
            catch (System.Exception)
            {
                return;
            }

            _status = UIStatus.Hidden;

            this.gameObject.SetActive(false);
            this.OnHidden();
            onHiddenEvent.Invoke();
        }

        protected async virtual Task handleShowAction(CancellationToken cancellationToken)
        {
            if (uiAnimComponent != null)
            {
                await uiAnimComponent.BuildShowTask();
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        protected async virtual Task handleHideAction(CancellationToken cancellationToken)
        {
            if (uiAnimComponent != null)
            {
                await uiAnimComponent.BuildHideTask();
            }
            else
            {
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
