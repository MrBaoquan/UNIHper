using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UNIHper.UI
{
    public abstract class UIAnimationBase : MonoBehaviour
    {
        internal abstract void OnUIAttached();
        public abstract Task BuildShowTask(CancellationToken cancellationToken = default);
        public abstract Task BuildHideTask(CancellationToken cancellationToken = default);

        public float ShowDuration { get; protected set; } = 0.0f;
        public float HideDuration { get; protected set; } = 0.0f;
    }
}
