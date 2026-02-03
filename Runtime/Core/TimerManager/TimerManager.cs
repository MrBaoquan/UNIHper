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

        #region Delay - 延时操作

        /// <summary>
        /// 创建延时 Observable（Rx 风格）
        /// 用法: Managements.Timer.Delay(3f).Subscribe(_ => { ... });
        /// </summary>
        /// <param name="delayInSeconds">延时秒数</param>
        /// <returns>IObservable，触发一次后完成</returns>
        public IObservable<long> Delay(float delayInSeconds)
        {
            return Observable.Timer(TimeSpan.FromSeconds(delayInSeconds));
        }

        /// <summary>
        /// 延时执行回调（便捷方法）
        /// </summary>
        /// <param name="delayInSeconds">延时秒数</param>
        /// <param name="callback">回调函数</param>
        /// <returns>可取消的 IDisposable</returns>
        public IDisposable Delay(float delayInSeconds, Action callback)
        {
            return Delay(delayInSeconds).Subscribe(_ => callback());
        }

        /// <summary>
        /// 延时执行回调，可通过 key 取消（替换模式）
        /// 如果该 key 已存在延时，会自动取消旧的延时
        /// </summary>
        /// <param name="delayInSeconds">延时秒数</param>
        /// <param name="callback">回调函数</param>
        /// <param name="key">用于取消的 key</param>
        /// <returns>可取消的 IDisposable</returns>
        public IDisposable Delay(float delayInSeconds, Action callback, string key)
        {
            return Delay(delayInSeconds).Subscribe(_ => callback()).DisposeWith(key);
        }

        /// <summary>
        /// 延时（异步等待）
        /// </summary>
        /// <param name="delayInSeconds">延时秒数</param>
        public Task DelayAsync(float delayInSeconds)
        {
            return Delay(delayInSeconds).ToTask();
        }

        #endregion

        #region Interval - 间隔重复操作

        /// <summary>
        /// 创建间隔重复 Observable（Rx 风格）
        /// 用法: Managements.Timer.Interval(1f).Subscribe(count => { ... });
        /// </summary>
        /// <param name="intervalInSeconds">间隔秒数</param>
        /// <returns>IObservable，每隔指定时间触发一次，值为触发次数（从0开始）</returns>
        public IObservable<long> Interval(float intervalInSeconds)
        {
            return Observable.Interval(TimeSpan.FromSeconds(intervalInSeconds));
        }

        /// <summary>
        /// 间隔重复执行回调
        /// </summary>
        /// <param name="intervalInSeconds">间隔秒数</param>
        /// <param name="callback">回调函数</param>
        /// <returns>可取消的 IDisposable</returns>
        public IDisposable Interval(float intervalInSeconds, Action callback)
        {
            return Interval(intervalInSeconds).Subscribe(_ => callback());
        }

        /// <summary>
        /// 间隔重复执行回调（带计数）
        /// </summary>
        /// <param name="intervalInSeconds">间隔秒数</param>
        /// <param name="callback">回调函数，参数为触发次数（从0开始）</param>
        /// <returns>可取消的 IDisposable</returns>
        public IDisposable Interval(float intervalInSeconds, Action<long> callback)
        {
            return Interval(intervalInSeconds).Subscribe(callback);
        }

        /// <summary>
        /// 间隔重复执行回调，可通过 key 取消（替换模式）
        /// </summary>
        public IDisposable Interval(float intervalInSeconds, Action callback, string key)
        {
            return Interval(intervalInSeconds).Subscribe(_ => callback()).DisposeWith(key);
        }

        #endregion

        #region Timeout - 超时操作

        /// <summary>
        /// 创建超时 Observable，在指定时间内持续触发进度更新
        /// 用法: Managements.Timer.Timeout(5f).Subscribe(progress => { ... }, () => { 完成 });
        /// </summary>
        /// <param name="durationInSeconds">持续时间</param>
        /// <param name="updateInterval">更新间隔，默认0.05秒</param>
        /// <returns>IObservable，值为进度(0-1)</returns>
        public IObservable<float> Timeout(float durationInSeconds, float updateInterval = 0.05f)
        {
            float startTime = Time.time;
            return Observable
                .Interval(TimeSpan.FromSeconds(updateInterval))
                .Select(_ => Mathf.Clamp01((Time.time - startTime) / durationInSeconds))
                .TakeWhile(progress => progress < 1f)
                .Concat(Observable.Return(1f));
        }

        /// <summary>
        /// 超时执行，带进度更新和完成回调
        /// </summary>
        public IDisposable Timeout(float durationInSeconds, Action<float> onUpdate, Action onCompleted, float updateInterval = 0.05f)
        {
            return Timeout(durationInSeconds, updateInterval).Subscribe(onUpdate, () => onCompleted?.Invoke());
        }

        #endregion

        #region Cancel - 取消操作

        /// <summary>
        /// 取消指定 key 的定时操作
        /// </summary>
        /// <param name="key">定时操作的 key</param>
        public void Cancel(string key)
        {
            DisposableManager.Instance.Cancel(key);
        }

        /// <summary>
        /// 检查指定 key 的定时是否正在进行
        /// </summary>
        /// <param name="key">定时操作的 key</param>
        /// <returns>是否正在进行</returns>
        public bool IsPending(string key)
        {
            return DisposableManager.Instance.HasSerial(key);
        }

        #endregion

        #region Countdown - 倒计时

        /// <summary>
        /// 创建倒计时 Observable
        /// 用法: Managements.Timer.CountdownObservable(10).Subscribe(remain => { ... }, () => { 完成 });
        /// </summary>
        /// <param name="durationInSeconds">倒计时总时长</param>
        /// <param name="tickInterval">tick间隔，默认1秒</param>
        /// <returns>IObservable，值为剩余时间</returns>
        public IObservable<float> CountdownObservable(float durationInSeconds, float tickInterval = 1f)
        {
            int totalTicks = Mathf.CeilToInt(durationInSeconds / tickInterval);
            return Observable
                .Interval(TimeSpan.FromSeconds(tickInterval))
                .Take(totalTicks + 1)
                .Select(tick => Mathf.Max(0, durationInSeconds - tick * tickInterval))
                .StartWith(durationInSeconds);
        }

        /// <summary>
        /// 创建 Countdown 对象（兼容现有API）
        /// </summary>
        public Countdown Countdown(float durationInSeconds, float tickInterval = 1)
        {
            return new Countdown(durationInSeconds, tickInterval);
        }

        #endregion

        #region NextFrame - 下一帧操作

        /// <summary>
        /// 创建下一帧 Observable（Rx 风格）
        /// </summary>
        public IObservable<Unit> NextFrameObservable()
        {
            return Observable.NextFrame();
        }

        /// <summary>
        /// 下一帧执行回调
        /// </summary>
        public IDisposable NextFrame(Action callback)
        {
            return NextFrameObservable().Subscribe(_ => callback());
        }

        /// <summary>
        /// 下一帧（异步等待）
        /// </summary>
        public Task NextFrameAsync()
        {
            return NextFrameObservable().ToTask();
        }

        #endregion

        #region Throttle & Debounce - 节流与防抖

        /// <summary>
        /// 创建节流函数 - 忽略指定时间内的多次调用
        /// 主动调用时: 计时器小于指定时间时调用将会被忽略，大于时回调一次并计时器清零
        /// </summary>
        public Action Throttle(float intervalInSeconds, Action callback)
        {
            float lastTime = 0;
            return () =>
            {
                float delta = Time.time - lastTime;
                if (delta >= intervalInSeconds)
                {
                    callback();
                    lastTime = Time.time;
                }
            };
        }

        /// <summary>
        /// 创建防抖函数 - 延迟执行，期间有新调用则重新计时
        /// 主动调用后: 计时器清零，计时器大于指定时间时回调一次
        /// </summary>
        public Action Debounce(float delayInSeconds, Action callback)
        {
            IDisposable timerHandler = null;
            float lastCallTime = Time.time;

            Func<long, bool> condition = _ => (Time.time - lastCallTime) >= delayInSeconds;

            Action registerTrigger = () =>
            {
                timerHandler = Observable
                    .EveryUpdate()
                    .Where(condition)
                    .First()
                    .Subscribe(_ =>
                    {
                        lastCallTime = Time.time;
                        timerHandler = null;
                        callback();
                    });
            };

            return () =>
            {
                lastCallTime = Time.time;
                if (timerHandler == null)
                    registerTrigger();
            };
        }

        #endregion
    }
}
