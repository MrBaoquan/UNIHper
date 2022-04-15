using System;
using FairyGUI;
using UniRx;
using UnityEngine;

namespace UNIHper {

    public static class FairyGUIComponentExtension {
        public static T Get<T> (this GComponent component, string path) where T : GComponent {
            return component.GetChildByPath (path) as T;
        }

        public static IObservable<EventContext> OnClickAsObservable (this GObject gObject) {
            return gObject.onClick.AsObservable ();
        }

        public static IObservable<double> OnValueChangedAsObservable (this GSlider slider) {
            return Observable.CreateWithState<double, GSlider> (slider, (s, observer) => {
                observer.OnNext (s.value);
                return s.onChanged.AsObservable ().Subscribe (_ => observer.OnNext (s.value), observer.OnError, observer.OnCompleted);
            });
        }
    }

}