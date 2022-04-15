using System;
using FairyGUI;
using UniRx;
using UnityEngine.Events;

namespace UNIHper {

    public static class FairyGUIEventExtension {

        public static IObservable<EventContext> AsObservable (this EventListener fguiEvent) {
            Action<EventContext> callback = (context) => { };
            return Observable.FromEvent<EventCallback1, EventContext> (
                h => new EventCallback1 (h),
                h => fguiEvent.Add (h),
                h => fguiEvent.Remove (h)
            );
        }
    }

}