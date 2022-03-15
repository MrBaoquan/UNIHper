using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

public class TestObservable : IObservable<int> {
    public IDisposable Subscribe (IObserver<int> observer) {
        Timer _timer = null;
        int _count = -1;
        int _flag = 0;

        _timer = new Timer (_ => {
            if (Interlocked.Exchange (ref _flag, 1) == 0) {
                observer.OnNext (_count);
                if (_count++ > 5) {
                    observer.OnCompleted ();
                    _timer.Dispose ();
                }
                Interlocked.Exchange (ref _flag, 0);
            }

        }, null, 0, 10);
        return Disposable.Empty;
    }

}

public class TestDHelper {

    public static void Test () {
        // NLogger.Debug("current thread id: " +Thread.CurrentThread.ManagedThreadId);
        // (new TestObservable())
        // .ObserveOn(Scheduler.CurrentThread)
        // .Subscribe(Observer.Create<int>(_=>{
        //     NLogger.Debug("thread id: " +Thread.CurrentThread.ManagedThreadId);
        //     NLogger.Debug(_);
        // }));
        //Observable.Subscribe(Observable.Create<int>(_=>{return Disposable.Empty;}));

    }

}