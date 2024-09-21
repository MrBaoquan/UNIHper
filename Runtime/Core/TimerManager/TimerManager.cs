using System;
using System.Threading.Tasks;
using DNHper;
using UnityEngine;

namespace UNIHper
{
    using UniRx;

    public class TimerManager : Singleton<TimerManager>
    {
        internal Task Initialize()
        {
            return Task.CompletedTask;
        }

        public IDisposable Delay(float delayInSeconds, Action callback)
        {
            return Observable
                .Timer(TimeSpan.FromSeconds(delayInSeconds))
                .Subscribe(_ => callback());
        }

        public Task Delay(float delayInSeconds)
        {
            return Observable.Timer(TimeSpan.FromSeconds(delayInSeconds)).ToTask();
        }

        public Countdown Countdown(float durationInSecons, float tickInterval = 1)
        {
            return new Countdown(durationInSecons, tickInterval);
        }

        // public IDisposable Countdown(Action<int> update, Action completed, int duration)
        // {
        //     update?.Invoke(duration);
        //     return Observable
        //         .Interval(TimeSpan.FromSeconds(1))
        //         .Take(duration + 1)
        //         .Subscribe(_ =>
        //         {
        //             if (duration <= 0)
        //             {
        //                 completed?.Invoke();
        //                 return;
        //             }
        //             update?.Invoke(Mathf.Max(--duration, 0));
        //         });
        // }

        // public IDisposable SetTimeout(Action InHandler, float InTime)
        // {
        //     return SetTimeout(InHandler, null, InTime);
        // }

        // public IDisposable SetTimeout(
        //     Action OnCompleted,
        //     Action<float> OnUpdate,
        //     float InDuration,
        //     float InInterval = 0.05f
        // )
        // {
        //     float _startTime = Time.time;
        //     IDisposable _timerHandler = null;
        //     _timerHandler = Observable
        //         .Interval(TimeSpan.FromSeconds(InInterval))
        //         .Where(
        //             (_1, _2) =>
        //             {
        //                 float _delta = Time.time - _startTime;
        //                 bool _condition = _delta <= InDuration;
        //                 if (_condition)
        //                 {
        //                     float _progress = Mathf.Clamp(_delta / InDuration, 0, 1);
        //                     if (OnUpdate != null)
        //                         OnUpdate(_progress);
        //                 }
        //                 else
        //                 {
        //                     if (OnUpdate != null)
        //                         OnUpdate(1.0f);
        //                     _timerHandler.Dispose();
        //                     if (OnCompleted != null)
        //                         OnCompleted();
        //                 }
        //                 return _condition;
        //             }
        //         )
        //         .Subscribe(_ => { });
        //     return _timerHandler;
        // }

        // public IDisposable SetInterval(Action callback, float interval)
        // {
        //     return Observable
        //         .Interval(TimeSpan.FromSeconds(interval))
        //         .Subscribe(_ =>
        //         {
        //             callback?.Invoke();
        //         });
        // }

        /// <summary>
        /// 忽略InTime时间内的多次调用          (节流)
        /// </summary>
        /// <param name="InTime"></param>
        /// <param name="InCallback"></param>   主动调用时: 计时器小于InTime时  调用将会被忽略  计时器大于InTime时，回调一次  计时器清零
        /// <returns></returns>
        public Action Throttle(float InTime, Action callback)
        {
            float _last = 0;
            return () =>
            {
                float _delta = Time.time - _last;
                if (_delta >= InTime)
                {
                    callback();
                    _last = Time.time;
                }
            };
        }

        /// <summary>
        /// 忽略InTime时间内的多次调用
        /// </summary>
        /// <param name="InTime"></param>
        /// <param name="InCallback"></param>   主动调用后: 计时器清零, 计时器大于InTime时，回调一次, 计时器重新计时
        /// <returns></returns>
        public Action Debounce(float InTime, Action InCallback)
        {
            IDisposable _timerHandler = null;
            float _lastCallTime = Time.time;
            Func<long, bool> _condition = _ =>
            {
                return (Time.time - _lastCallTime) >= InTime;
            };

            Action _registerTrigger = () =>
            {
                _timerHandler = Observable
                    .EveryUpdate()
                    .Where(_condition)
                    .First()
                    .Subscribe(_ =>
                    {
                        _lastCallTime = Time.time;
                        _timerHandler = null;
                        InCallback();
                    });
            };

            Action _debounceDelegate = () =>
            {
                _lastCallTime = Time.time;
                if (_timerHandler == null)
                    _registerTrigger();
            };
            //_debounceDelegate ();
            return _debounceDelegate;
        }

        public Task NextFrame()
        {
            return Observable.NextFrame().ToTask();
        }

        public IDisposable NextFrame(Action callback)
        {
            return Observable.NextFrame().Subscribe(_ => callback());
        }
    }
}
