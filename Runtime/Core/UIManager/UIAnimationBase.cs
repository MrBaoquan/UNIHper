using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UNIHper.UI
{
    public abstract class UIAnimationBase : MonoBehaviour
    {
        protected abstract void OnUIAttached();
        public abstract Task BuildShowTask();
        public abstract Task BuildHideTask();
    }
}
