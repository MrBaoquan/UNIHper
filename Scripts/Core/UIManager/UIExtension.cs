using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
namespace UHelper {

    public abstract partial class UIBase {

        public void EnableDragMove () {
            if (!this.Contains<ObservableBeginDragTrigger> ()) {
                this.AddComponent<ObservableBeginDragTrigger> ();
            }
            if (!this.Contains<ObservableDragTrigger> ()) {
                this.AddComponent<ObservableDragTrigger> ();
            }

            var _delta = Vector2.zero;
            this.GetComponent<ObservableBeginDragTrigger> ()
                .OnBeginDragAsObservable ()
                .Subscribe (_event => {
                    _delta = _event.position - new Vector2 (transform.position.x, transform.position.y);
                });

            this.GetComponent<ObservableDragTrigger> ()
                .OnDragAsObservable ()
                .Subscribe (_event => {
                    transform.position = _event.position - _delta;
                });
        }

        public void Show (Action<UIBase> InCallback = null) {
            Managements.UI.Show (__UIKey, InCallback);
        }

        public void Hide () {
            Managements.UI.Hide (__UIKey);
        }

    }

}