using System;
using UnityEngine;
using DG.Tweening;
using UNIHper;
using DigitalRubyShared;
using Sirenix.OdinInspector;

public class SphereLayout : MonoBehaviour
{
    enum BaseAxis
    {
        X,
        Y,
        Z
    }

    [Title("Layout Settings")]
    [SerializeField]
    private BaseAxis baseAxis = BaseAxis.Y;

    [SerializeField]
    public float radius = 1000f;

    [SerializeField]
    public float angleInterval = 60f;

    [SerializeField, HideInInspector]
    private float _defaultAngleInterval = 0;

    [SerializeField]
    public float angleOffset = 0;

    [Title("Display Settings")]
    [SerializeField]
    private Vector2 displayRange = new Vector2(0, 360);

    Indexer indexer = new Indexer();

    private void OnEnable()
    {
        alignToCurrent();
    }

    private void OnValidate()
    {
        _defaultAngleInterval = angleInterval;
        RegenerateLayout();
    }

    public int SelectNext()
    {
        indexer.Next();
        alignToCurrent();
        return indexer.Current;
    }

    public int SelectPrev()
    {
        indexer.Prev();
        alignToCurrent();
        return indexer.Current;
    }

    public void Select(int ChildIndex)
    {
        indexer.Set(ChildIndex);
        alignToCurrent();
    }

    public void Collapse(bool forceCollapse = false)
    {
        if (forceCollapse)
            angleInterval = _defaultAngleInterval;
        if (angleInterval == 0)
            return;
        DOInterval(0, 0.5f);
    }

    public void Expand(bool forceExpand = false)
    {
        if (forceExpand)
            angleInterval = 0;
        if (angleInterval == _defaultAngleInterval)
            return;
        DOInterval(_defaultAngleInterval, 0.35f);
    }

    public float ItemAngle(int ChildIndex)
    {
        return angleInterval * ChildIndex + angleOffset;
    }

    public void DOInterval(float endInterval, float duration = 0.35f, Action callback = null)
    {
        DOTween
            .To(
                () => angleInterval,
                _ =>
                {
                    angleInterval = _;
                    alignToCurrent();
                },
                endInterval,
                duration
            )
            .OnComplete(() =>
            {
                if (callback != null)
                    callback();
            });
    }

    public void RegenerateLayout()
    {
        Vector3 _startPoint = Vector3.zero;
        switch (baseAxis)
        {
            case BaseAxis.X:
                _startPoint = transform.position + transform.forward * -radius;
                break;
            case BaseAxis.Y:
                _startPoint = transform.position + transform.right * -radius;
                break;
            case BaseAxis.Z:
                _startPoint = transform.position + transform.up * -radius;
                break;
        }
        // transform.position + transform.right * -distance;
        int _index = 0;
        var _children = gameObject.Children();
        _children.ForEach(_transform =>
        {
            _transform.position = _startPoint;
            switch (baseAxis)
            {
                case BaseAxis.X:
                    _transform.RotateAround(
                        transform.position,
                        -transform.right,
                        angleInterval * _index + angleOffset
                    );
                    break;
                case BaseAxis.Y:
                    _transform.RotateAround(
                        transform.position,
                        -transform.up,
                        angleInterval * _index + angleOffset
                    );
                    break;
                case BaseAxis.Z:
                    _transform.RotateAround(
                        transform.position,
                        transform.forward,
                        angleInterval * _index + angleOffset
                    );
                    break;
            }

            var _euler = transform.eulerAngles;
            _transform.eulerAngles = Vector3.zero;
            ++_index;
        });
    }

    Vector3 eulerAngle(float InAxisX)
    {
        switch (baseAxis)
        {
            case BaseAxis.X:
                return new Vector3(InAxisX, 0, 0);
            case BaseAxis.Y:
                return new Vector3(0, InAxisX, 0);
            case BaseAxis.Z:
                return new Vector3(0, 0, InAxisX);
            default:
                return Vector3.zero;
        }
    }

    void alignToCurrent()
    {
        transform
            .DOLocalRotateQuaternion(
                Quaternion.Euler(eulerAngle(indexer.Current * angleInterval)),
                0.35f
            )
            .OnUpdate(() =>
            {
                RegenerateLayout();
            });
    }

    private void addOffsetRotation(float InAngle)
    {
        var _rotation = Quaternion.Euler(Vector3.zero);
        switch (baseAxis)
        {
            case BaseAxis.X:
                _rotation = Quaternion.Euler(new Vector3(InAngle, 0, 0));
                break;
            case BaseAxis.Y:
                _rotation = Quaternion.Euler(new Vector3(0, InAngle, 0));
                break;
            case BaseAxis.Z:
                _rotation = Quaternion.Euler(new Vector3(0, 0, InAngle));
                break;
        }
        transform.rotation = startRotation * _rotation;
    }

    private Quaternion startRotation;

    [SerializeField]
    private bool enableDragRotation = true;

    private void Start()
    {
        indexer.Loop = false;
        indexer.SetMin((int)displayRange.x);
        indexer.SetMax((int)displayRange.y);
        startRotation = transform.rotation;
        RegenerateLayout();
        var _moveGesture = new PanGestureRecognizer();

        _moveGesture.StateUpdated += (InGesture) =>
        {
            if (InGesture.State == GestureRecognizerState.Began)
            {
                startRotation = transform.rotation;
            }
            else if (InGesture.State == GestureRecognizerState.Executing)
            {
                var _pan = InGesture as PanGestureRecognizer;
                var _startPos = new Vector3(_pan.StartFocusX, 1080 - _pan.StartFocusY, 0);
                var _curPos = new Vector3(_pan.FocusX, 1080 - _pan.FocusY, 0);

                var _startDir = _startPos - transform.position;
                var _curDir = _curPos - transform.position;
                var _deltaAngle = Vector3.Angle(_startDir, _curDir);
                var _cross = Vector3.Cross(_startDir, _curDir);
                if (_cross.z > 0)
                {
                    _deltaAngle = -_deltaAngle;
                }
                addOffsetRotation(_deltaAngle);
                RegenerateLayout();
            }
            else if (InGesture.State == GestureRecognizerState.Ended)
            {
                var _pan = InGesture as PanGestureRecognizer;
                var _startPos = new Vector3(_pan.StartFocusX, 1080 - _pan.StartFocusY, 0);
                var _curPos = new Vector3(_pan.FocusX, 1080 - _pan.FocusY, 0);

                var _startDir = _startPos - transform.position;
                var _curDir = _curPos - transform.position;
                var _deltaAngle = Vector3.Angle(_startDir, _curDir);
                var _cross = Vector3.Cross(_startDir, _curDir);
                if (_cross.z > 0)
                {
                    _deltaAngle = -_deltaAngle;
                }
                var _deltaIndex = Mathf.RoundToInt(_deltaAngle / angleInterval);
                indexer.Set(indexer.Current + _deltaIndex);
                alignToCurrent();
            }
        };
        if (enableDragRotation)
            FingersScript.Instance.AddGesture(_moveGesture);
        FingersScript.Instance.ShowTouches = false;
    }
}
