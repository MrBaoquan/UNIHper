using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    public abstract class SceneScriptBase { 
        protected virtual void OnLongTimeNoOperation() { }
    }
}
