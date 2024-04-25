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
        internal bool isSceneReady { get; set; } = false;

        /// <summary>
        /// 当前场景脚本是否就绪
        /// </summary>
        public bool IsSceneReady => isSceneReady;

        protected virtual void OnLongTimeNoOperation() { }
    }
}
