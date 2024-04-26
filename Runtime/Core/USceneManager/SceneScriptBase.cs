using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    public abstract class SceneScriptBase
    {
        internal ReactiveProperty<bool> isSceneReady { get; set; } =
            new ReactiveProperty<bool>(false);

        public IObservable<Unit> OnSceneReadyAsObservable()
        {
            return isSceneReady.Value
                ? Observable.Return(Unit.Default)
                : isSceneReady.Where(_isReady => _isReady).AsUnitObservable();
        }

        protected virtual void OnLongTimeNoOperation() { }
    }
}
