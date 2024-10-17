using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace UNIHper
{
    public class MultipleTouchManager : SingletonBehaviourDontDestroy<MultipleTouchManager>
    {
        private UnityEvent<Finger> onFingerMove = new UnityEvent<Finger>();
        private UnityEvent<Finger> onFingerDown = new UnityEvent<Finger>();
        private UnityEvent<Finger> onFingerUp = new UnityEvent<Finger>();

        private UnityEvent<Finger, float> onZoom = new UnityEvent<Finger, float>();

        public IObservable<Finger> OnFingerUpAsObservable()
        {
            return onFingerUp.AsObservable();
        }

        public IObservable<Finger> OnFingerDownAsObservable()
        {
            return onFingerDown.AsObservable();
        }

        public IObservable<Finger> OnFingerMoveAsObservable()
        {
            return onFingerMove.AsObservable();
        }

        public IObservable<Tuple<Finger, float>> OnZoomAsObservable()
        {
            return onZoom.AsObservable();
        }

        private void OnEnable()
        {
            TouchSimulation.Enable();
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }

        private Dictionary<int, int> _zoomPairDict = new Dictionary<int, int>();
        private Dictionary<int, float> _zoomDeltaDict = new Dictionary<int, float>();

        public bool TrueIfUnbind(int touchID)
        {
            return !_zoomPairDict.ContainsKey(touchID) && !_zoomPairDict.ContainsValue(touchID);
        }

        private bool trueIfBind(int touchID)
        {
            return _zoomPairDict.ContainsKey(touchID) || _zoomPairDict.ContainsValue(touchID);
        }

        private (int firstTouchID, int secondTouchdID) getPairTouchID(int touchID)
        {
            int _firstID = -1,
                _secondID = -1;
            if (_zoomPairDict.ContainsKey(touchID))
            {
                _firstID = touchID;
                _secondID = _zoomPairDict[_firstID];
                _firstID = _zoomPairDict.Where(_pair => _pair.Value == _secondID).First().Key;
            }
            Debug.Log($"FirstID: {_firstID}, SecondID: {_secondID}");
            return (_firstID, _secondID);
        }

        // Start is called before the first frame update
        void Start()
        {
            var _touchScreen = Touchscreen.current;

            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += ctx =>
            {
                onFingerDown.Invoke(ctx);

                var _touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.ToList();
                _touches = _touches.Where(_touch => TrueIfUnbind(_touch.touchId)).ToList();
                if (_touches.Count < 2)
                    return;

                var _first = _touches.First();
                var _others = _touches.Except(
                    new List<UnityEngine.InputSystem.EnhancedTouch.Touch>() { _first }
                );
                var _second = _others
                    .OrderBy(_touch => (_touch.screenPosition - _first.screenPosition).sqrMagnitude)
                    .FirstOrDefault();
                var _distance = (_first.screenPosition - _second.screenPosition).sqrMagnitude;
                _zoomPairDict.Add(_first.touchId, _second.touchId);
                _zoomDeltaDict.Add(_first.touchId, _distance);
            };

            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += ctx =>
            {
                onFingerUp.Invoke(ctx);

                if (TrueIfUnbind(ctx.index))
                    return;
                var _touchPairID = getPairTouchID(ctx.index);
                _zoomPairDict.Remove(_touchPairID.firstTouchID);
                _zoomDeltaDict.Remove(_touchPairID.firstTouchID);
            };

            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += ctx =>
            {
                onFingerMove.Invoke(ctx);
                if (TrueIfUnbind(ctx.index))
                    return;
                var _touchPairID = getPairTouchID(ctx.index);
                var _firstTouch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches
                    .Where(_touch => _touch.touchId == _touchPairID.firstTouchID)
                    .First();
                var _secondTouch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches
                    .Where(_touch => _touch.touchId == _touchPairID.secondTouchdID)
                    .First();
                var _lastDis = _zoomDeltaDict[_touchPairID.firstTouchID];
                var _curDis = (
                    _firstTouch.screenPosition - _secondTouch.screenPosition
                ).sqrMagnitude;
                var _delta = _curDis - _lastDis;
                _zoomDeltaDict[_touchPairID.firstTouchID] = _curDis;
                onZoom.Invoke(_firstTouch.finger, _delta);
            };
        }

        // Update is called once per frame
        void Update() { }
    }
}
