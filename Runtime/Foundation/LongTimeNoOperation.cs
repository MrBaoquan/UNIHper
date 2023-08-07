using System;
using UnityEngine.Events;
using UniRx;

/// <summary>
/// 长时间无操作，响应回调
/// </summary>

namespace UNIHper
{
    public class LongTimeNoOperation
    {
        public UnityEvent OnLongTimeNoOperationEvent = new UnityEvent();
        public UnityEvent OnResetOperationEvent = new UnityEvent();

        public IObservable<Unit> OnResetOperationAsObservable() =>
            OnResetOperationEvent.AsObservable();

        public void ResetOperation()
        {
            OnResetOperationEvent.Invoke();
            timeoutEvent.Invoke();
        }

        public LongTimeNoOperation(float limitTime, Action callback = null)
        {
            LimitTime = limitTime;
            OnLongTimeNoOperationAsObservable()
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                    OnLongTimeNoOperationEvent.Invoke();
                });
            timeoutEvent.Invoke();
        }

        public IObservable<Unit> OnLongTimeNoOperationAsObservable()
        {
            return Observable
                .FromEvent<UnityAction>(
                    h => new UnityAction(h),
                    h => timeoutEvent.AddListener(h),
                    h => timeoutEvent.RemoveListener(h)
                )
                .Throttle(TimeSpan.FromSeconds(LimitTime));
        }

        private UnityEvent timeoutEvent = new UnityEvent();
        private float LimitTime = 60f;
    }
}
