using UnityEngine;

namespace UNIHper.UI
{
    /// <summary>
    /// Unmask Raycast Filter.
    /// The ray passes through the unmasked rectangle.
    /// </summary>
    public class InverseMaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        //################################
        // Serialize Members.
        //################################
        [Tooltip("Target unmask component. The ray passes through the unmasked rectangle.")]
        [SerializeField]
        private InverseMask m_TargetUnmask;

        //################################
        // Public Members.
        //################################
        /// <summary>
        /// Target unmask component. Ray through the unmasked rectangle.
        /// </summary>
        public InverseMask targetUnmask
        {
            get { return m_TargetUnmask; }
            set { m_TargetUnmask = value; }
        }

        /// <summary>
        /// Given a point and a camera is the raycast valid.
        /// </summary>
        /// <returns>Valid.</returns>
        /// <param name="sp">Screen position.</param>
        /// <param name="eventCamera">Raycast camera.</param>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // Skip if deactived.
            if (!isActiveAndEnabled || !m_TargetUnmask || !m_TargetUnmask.isActiveAndEnabled)
            {
                return true;
            }

            Debug.LogError(sp);
            // check inside
            if (eventCamera)
            {
                Debug.LogWarning(" AAAA");
                return !RectTransformUtility.RectangleContainsScreenPoint(
                    (m_TargetUnmask.transform as RectTransform),
                    sp,
                    eventCamera
                );
            }
            else
            {
                bool _result = RectTransformUtility.RectangleContainsScreenPoint(
                    (m_TargetUnmask.transform as RectTransform),
                    sp
                );
                Debug.LogWarning("BBBB" + _result);
                return _result;
            }
        }

        //################################
        // Private Members.
        //################################

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable() { }
    }
}
