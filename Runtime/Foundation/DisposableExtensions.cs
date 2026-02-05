using System;
using UniRx;

namespace UNIHper
{
    /// <summary>
    /// IDisposable 扩展方法，支持通过字符串 key 管理订阅
    /// </summary>
    public static class DisposableExtensions
    {
        #region 替换模式扩展 (Serial Mode)

        /// <summary>
        /// 将 IDisposable 注册到 DisposableManager 中指定的 key（替换模式）
        /// 如果该 key 已存在订阅，会自动取消旧订阅
        /// 用法: Observable.Timer(...).Subscribe(...).DisposeWith("autoReturn");
        /// </summary>
        public static T DisposeWith<T>(this T disposable, string key)
            where T : IDisposable
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            DisposableManager.Instance.Set(key, disposable);
            return disposable;
        }

        /// <summary>
        /// 将 IObservable 订阅后注册到 DisposableManager（替换模式）
        /// 用法: Observable.Timer(...).SubscribeWith("autoReturn", _ => { });
        /// </summary>
        public static IDisposable SubscribeWith<T>(this IObservable<T> source, string key, Action<T> onNext)
        {
            return source.Subscribe(onNext).DisposeWith(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后注册到 DisposableManager（替换模式）
        /// </summary>
        public static IDisposable SubscribeWith<T>(this IObservable<T> source, string key, Action<T> onNext, Action<Exception> onError)
        {
            return source.Subscribe(onNext, onError).DisposeWith(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后注册到 DisposableManager（替换模式）
        /// </summary>
        public static IDisposable SubscribeWith<T>(this IObservable<T> source, string key, Action<T> onNext, Action onCompleted)
        {
            return source.Subscribe(onNext, onCompleted).DisposeWith(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后注册到 DisposableManager（替换模式）
        /// </summary>
        public static IDisposable SubscribeWith<T>(
            this IObservable<T> source,
            string key,
            Action<T> onNext,
            Action<Exception> onError,
            Action onCompleted
        )
        {
            return source.Subscribe(onNext, onError, onCompleted).DisposeWith(key);
        }

        #endregion

        #region 累积模式扩展 (Composite Mode)

        /// <summary>
        /// 将 IDisposable 添加到 DisposableManager 中指定的 key（累积模式）
        /// 同 key 可以累积多个订阅
        /// 用法: Observable.Timer(...).Subscribe(...).AddTo("gameLoop");
        /// </summary>
        public static T AddTo<T>(this T disposable, string key)
            where T : IDisposable
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            DisposableManager.Instance.Add(key, disposable);
            return disposable;
        }

        /// <summary>
        /// 将 IObservable 订阅后添加到 DisposableManager（累积模式）
        /// 用法: Observable.Interval(...).SubscribeAndAddTo("gameLoop", count => { });
        /// </summary>
        public static IDisposable SubscribeAndAddTo<T>(this IObservable<T> source, string key, Action<T> onNext)
        {
            return source.Subscribe(onNext).AddTo(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后添加到 DisposableManager（累积模式）
        /// </summary>
        public static IDisposable SubscribeAndAddTo<T>(this IObservable<T> source, string key, Action<T> onNext, Action<Exception> onError)
        {
            return source.Subscribe(onNext, onError).AddTo(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后添加到 DisposableManager（累积模式）
        /// </summary>
        public static IDisposable SubscribeAndAddTo<T>(this IObservable<T> source, string key, Action<T> onNext, Action onCompleted)
        {
            return source.Subscribe(onNext, onCompleted).AddTo(key);
        }

        /// <summary>
        /// 将 IObservable 订阅后添加到 DisposableManager（累积模式）
        /// </summary>
        public static IDisposable SubscribeAndAddTo<T>(
            this IObservable<T> source,
            string key,
            Action<T> onNext,
            Action<Exception> onError,
            Action onCompleted
        )
        {
            return source.Subscribe(onNext, onError, onCompleted).AddTo(key);
        }

        #endregion
    }
}
