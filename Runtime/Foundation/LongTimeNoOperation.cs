using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UniRx;
using System.Collections.Generic;

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

        public LongTimeNoOperation(float limitTime, Action callback = null, bool autoReset = true)
        {
            LimitTime = limitTime;
            OnLongTimeNoOperationAsObservable()
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                    OnLongTimeNoOperationEvent.Invoke();
                });
            timeoutEvent.Invoke();
            if (autoReset)
                _longTimeNoOperations.Add(this);
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

        private static bool hasAnyInput()
        {
            bool _hasAnyInput = false;
            _hasAnyInput =
#if ENABLE_INPUT_SYSTEM
                Keyboard.current.anyKey.wasPressedThisFrame
                || Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame;
#else
            Input.anyKeyDown;
#endif

            if (!_hasAnyInput)
            {
                // check any touch
#if ENABLE_INPUT_SYSTEM
                if (Touchscreen.current != null)
                {
                    _hasAnyInput = Touchscreen.current.touches
                        .ToList()
                        .Exists(_ => _.press.wasPressedThisFrame);
                }
#else
                _hasAnyInput = Input.touchCount > 0;
#endif
            }

            return _hasAnyInput;
        }

        static readonly List<LongTimeNoOperation> _longTimeNoOperations = new();

        static LongTimeNoOperation()
        {
            Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
                    if (hasAnyInput())
                    {
                        foreach (var _longTimeNoOperation in _longTimeNoOperations)
                        {
                            _longTimeNoOperation.ResetOperation();
                        }
                    }
                });
        }
    }
}
