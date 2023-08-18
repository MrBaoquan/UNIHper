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
        public IObservable<Unit> OnResetOperationAsObservable() =>
            onResetOperationEvent.AsObservable();

        public void ResetOperation()
        {
            elapsedTime.Value = 0f;
            onResetOperationEvent.Invoke();
        }

        /// <summary>
        /// 长时间无操作，响应回调
        /// <param name="callback">长时间无操作回调</param>
        /// <param name="autoReset">有任何输入则自动重置计时</param>
        public LongTimeNoOperation(Action callback = null, bool autoReset = true)
        {
            OnLongTimeNoOperationAsObservable()
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                });
            if (autoReset)
                _longTimeNoOperations.Add(this);
            setupCheckLogic();
        }

        public LongTimeNoOperation(float timeout, Action callback = null, bool autoReset = true)
            : this(callback, autoReset)
        {
            this.timeout = timeout;
        }

        public IObservable<Unit> OnLongTimeNoOperationAsObservable()
        {
            return isLongTimeNoOperation.Where(_ => _).AsUnitObservable();
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        /// <param name="timeout">超时时间(s)</param>
        public void SetTimeout(float timeout)
        {
            this.timeout = timeout;
        }

        /// <summary>
        /// 释放计时器， 终止检测
        /// </summary>
        public void Dispose()
        {
            foreach (var _disposable in _disposables)
            {
                _disposable.Dispose();
            }
            _disposables.Clear();
        }

        private float timeout = 60f;
        private readonly List<IDisposable> _disposables = new();
        private readonly UnityEvent onResetOperationEvent = new();
        private readonly FloatReactiveProperty elapsedTime = new(0f);
        private readonly BoolReactiveProperty isLongTimeNoOperation = new(false);

        // 处理检测逻辑
        private void setupCheckLogic()
        {
            var _elapseTimeDisposable = elapsedTime.Subscribe(_elapsed =>
            {
                isLongTimeNoOperation.Value = _elapsed >= timeout;
            });
            _disposables.Add(_elapseTimeDisposable);

            var _elapsedDisposable = Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
                    elapsedTime.Value += Time.deltaTime;
                });
            _disposables.Add(_elapsedDisposable);
        }

        private static bool hasAnyInput()
        {
#if ENABLE_INPUT_SYSTEM
            bool _hasAnyInput =
                Keyboard.current.anyKey.wasPressedThisFrame
                || Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame;

            if (!_hasAnyInput)
            {
                // check any touch
                if (Touchscreen.current != null)
                {
                    _hasAnyInput = Touchscreen.current.touches
                        .ToList()
                        .Exists(_ => _.press.wasPressedThisFrame);
                }
            }
#else
            bool _hasAnyInput = Input.anyKeyDown;
            if (!_hasAnyInput)
            {
                _hasAnyInput = Input.touchCount > 0;
            }
#endif

            return _hasAnyInput;
        }

        public static void DisposeInputCheck()
        {
            checkInputDisposable?.Dispose();
        }

        private static readonly List<LongTimeNoOperation> _longTimeNoOperations = new();
        private static IDisposable checkInputDisposable = null;

        static LongTimeNoOperation()
        {
            checkInputDisposable = Observable
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
