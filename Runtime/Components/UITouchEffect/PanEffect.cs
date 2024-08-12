using UnityEngine;

using DigitalRubyShared;
using Coffee.UIExtensions;

namespace UNIHper
{
    public class PanEffect : SingletonBehaviour<PanEffect>
    {
        internal void Initialize() { }

        private PanGestureRecognizer panGestureRecognizer;
        private UIParticle panTrialEffect;

        public void SetViewTarget(GameObject target)
        {
            panGestureRecognizer.PlatformSpecificView = target;
        }

        public void SetColor(Color color)
        {
            var main = panTrialEffect.particles[0].main;
            main.startColor = color;
        }

        private void Awake()
        {
            transform.SetParent(Framework.Instance.TopmostCanvas.transform);
            var _panEffectAsset = Resources.Load<GameObject>("__Prefabs/Common/PanEffect");
            if (_panEffectAsset is null)
            {
                Debug.LogWarning("Cannot find PanEffect prefab");
                return;
            }
            var _panEffect = GameObject.Instantiate(_panEffectAsset);
            _panEffect.transform.SetParent(transform.parent);

            panTrialEffect = _panEffect.GetComponent<UIParticle>();
            panTrialEffect.Stop();

            panGestureRecognizer = new PanGestureRecognizer();
            // _panGesture.PlatformSpecificView = this.Get("game_area").gameObject;
            panGestureRecognizer.StateUpdated += (_) =>
            {
                if (_.State == GestureRecognizerState.Began)
                {
                    panTrialEffect.gameObject.SetActive(true);
                    panTrialEffect.transform.position = new Vector3(_.FocusX, _.FocusY);
                    panTrialEffect.Play();
                }
                else if (_.State == GestureRecognizerState.Ended)
                {
                    panTrialEffect.Stop();
                    panTrialEffect.gameObject.SetActive(false);
                }
                else if (_.State == GestureRecognizerState.Executing)
                {
                    panTrialEffect.transform.position = new Vector3(_.FocusX, _.FocusY);
                }
            };
            FingersScript.Instance.AddGesture(panGestureRecognizer);
            FingersScript.Instance.ShowTouches = false;
        }
    }
}
