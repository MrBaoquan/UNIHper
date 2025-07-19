using System;
using UnityEngine;
using DG.Tweening;
using UNIHper;
using DigitalRubyShared;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UniRx;
using UnityEngine.Events;
using System.Net.Http.Headers;

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
    public GameObject ItemTemplate;
    public int MaxItemsCount = 5;

    public int ActiveItemIndex = 0;

    [SerializeField]
    public float angleOffset = 0;

    [Title("Display Settings")]
    Indexer angleIndexer = new Indexer();

    private void OnEnable()
    {
        alignToCurrent();
    }

    private void OnValidate()
    {
        _defaultAngleInterval = angleInterval;
        RegenerateLayout();
    }

    int offsetIdx = 0;

    private Transform _itemTemplate
    {
        get
        {
            if (ItemTemplate != null)
                return ItemTemplate.transform;
            if (transform.GetChild(0) != null)
                return transform.GetChild(0);

            Debug.LogError("ItemTemplate is not set and no child found in SphereLayout.");
            return null;
        }
    }

    private UnityEvent<Transform, int> onCreatingItem = new UnityEvent<Transform, int>();

    // 在前面追加一个
    public void InsertBefore()
    {
        var _newItem = GameObject.Instantiate(_itemTemplate, transform);
        var _itemID = leftBorderIndexer.PrevValue();
        _newItem.name = $"Item_{_itemID}";

        onCreatingItem.Invoke(_newItem, _itemID);

        offsetIdx--;
        _newItem.SetAsFirstSibling();
        alignToCurrent();
        if (transform.childCount > MaxItemsCount)
        {
            DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
        }
    }

    // 在后面追加一个
    public void InsertAfter()
    {
        var _newItem = GameObject.Instantiate(_itemTemplate, transform);
        var _itemID = rightBorderIndexer.NextValue();

        _newItem.name = $"Item_{_itemID}";
        onCreatingItem.Invoke(_newItem, _itemID);

        offsetIdx++;
        _newItem.SetAsLastSibling();
        alignToCurrent();
        if (transform.childCount > MaxItemsCount)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public int SelectNext()
    {
        InsertBefore();
        angleIndexer.Next();

        ItemIndexer.Prev();

        // Debug.Log($"Range: {leftBorderIndexer.Current} - {rightBorderIndexer.Current}");

        alignToCurrent();
        return angleIndexer.Current;
    }

    public int SelectPrev()
    {
        InsertAfter();
        angleIndexer.Prev();

        ItemIndexer.Next();

        // Debug.Log($"Range: {leftBorderIndexer.Current} - {rightBorderIndexer.Current}");

        alignToCurrent();
        return angleIndexer.Current;
    }

    public void Select(int ChildIndex)
    {
        var (step, dir) = ItemIndexer.MinStepForValue(ChildIndex);

        if (dir > 0)
        {
            for (var i = 0; i < step; i++)
            {
                SelectPrev();
            }
        }
        else if (dir < 0)
        {
            for (var i = 0; i < step; i++)
            {
                SelectNext();
            }
        }
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
        int _index = 0 + offsetIdx;
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

            _transform.localEulerAngles = new Vector3(
                0,
                0,
                angleInterval * _index + angleOffset - 90
            );
            // _transform.Get<RectTransform>().AlignLeftEdgePerpendicularToOrigin(transform.position);
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

    public float fadeDuration = 1.0f;
    public Ease fadeEase = Ease.InOutCubic;

    void alignToCurrent()
    {
        transform
            .DOLocalRotateQuaternion(
                Quaternion.Euler(eulerAngle(angleIndexer.Current * angleInterval)),
                fadeDuration
            )
            .SetEase(fadeEase)
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

    public Indexer ItemIndexer { get; private set; } = new Indexer();

    public IObservable<Transform> OnItemSelectedAsObservable()
    {
        return ItemIndexer
            .OnValueChangedAsObservable()
            .Select(_ => transform.GetChild(transform.childCount - 1));
    }

    public IObservable<(Transform Item, int ID)> OnCreatingNewItemAsObservable()
    {
        return onCreatingItem.AsObservable().Select(_ => (_.Item1, _.Item2));
    }

    Indexer leftBorderIndexer = new Indexer();

    Indexer rightBorderIndexer = new Indexer();

    private void Start()
    {
        angleIndexer.Loop = false;
        angleIndexer.SetMin(int.MinValue);
        angleIndexer.SetMax(int.MaxValue);

        ItemIndexer.Loop = true;
        ItemIndexer.SetMax(MaxItemsCount - 1);
        leftBorderIndexer.Loop = true;
        leftBorderIndexer.SetMax(MaxItemsCount - 1);
        rightBorderIndexer.Loop = true;
        rightBorderIndexer.SetMax(MaxItemsCount - 1);

        ItemIndexer.SetValueWithoutNotify(ActiveItemIndex);
        leftBorderIndexer.SetValueWithoutNotify(0);
        rightBorderIndexer.SetValueWithoutNotify(MaxItemsCount - 1);

        ItemIndexer
            .OnPrevAsObservable()
            .Subscribe(_ =>
            {
                leftBorderIndexer.Prev();
                rightBorderIndexer.Prev();
            });

        ItemIndexer
            .OnNextAsObservable()
            .Subscribe(_ =>
            {
                leftBorderIndexer.Next();
                rightBorderIndexer.Next();
            });

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
                angleIndexer.Set(angleIndexer.Current + _deltaIndex);
                alignToCurrent();
            }
        };
        if (enableDragRotation)
            FingersScript.Instance.AddGesture(_moveGesture);
        FingersScript.Instance.ShowTouches = false;
    }
}
