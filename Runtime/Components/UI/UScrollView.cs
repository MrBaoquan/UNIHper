using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DNHper;
using PathologicalGames;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UNIHper
{
    using UniRx;
    using UniRx.Triggers;

    public enum ScrollLayout
    {
        Horizontal_Normal,
        Horizontal_Loop
    }

    [RequireComponent(typeof(ScrollRect))]
    public class UScrollView : MonoBehaviour
    {
        public int MaxShowCount = 5;
        public ScrollLayout scrollLayout = ScrollLayout.Horizontal_Normal;
        ScrollRect scrollRect;
        RectTransform viewport = null;
        RectTransform content = null;

        SpawnPool menuPool = null;
        GameObject menuPoolRoot = null;

        public float _max = 200;
        public int cacheItemsCount = 15;

        [HideInInspector]
        RectTransform rectTransform = null;

        public List<Transform> Prefabs = new List<Transform>();

        private void Awake()
        {
            rectTransform = this.GetComponent<RectTransform>();

            menuPoolRoot = new GameObject("scroll item pool");
            menuPool = PoolManager.Pools.Create("MenuItem", menuPoolRoot);
            scrollRect = this.GetComponent<ScrollRect>();
            viewport = scrollRect.viewport;
            content = scrollRect.content;
        }

        public void DestoryAll()
        {
            menuPool.DespawnAll();
            Debug.Log("子节点数量 " + contentChildCount);
        }

        private int contentChildCount
        {
            get => content.Children().Count;
        }

        public void AddItem(Transform InItem, string InName = "")
        {
            if (InName == "")
                InItem.name = string.Format("item_{0}", Prefabs.Count);
            else
                InItem.name = InName;
            menuPool.CreatePrefabPool(new PrefabPool(InItem));
            Prefabs.Add(InItem);
        }

        public void InitItems()
        {
            if (scrollLayout == ScrollLayout.Horizontal_Normal)
            {
                spawnNormalLayout();
                Observable
                    .NextFrame()
                    .Subscribe(_ =>
                    {
                        Debug.LogFormat("max show count:{0}", MaxShowCount);
                        //ScrollTo (MaxShowCount - 1);
                        ScrollTo(HalfShowCount);
                    });
            }
            else if (scrollLayout == ScrollLayout.Horizontal_Loop)
            {
                spawnLoopLayout();
                Observable
                    .NextFrame()
                    .Subscribe(_ =>
                    {
                        scrollRect.ScrollToCenter(
                            content.GetChild(cacheItemsCount) as RectTransform
                        );
                    });
            }
        }

        public int ScrollToPrev()
        {
            int _prevID = ClosetCenterItemIndex() - 1;
            if (_prevID < HalfShowCount)
            {
                _prevID = HalfShowCount;
            }
            ScrollTo(_prevID);
            return _prevID;
        }

        public int ScrollToNext()
        {
            int _nextID = ClosetCenterItemIndex() + 1;
            if (_nextID > (contentChildCount - HalfShowCount - 1))
            {
                _nextID = contentChildCount - HalfShowCount - 1;
            }
            ScrollTo(_nextID);
            return _nextID;
        }

        public int HalfShowCount
        {
            get { return Mathf.FloorToInt(MaxShowCount / 2); }
        }

        public void ScrollTo(int InIndex)
        {
            if (InIndex <= HalfShowCount)
            {
                InIndex = HalfShowCount;
                Debug.Log("DBUG 1");
            }
            else if (InIndex > (contentChildCount - HalfShowCount - 1))
            {
                InIndex = contentChildCount - HalfShowCount - 1;
                Debug.Log("DBUG 2");
            }

            Debug.Log($"halfcount {HalfShowCount}, 现在是 --  " + InIndex);

            var _transform = content.Children().Skip(InIndex).First();
            float _position = scrollRect.GetItemNormallizedPosition(_transform as RectTransform);
            DOTween
                .To(
                    () => scrollRect.horizontalNormalizedPosition,
                    _ =>
                    {
                        scrollRect.horizontalNormalizedPosition = _;
                    },
                    _position,
                    0.15f
                )
                .OnComplete(() =>
                {
                    OnRetargetEvent.Invoke(_transform as RectTransform);
                });
        }

        public IObservable<Transform> OnRetargetEventAsObservable()
        {
            return OnRetargetEvent.AsObservable();
        }

        private void spawnNormalLayout()
        {
            Prefabs.ForEach(_ =>
            {
                spawnNext();
            });
            var _halfCount = HalfShowCount;
            for (int _index = 0; _index < _halfCount; ++_index)
            {
                var _newPrev = spawnPrev();
                var _newNext = spawnNext();
                _newPrev.SendMessage("DoClear", SendMessageOptions.DontRequireReceiver);
                _newNext.SendMessage("DoClear", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void spawnLoopLayout()
        {
            spawnItem(0);
            for (int _index = 0; _index < cacheItemsCount; ++_index)
            {
                spawnPrev();
                spawnNext();
            }
        }

        private int getItemID(Transform InItem)
        {
            if (InItem == null)
            {
                return 0;
            }
            return InItem.name.Replace("item_", string.Empty).Parse2Int();
        }

        private int nextID(int InID)
        {
            int _next = InID + 1;
            if (_next >= Prefabs.Count)
            {
                _next = 0;
            }
            return _next;
        }

        private int prevID(int InID)
        {
            int _prev = InID - 1;
            if (_prev < 0)
            {
                _prev = Prefabs.Count - 1;
            }
            return _prev;
        }

        private Transform spawnNext()
        {
            var _children = content.Children(true);
            int _currentID = _children.Count <= 0 ? -1 : getItemID(_children.Last());
            var _newItem = spawnItem(nextID(_currentID));
            _newItem.SetAsLastSibling();
            return _newItem;
        }

        private Transform spawnPrev()
        {
            var _children = content.Children(true);
            int _currentID = _children.Count <= 0 ? 0 : getItemID(_children.First());
            var _newItem = spawnItem(prevID(_currentID));
            _newItem.SetAsFirstSibling();
            return _newItem;
        }

        private Transform spawnItem(int InID)
        {
            Debug.Log("spawn " + InID);
            var _item = menuPool.Spawn(Prefabs[InID], content);
            _item.name = string.Format("item_{0}", InID);
            return _item;
        }

        public float NormalizedUnitPosition(float InOffset)
        {
            var _bounds = (content.Children().First() as RectTransform).TransformBoundsTo(viewport);
            Bounds _contentBounds = content.TransformBoundsTo(viewport);
            Bounds _viewBounds = new Bounds(viewport.rect.center, viewport.rect.size);
            var _hiddenLength = _contentBounds.size[0] - _viewBounds.size[0]; // contentBounds.size[axis] - viewBounds.size[axis];
            return InOffset / _hiddenLength;
        }

        public Transform ClosetCenterItem()
        {
            return content.gameObject
                .Children()
                .OrderBy(_ =>
                {
                    var _bounds = (_ as RectTransform).TransformBoundsTo(viewport);
                    return Mathf.Abs(_bounds.center.x - viewport.rect.center.x);
                })
                .Take(1)
                .First();
        }

        public int ClosetCenterItemIndex()
        {
            return ClosetCenterItem().GetSiblingIndex();
        }

        public UnityEvent<RectTransform> OnRetargetEvent = null;

        /// 事件穿透
        // private void PassEvent<T>(PointerEventData data, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
        // {
        //     var results = new List<RaycastResult>();
        //     EventSystem.current.RaycastAll(data, results);
        //     var current = data.pointerCurrentRaycast.gameObject;
        //     for (int i = 0; i < results.Count; i++)
        //     {
        //         //判断穿透对象是否是需要要点击的对象
        //         if (results[i].gameObject.name=="Scroll View")
        //         {
        //             ExecuteEvents.Execute(results[i].gameObject, data, function);
        //         }
        //     }
        // }

        void Start()
        {
            // 如果字元素阻挡了ScrollView  则需要进行事件穿透
            IDisposable _stopHandler = null;
            scrollRect
                .OnPointerDownAsObservable()
                .Subscribe(_ =>
                {
                    if (_stopHandler != null)
                    {
                        _stopHandler.Dispose();
                        _stopHandler = null;
                    }
                    scrollRect.StopMovement();
                    _stopHandler = Observable
                        .Timer(TimeSpan.FromSeconds(0.3))
                        .Subscribe(_1 =>
                        {
                            var _rectTransform = ClosetCenterItem() as RectTransform;
                            ScrollTo(_rectTransform.GetSiblingIndex());
                            _stopHandler = null;
                        });
                });

            scrollRect
                .OnBeginDragAsObservable()
                .Subscribe(_ =>
                {
                    if (_stopHandler != null)
                    {
                        _stopHandler.Dispose();
                        _stopHandler = null;
                    }
                });

            scrollRect
                .OnEndDragAsObservable()
                .Subscribe(_ =>
                {
                    if (_stopHandler != null)
                        _stopHandler.Dispose();
                    _stopHandler = Observable
                        .EveryUpdate()
                        .Where(_1 => Mathf.Abs(scrollRect.velocity.x) <= 1000f)
                        .Subscribe(_2 =>
                        {
                            try
                            {
                                var _rectTransform = ClosetCenterItem() as RectTransform;
                                ScrollTo(
                                    _rectTransform.GetSiblingIndex()
                                        - content
                                            .Children(false)
                                            .Where(_3 => !_3.gameObject.activeInHierarchy)
                                            .Count()
                                );
                                _stopHandler.Dispose();
                                _stopHandler = null;
                            }
                            catch (System.Exception)
                            {
                                _stopHandler.Dispose();
                                _stopHandler = null;
                            }
                        });
                });
        }

        private void refreshItems()
        {
            var _center = ClosetCenterItem();
            syncLeft(_center);
            syncRight(_center);
        }

        private int syncLeft(Transform InCenter)
        {
            int InDelta = cacheItemsCount - InCenter.GetSiblingIndex();
            var _absDetla = Mathf.Abs(InDelta);
            if (InDelta > 0)
            {
                Debug.LogFormat("左边补充{0}个", _absDetla);
                for (int _index = 0; _index < _absDetla; ++_index)
                {
                    spawnPrev();
                }
            }
            else if (InDelta < 0)
            {
                Debug.LogFormat("左边移出{0}个", _absDetla);
                for (int _index = 0; _index < _absDetla; ++_index)
                {
                    menuPool.Despawn(content.Children().First(), menuPoolRoot.transform);
                }
            }
            return _absDetla;
        }

        private void syncRight(Transform InCenter)
        {
            int InDelta = (contentChildCount - 1 - InCenter.GetSiblingIndex()) - cacheItemsCount;
            var _absDetla = Mathf.Abs(InDelta);
            if (InDelta > 0)
            {
                Debug.LogFormat("右边移出{0}个", _absDetla);
                for (int _index = 0; _index < _absDetla; ++_index)
                {
                    menuPool.Despawn(content.Children().Last(), menuPoolRoot.transform);
                }
            }
            else if (InDelta < 0)
            {
                Debug.LogFormat("右边补充{0}个", _absDetla);
                for (int _index = 0; _index < _absDetla; ++_index)
                {
                    spawnNext();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            content.gameObject
                .Children()
                .ForEach(rectTransform =>
                {
                    var _bounds = (rectTransform as RectTransform).TransformBoundsTo(viewport);
                    float _distance = Mathf.Abs(_bounds.center.x - viewport.rect.center.x);
                    float _basicScale = 0.75f;
                    float _deltaScale = 0f;
                    if (_distance <= _max)
                    {
                        float _percent = Mathf.Clamp(_distance / _max, 0, 1);
                        _deltaScale = (1 - _percent) * 0.55f;
                        // isCenter = true;
                    }
                    else
                    {
                        // isCenter = false;
                    }
                    rectTransform.localScale = Vector3.one * (_basicScale + _deltaScale);
                });
        }
    }
}
