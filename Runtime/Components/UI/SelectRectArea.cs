using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNIHper
{
    using UniRx;

    [RequireComponent(typeof(ResizeableImage))]
    public class SelectRectArea : MonoBehaviour
    {
        public KeyCode HoldKey = KeyCode.None;
        public KeyCode NotHoldKey = KeyCode.None;

        // Start is called before the first frame update
        void Start()
        {
            startRect();
        }

        public IObservable<int> OnUpdateAsObservable()
        {
            return Observable.FromEvent<int>(
                _action => Handler += _action,
                _action => Handler -= _action
            );
        }

        Action<int> Handler = null;

        private void startRect()
        {
            ResizeableImage _resizeImage = this.GetComponent<ResizeableImage>();
            _resizeImage.Hide();

            bool _pressed = false;
            Vector3 _origin = Vector3.zero;
            Observable
                .EveryUpdate()
                .Where(
                    _ =>
                        Input.GetMouseButtonDown(0)
                        && (
                            (HoldKey != KeyCode.None && Input.GetKey(HoldKey))
                            || HoldKey == KeyCode.None
                        )
                        && (
                            (NotHoldKey != KeyCode.None && !Input.GetKey(NotHoldKey))
                            || NotHoldKey == KeyCode.None
                        )
                )
                .Subscribe(_ =>
                {
                    _origin = Input.mousePosition;
                    _pressed = true;
                    _resizeImage.Show();
                    if (Handler != null)
                        Handler(0);
                });

            Observable
                .EveryUpdate()
                .Where(
                    _ =>
                        _pressed
                        && (
                            (HoldKey != KeyCode.None && Input.GetKey(HoldKey))
                            || HoldKey == KeyCode.None
                        )
                        && (
                            (NotHoldKey != KeyCode.None && !Input.GetKey(NotHoldKey))
                            || NotHoldKey == KeyCode.None
                        )
                )
                .Subscribe(_ =>
                {
                    Vector3 _min,
                        _max;
                    calcRect(_origin, Input.mousePosition, out _min, out _max);
                    _resizeImage.Resize2Rect(_min, _max);
                    if (Handler != null)
                        Handler(1);
                });

            Observable
                .EveryUpdate()
                .Where(
                    _ =>
                        Input.GetMouseButtonUp(0)
                        && (
                            (HoldKey != KeyCode.None && Input.GetKey(HoldKey))
                            || HoldKey == KeyCode.None
                        )
                        && (
                            (NotHoldKey != KeyCode.None && !Input.GetKey(NotHoldKey))
                            || NotHoldKey == KeyCode.None
                        )
                )
                .Subscribe(_ =>
                {
                    _resizeImage.Hide();
                    _pressed = false;
                    if (Handler != null)
                        Handler(2);
                });
        }

        void calcRect(
            Vector3 InPoint1,
            Vector3 InPoint2,
            out Vector3 _leftLower,
            out Vector3 _rightUpper
        )
        {
            _leftLower = InPoint1;
            _rightUpper = InPoint2;
            if (_rightUpper.x < _leftLower.x)
            {
                _leftLower = InPoint2;
                _rightUpper = InPoint1;
            }

            if (_leftLower.y > _rightUpper.y)
            {
                float _temp = _leftLower.y;
                _leftLower.y = _rightUpper.y;
                _rightUpper.y = _temp;
            }

            _leftLower.x -= Screen.width / 2;
            _leftLower.y -= Screen.height / 2;

            _rightUpper.x -= Screen.width / 2;
            _rightUpper.y -= Screen.height / 2;
        }
    }
}
