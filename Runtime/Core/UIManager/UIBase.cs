using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UNIHper.UI;

namespace UNIHper
{
    public abstract partial class UIBase : MonoBehaviour
    {
        internal string __CanvasKey = string.Empty;
        protected string __UIKey = string.Empty;
        protected UIType __Type = UIType.Normal;
        public UIType Type
        {
            get { return __Type; }
        }

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

        public bool isShowing { get; private set; } = false;

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

        private Func<Task> m_showTask = () => Task.CompletedTask;

        public void SetShowTask(Func<Task> InTask)
        {
            m_showTask = InTask;
        }

        private Func<Task> m_hideTask = () => Task.CompletedTask;

        public void SetHideTask(Func<Task> InTask)
        {
            m_hideTask = InTask;
        }

        private UIAnimationBase uiAnimComponent
        {
            get => GetComponent<UIAnimationBase>();
        }

        // Called when the ui is loaded
        protected void OnLoad()
        {
            if (uiAnimComponent != null)
                UReflection.CallPrivateMethod(uiAnimComponent, "OnUIAttached");
            OnLoaded();
        }

        // Called when the ui is being requested to show
        protected void HandleShow()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                this.gameObject.SetActive(true);
            }
            isShowing = true;
            handleShowEvents();
        }

        protected async void handleShowEvents()
        {
            this.OnShowing();
            await handleShowAction();
            this.OnShown();
            onShownEvent.Invoke();
        }

        // Called when the ui is being requested to hide
        protected void HandleHide()
        {
            if (!isShowing)
                return;
            isShowing = false;
            this.handleHideEvents();
        }

        protected async void handleHideEvents()
        {
            this.OnHiding();
            await handleHideAction();
            this.OnHidden();
            onHiddenEvent.Invoke();
            if (!isShowing && this.gameObject)
                this.gameObject.SetActive(false);
        }

        protected async virtual Task handleShowAction()
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

        protected async virtual Task handleHideAction()
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
