using System;
using UnityEngine;

namespace UNIHper.UI
{
    using UniRx;
    using UniRx.Triggers;

    public abstract partial class UIBase
    {
        public void EnableDragMove()
        {
            if (!this.Contains<ObservableBeginDragTrigger>())
            {
                this.AddComponent<ObservableBeginDragTrigger>();
            }
            if (!this.Contains<ObservableDragTrigger>())
            {
                this.AddComponent<ObservableDragTrigger>();
            }

            var _delta = Vector2.zero;
            this.GetComponent<ObservableBeginDragTrigger>()
                .OnBeginDragAsObservable()
                .Subscribe(_event =>
                {
                    _delta = _event.position - new Vector2(transform.localPosition.x, transform.localPosition.y);
                });

            this.GetComponent<ObservableDragTrigger>()
                .OnDragAsObservable()
                .Subscribe(_event =>
                {
                    transform.localPosition = _event.position - _delta;
                });
        }

        public void Show(Action<UIBase> InCallback = null)
        {
            UIManager.Instance.Show(__UIKey, InCallback);
        }

        public void Hide()
        {
            UIManager.Instance.Hide(__UIKey);
        }
    }
}
