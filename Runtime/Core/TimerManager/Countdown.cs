using UniRx;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;

public class Countdown
{
    private IDisposable _timerSubscription;
    private float duration;
    private float remainingTime;
    private float interval;
    private bool isPaused;
    public bool IsPaused => isPaused;

    // 已用时间
    public float ElapsedTime => duration - remainingTime;

    // 剩余时间
    public float RemainingTime => remainingTime;

    // 倒计时 00:02:00 形式
    public string RemainingText => TimeSpan.FromSeconds(remainingTime).ToString(@"hh\:mm\:ss");

    public string HourText => TimeSpan.FromSeconds(remainingTime).ToString(@"hh");
    public string MinuteText => TimeSpan.FromSeconds(remainingTime).ToString(@"mm");
    public string SecondText => TimeSpan.FromSeconds(remainingTime).ToString(@"ss");

    public UnityEvent OnTimerEnd = new UnityEvent();
    public UnityEvent<float> OnTimerUpdate = new UnityEvent<float>();

    public void SetDuration(float durationInSeconds)
    {
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
    }

    public void SetInterval(float intervalInSeconds)
    {
        interval = intervalInSeconds;
    }

    public Countdown OnUpdate(Action<float> onUpdate)
    {
        OnTimerUpdate.AsObservable().Subscribe(onUpdate);
        return this;
    }

    public Countdown OnComplete(Action onFinish)
    {
        OnTimerEnd.AsObservable().Subscribe(_ => onFinish());
        return this;
    }

    public Task GetAwaiter()
    {
        return OnTimerEnd.AsObservable().First().ToTask();
    }

    // 构造函数
    public Countdown(float durationInSeconds = 10, float intervalInSeconds = 1f)
    {
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
        interval = intervalInSeconds;
        isPaused = false;
    }

    // 开始计时
    public Countdown Start()
    {
        // 如果已有订阅，先停止它，避免重复订阅
        StopTimerSubscription();

        isPaused = false;

        OnTimerUpdate?.Invoke(remainingTime); // 每次更新时调用回调
        _timerSubscription = Observable
            .Interval(TimeSpan.FromSeconds(interval))
            .TakeWhile(_ => remainingTime > 0)
            .Subscribe(
                _ =>
                {
                    remainingTime -= interval;
                    OnTimerUpdate?.Invoke(remainingTime); // 每次更新时调用回调
                },
                () =>
                {
                    Stop(); // 计时器结束时调用回调
                    OnTimerEnd?.Invoke(); // 触发结束回调
                }
            );
        return this;
    }

    // 暂停计时
    public void Pause()
    {
        isPaused = true;
        StopTimerSubscription();
    }

    // 继续计时
    public void Resume()
    {
        if (isPaused)
        {
            Start();
        }
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
        remainingTime = 0;
        StopTimerSubscription();
        OnTimerUpdate?.Invoke(remainingTime); // 计时器停止时更新
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
