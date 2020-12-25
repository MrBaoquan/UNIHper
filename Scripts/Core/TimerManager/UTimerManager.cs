using System.Timers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UHelper;
using UniRx;

public class UTimerManager : Singleton<UTimerManager>,Manageable
{

    Timer _timerDbc;
    Timer _timerTrt;

    public void Initialize(){}

    public void Uninitialize(){}

    public IDisposable SetTimeout(Action InHandler, float InTime){
        return SetTimeout(InTime,InHandler);
    }

    public IDisposable SetTimeout(float InDuration, Action OnCompleted = null, Action<float> OnUpdate = null, float InInterval=0.05f)
    {
        float _startTime = Time.time;
        IDisposable _timerHandler = null;
        _timerHandler = Observable.Interval(TimeSpan.FromSeconds(InInterval)).Where((_1,_2)=>{
            float _delta = Time.time - _startTime;
            bool _condition = _delta <= InDuration;
            if(_condition){
                float _progress = Mathf.Clamp(_delta / InDuration, 0 ,1);
               if(OnUpdate!=null) OnUpdate(_progress);
            }else{
                if(OnUpdate!=null) OnUpdate(1.0f);
                _timerHandler.Dispose();
                if(OnCompleted!=null) OnCompleted();
            }
            return _condition;
        }).Subscribe(_=>{});
        return _timerHandler;
    }

    public IDisposable SetInterval(Action InCallback, float InInterval)
    {
        return Observable.Interval(TimeSpan.FromSeconds(InInterval)).Subscribe(_=>{
            if(InCallback!=null) InCallback();
        });

    }

    public Action Throttle(float InTime, Action InAction)
    {
        float _last = 0;
        return ()=>{
            float _delta = Time.time - _last;
            if(_delta>=InTime){
                InAction();
                _last = Time.time;
            }
        };
    }

    public Action Debounce(float InTime, Action InCallback)
    {
        IDisposable _timerHandler = null;
        float _lastCallTime =Time.time;
        Func<long,bool> _condition = _=>{
            //Debug.LogFormat("count time:{0}", Time.time - _lastCallTime);
            return (Time.time - _lastCallTime)>=InTime;
        };

        _timerHandler = Observable.EveryUpdate()
                .Where(_condition)
                .Subscribe(_=>{
                    _lastCallTime = Time.time;
                    InCallback();
                });

        return ()=>{
            _lastCallTime = Time.time;
        };
    }

    public void NextFrame(Action InAction){
        Observable.NextFrame().Subscribe(_=>{
            if(InAction!=null) InAction();
        });
    }



}
