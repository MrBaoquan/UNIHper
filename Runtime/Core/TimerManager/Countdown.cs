using UniRx;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;

public class Countdown
{
    public enum CountdownState
    {
        Running,
        Paused,
        Stopped
    }

    private IDisposable _timerSubscription;
    private float duration;
    private float remainingTime;
    private float interval;

    public CountdownState State { get; private set; } = CountdownState.Stopped;

    // 已用时间
    public float ElapsedTime => duration - remainingTime;

    // 剩余时间
    public float RemainingTime => remainingTime;

    // 倒计时 00:02:00 形式
    public string RemainingText => TimeSpan.FromSeconds(remainingTime).ToString(@"hh\:mm\:ss");

    public string HourText => TimeSpan.FromSeconds(remainingTime).ToString(@"hh");
    public string MinuteText => TimeSpan.FromSeconds(remainingTime).ToString(@"mm");
    public string SecondText => TimeSpan.FromSeconds(remainingTime).ToString(@"ss");

    // 私有事件，不对外暴露
    private UnityEvent _onStart = new UnityEvent();
    private UnityEvent _onPause = new UnityEvent();
    private UnityEvent _onResume = new UnityEvent();
    private UnityEvent _onStop = new UnityEvent();
    private UnityEvent _onComplete = new UnityEvent();
    private UnityEvent<float> _onTick = new UnityEvent<float>();

    // 只提供 Observable 接口供外部订阅
    public IObservable<Unit> OnStartAsObservable() => _onStart.AsObservable();

    public IObservable<Unit> OnPauseAsObservable() => _onPause.AsObservable();

    public IObservable<Unit> OnResumeAsObservable() => _onResume.AsObservable();

    public IObservable<Unit> OnStopAsObservable() => _onStop.AsObservable();

    public IObservable<Unit> OnCompleteAsObservable() => _onComplete.AsObservable();

    public IObservable<float> OnTickAsObservable() => _onTick.AsObservable();

    public void SetDuration(float durationInSeconds)
    {
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
    }

    public void SetInterval(float intervalInSeconds)
    {
        interval = intervalInSeconds;
    }

    // 简洁的链式调用方法
    public Countdown OnStart(Action onStart)
    {
        _onStart.AsObservable().Subscribe(_ => onStart());
        return this;
    }

    public Countdown OnUpdate(Action<float> onTick)
    {
        _onTick.AsObservable().Subscribe(onTick);
        return this;
    }

    public Countdown OnComplete(Action onComplete)
    {
        _onComplete.AsObservable().Subscribe(_ => onComplete());
        return this;
    }

    public Countdown OnPause(Action onPause)
    {
        _onPause.AsObservable().Subscribe(_ => onPause());
        return this;
    }

    public Countdown OnResume(Action onResume)
    {
        _onResume.AsObservable().Subscribe(_ => onResume());
        return this;
    }

    public Countdown OnStop(Action onStop)
    {
        _onStop.AsObservable().Subscribe(_ => onStop());
        return this;
    }

    public Task GetAwaiter()
    {
        return _onComplete.AsObservable().First().ToTask();
    }

    // 构造函数
    public Countdown(float durationInSeconds = 10, float intervalInSeconds = 1f)
    {
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
        interval = intervalInSeconds;
        State = CountdownState.Stopped;
    }

    // 开始计时
    public Countdown Start()
    {
        if (remainingTime <= 0)
        {
            Debug.LogWarning("Countdown already finished now.");
            return this;
        }

        // 如果已有订阅，先停止它，避免重复订阅
        StopTimerSubscription();

        State = CountdownState.Running;
        _onStart?.Invoke(); // 触发开始事件

        _onTick?.Invoke(remainingTime); // 每次更新时调用回调
        _timerSubscription = Observable
            .Interval(TimeSpan.FromSeconds(interval))
            .TakeWhile(_ => remainingTime > 0)
            .Subscribe(
                _ =>
                {
                    remainingTime -= interval;
                    _onTick?.Invoke(remainingTime); // 每次更新时调用回调
                },
                () =>
                {
                    State = CountdownState.Stopped;
                    remainingTime = 0;
                    _onTick?.Invoke(remainingTime); // 最后一次更新
                    _onComplete?.Invoke(); // 触发完成回调
                }
            );
        return this;
    }

    // 暂停计时
    public void Pause()
    {
        if (State != CountdownState.Running)
            return;

        State = CountdownState.Paused;
        StopTimerSubscription();
        _onPause?.Invoke(); // 触发暂停事件
    }

    // 继续计时
    public void Resume()
    {
        if (State != CountdownState.Paused)
            return;

        _onResume?.Invoke(); // 触发恢复事件
        Start();
    }

    // 重新开始计时
    public void Restart(float durationInSeconds)
    {
        remainingTime = durationInSeconds;
        Start();
    }

    public void Restart()
    {
        Restart(duration);
    }

    // 停止计时并清理订阅
    public void Stop()
    {
        if (State == CountdownState.Stopped)
            return;

        State = CountdownState.Stopped;
        remainingTime = 0;
        StopTimerSubscription();
        _onTick?.Invoke(remainingTime); // 计时器停止时更新
        _onStop?.Invoke(); // 触发停止事件
    }

    // 停止计时器订阅
    private void StopTimerSubscription()
    {
        if (_timerSubscription != null)
        {
            _timerSubscription.Dispose();
            _timerSubscription = null;
        }
    }
}
