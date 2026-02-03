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
    private CompositeDisposable _eventSubscriptions = new CompositeDisposable();
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
    private readonly Subject<Unit> _onStart = new Subject<Unit>();
    private readonly Subject<Unit> _onPause = new Subject<Unit>();
    private readonly Subject<Unit> _onResume = new Subject<Unit>();
    private readonly Subject<Unit> _onStop = new Subject<Unit>();
    private readonly Subject<Unit> _onComplete = new Subject<Unit>();
    private readonly Subject<float> _onTick = new Subject<float>();

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

    // 简洁的链式调用方法 - 订阅会被自动管理
    public Countdown OnStart(Action onStart)
    {
        _onStart.Subscribe(_ => onStart()).AddTo(_eventSubscriptions);
        return this;
    }

    public Countdown OnUpdate(Action<float> onTick)
    {
        _onTick.Subscribe(onTick).AddTo(_eventSubscriptions);
        return this;
    }

    public Countdown OnComplete(Action onComplete)
    {
        _onComplete.Subscribe(_ => onComplete()).AddTo(_eventSubscriptions);
        return this;
    }

    public Countdown OnPause(Action onPause)
    {
        _onPause.Subscribe(_ => onPause()).AddTo(_eventSubscriptions);
        return this;
    }

    public Countdown OnResume(Action onResume)
    {
        _onResume.Subscribe(_ => onResume()).AddTo(_eventSubscriptions);
        return this;
    }

    public Countdown OnStop(Action onStop)
    {
        _onStop.Subscribe(_ => onStop()).AddTo(_eventSubscriptions);
        return this;
    }

    public Task GetAwaiter()
    {
        return _onComplete.First().ToTask();
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

        var wasRunning = State == CountdownState.Running;
        State = CountdownState.Running;

        // 只有首次启动才触发 onStart（Resume不触发）
        if (!wasRunning)
        {
            _onStart.OnNext(Unit.Default);
        }

        _onTick.OnNext(remainingTime); // 每次更新时调用回调
        _timerSubscription = Observable
            .Interval(TimeSpan.FromSeconds(interval))
            .TakeWhile(_ => remainingTime > 0)
            .Subscribe(
                _ =>
                {
                    remainingTime = Mathf.Max(0, remainingTime - interval);
                    _onTick.OnNext(remainingTime);
                },
                () =>
                {
                    State = CountdownState.Stopped;
                    remainingTime = 0;
                    _onTick.OnNext(remainingTime);
                    _onComplete.OnNext(Unit.Default);
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
        _onPause.OnNext(Unit.Default);
    }

    // 继续计时
    public void Resume()
    {
        if (State != CountdownState.Paused)
            return;

        State = CountdownState.Running; // 先设置状态，避免Start()触发onStart
        _onResume.OnNext(Unit.Default);

        // 直接启动计时器，不触发onStart
        StopTimerSubscription();
        _onTick.OnNext(remainingTime);
        _timerSubscription = Observable
            .Interval(TimeSpan.FromSeconds(interval))
            .TakeWhile(_ => remainingTime > 0)
            .Subscribe(
                _ =>
                {
                    remainingTime = Mathf.Max(0, remainingTime - interval);
                    _onTick.OnNext(remainingTime);
                },
                () =>
                {
                    State = CountdownState.Stopped;
                    remainingTime = 0;
                    _onTick.OnNext(remainingTime);
                    _onComplete.OnNext(Unit.Default);
                }
            );
    }

    // 重新开始计时
    public void Restart(float durationInSeconds)
    {
        Stop(); // 先完全停止
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
        Start();
    }

    public void Restart()
    {
        Restart(duration);
    }

    public void Reset()
    {
        remainingTime = duration;
        State = CountdownState.Stopped;
        StopTimerSubscription();
        _onTick.OnNext(remainingTime);
    }

    // 停止计时（保留剩余时间）
    public void Stop()
    {
        if (State == CountdownState.Stopped)
            return;

        State = CountdownState.Stopped;
        StopTimerSubscription();
        _onStop.OnNext(Unit.Default);
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

    // 清理所有订阅（在不再使用时调用）
    public void Dispose()
    {
        StopTimerSubscription();
        _eventSubscriptions.Dispose();
        _onStart.Dispose();
        _onPause.Dispose();
        _onResume.Dispose();
        _onStop.Dispose();
        _onComplete.Dispose();
        _onTick.Dispose();
    }
}
