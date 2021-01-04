using System;
using UniRx;
namespace UHelper
{
    
public static class UObservable
{
    public static IObservable<T> FromEvent<T>(Action<T> InDelegate)
    {
        return Observable.FromEvent<T>(_action=>InDelegate+=_action, _action=>InDelegate-=_action);
    }
}


}