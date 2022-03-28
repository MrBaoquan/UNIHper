using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UNIHper.UI;

namespace UNIHper {
    public abstract partial class UIBase : MonoBehaviour {

        internal string __CanvasKey = string.Empty;
        protected string __UIKey = string.Empty;
        protected UIType __Type = UIType.Normal;
        public UIType Type {
            get {
                return __Type;
            }
        }

        private UnityEvent m_onShowEvent = new UnityEvent ();
        public IObservable<Unit> OnShowAsObservable () {
            return m_onShowEvent.AsObservable ();
        }
        private UnityEvent m_onHideEvent = new UnityEvent ();
        public IObservable<Unit> OnHideAsObservable () {
            return m_onHideEvent.AsObservable ();
        }

        protected bool bShow = false;
        public bool isShowing {
            get { return bShow; }
        }

        public void Toggle () {
            if (isShowing) {
                Hide ();
            } else {
                Show ();
            }
        }

        private Func<Task> m_showTask = () => Task.CompletedTask;
        public void SetShowTask (Func<Task> InTask) {
            m_showTask = InTask;
        }

        private Func<Task> m_hideTask = () => Task.CompletedTask;
        public void SetHideTask (Func<Task> InTask) {
            m_hideTask = InTask;
        }

        private UIAnimationBase uiAnimComponent {
            get => GetComponent<UIAnimationBase> ();
        }

        // Called when the ui is loaded
        protected void OnLoad () {
            if (uiAnimComponent != null)
                UReflection.CallPrivateMethod (uiAnimComponent, "OnUIAttached");
            OnLoaded ();
        }

        // Called when the ui is being requested to show
        protected void HandleShow () {
            if (!this.gameObject.activeInHierarchy) {
                this.gameObject.SetActive (true);
            }
            bShow = true;
            handleShowEvents ();
        }

        protected async void handleShowEvents () {
            await handleShowAction ();
            this.OnShow ();
            m_onShowEvent.Invoke ();
        }

        // Called when the ui is being requested to hide
        protected void HandleHide () {
            if (!bShow) return;
            bShow = false;
            this.handleHideEvents ();
        }

        protected async void handleHideEvents () {
            await handleHideAction ();
            this.OnHidden ();
            m_onHideEvent.Invoke ();
            if (!bShow)
                this.gameObject.SetActive (false);
        }

        protected async virtual Task handleShowAction () {
            if (uiAnimComponent != null) {
                await uiAnimComponent.BuildShowTask ();
            } else {
                await Task.CompletedTask;
            }
        }

        protected async virtual Task handleHideAction () {
            if (uiAnimComponent != null) {
                await uiAnimComponent.BuildHideTask ();
            } else {
                await Task.CompletedTask;
            }
        }

        protected virtual void OnLoaded () { }

        protected virtual void OnShow () { }

        protected virtual void OnHidden () { }
    }

}