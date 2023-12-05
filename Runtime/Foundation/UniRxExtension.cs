using System;
using TMPro;
using UniRx;

namespace UNIHper
{
    public static class UniRxExtension
    {
        public static IObservable<T> FromEvent<T>(Action<T> InDelegate)
        {
            return Observable.FromEvent<T>(
                _action => InDelegate += _action,
                _action => InDelegate -= _action
            );
        }

        public static IDisposable SubscribeToText(
            this IObservable<string> source,
            TextMeshProUGUI text
        )
        {
            return source.SubscribeWithState(text, (x, t) => t.text = x);
        }

        public static IDisposable SubscribeToText<T>(
            this IObservable<T> source,
            TextMeshProUGUI text
        )
        {
            return source.SubscribeWithState(text, (x, t) => t.text = x.ToString());
        }
    }
}
