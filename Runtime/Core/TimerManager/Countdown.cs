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

    // 已用时间
    public float ElapsedTime => duration - remainingTime;

    // 剩余时间
    public float RemainingTime => remainingTime;

    public UnityEvent OnTimerEnd;
    public UnityEvent<float> OnTimerUpdate;

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
    public Countdown(float durationInSeconds, float intervalInSeconds = 1f)
    {
        duration = durationInSeconds;
        remainingTime = durationInSeconds;
        interval = intervalInSeconds;
        isPaused = false;

        // 初始化 UnityEvent
        OnTimerEnd = new UnityEvent();
        OnTimerUpdate = new UnityEvent<float>();
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
