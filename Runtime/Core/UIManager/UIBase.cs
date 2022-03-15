using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNIHper {
    public abstract partial class UIBase : MonoBehaviour {
        protected string __UIKey = string.Empty;
        protected UIType __Type = UIType.Normal;
        public UIType Type {
            get {
                return __Type;
            }
        }

        protected bool bShow = false;
        public bool isShowing {
            get { return bShow; }
        }

        protected virtual void OnLoad () { }

        protected void HandleShow () {
            if (!this.gameObject.activeInHierarchy) {
                this.gameObject.SetActive (true);
            }
            bShow = true;
            this.OnShow ();
            handleShowAction ();
        }

        protected void HandleHide () {
            if (!bShow) return;
            bShow = false;
            this.OnHidden ();
            handleHideAction ();
        }

        protected virtual void handleShowAction () {
            this.gameObject.SetActive (true);
        }

        protected virtual void handleHideAction () {
            this.gameObject.SetActive (false);
        }

        protected virtual void OnShow () {

        }

        protected virtual void OnHidden () {

        }
    }

}