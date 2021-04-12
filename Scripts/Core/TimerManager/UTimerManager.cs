using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UniRx;
using UnityEngine;
using UNIHper;

public class UTimerManager : Singleton<UTimerManager>, Manageable {

    Timer _timerDbc;
    Timer _timerTrt;

    public void Initialize () { }

    public void Uninitialize () { }

    public IDisposable SetTimeout (Action InHandler, float InTime) {
        return SetTimeout (InHandler, InTime);
    }

    public IDisposable SetTimeout (Action OnCompleted, Action<float> OnUpdate, float InDuration, float InInterval = 0.05f) {
        float _startTime = Time.time;
        IDisposable _timerHandler = null;
        _timerHandler = Observable.Interval (TimeSpan.FromSeconds (InInterval)).Where ((_1, _2) => {
            float _delta = Time.time - _startTime;
            bool _condition = _delta <= InDuration;
            if (_condition) {
                float _progress = Mathf.Clamp (_delta / InDuration, 0, 1);
                if (OnUpdate != null) OnUpdate (_progress);
            } else {
                if (OnUpdate != null) OnUpdate (1.0f);
                _timerHandler.Dispose ();
                if (OnCompleted != null) OnCompleted ();
            }
            return _condition;
        }).Subscribe (_ => { });
        return _timerHandler;
    }

    public IDisposable SetInterval (Action InCallback, float InInterval) {
        return Observable.Interval (TimeSpan.FromSeconds (InInterval)).Subscribe (_ => {
            if (InCallback != null) InCallback ();
        });

    }

    /// <summary>
    /// 忽略InTime时间内的多次调用          (节流)
    /// </summary>
    /// <param name="InTime"></param>      
    /// <param name="InCallback"></param>   主动调用时: 计时器小于InTime时  调用将会被忽略  计时器大于InTime时，回调一次  计时器清零
    /// <returns></returns>
    public Action Throttle (float InTime, Action InAction) {
        float _last = 0;
        return () => {
            float _delta = Time.time - _last;
            if (_delta >= InTime) {
                InAction ();
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
    public Action Debounce (float InTime, Action InCallback) {
        IDisposable _timerHandler = null;
        float _lastCallTime = Time.time;
        Func<long, bool> _condition = _ => {
            return (Time.time - _lastCallTime) >= InTime;
        };

        _timerHandler = Observable.EveryUpdate ()
            .Where (_condition)
            .Subscribe (_ => {
                _lastCallTime = Time.time;
                if (_timerHandler != null) {
                    _timerHandler.Dispose ();
                    _timerHandler = null;
                }
                InCallback ();
            });
        return () => {
            _lastCallTime = Time.time;
        };
    }

    public void NextFrame (Action InAction) {
        Observable.NextFrame ().Subscribe (_ => {
            if (InAction != null) InAction ();
        });
    }

}