using System;
using System.Diagnostics;
using UniRx;
using UnityEngine;

namespace UHelper {

    public static class AppUtility {
        public static void KeepWindowTop (float InInterval = 10.0f) {
            Observable.Interval (TimeSpan.FromSeconds (InInterval))
                .Subscribe (_ => {
                    var _window = WinAPI.CurrentWindow ();
                    WinAPI.ShowWindow (_window, 3);
                    WinAPI.SetForegroundWindow (_window);
                });
        }
    }

}