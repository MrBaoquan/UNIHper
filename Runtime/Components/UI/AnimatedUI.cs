using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace UNIHper.UI
{
    public enum UIAnimionDriver
    {
        Animator,
        Tweener
    }

    public enum UIAnimationType
    {
        Fly_Up = 100,
        Fly_Right = 101,
        Fly_Down = 102,
        Fly_Left = 103,
        Zoom = 110,
    }

    public class AnimatedUI : UIAnimationBase
    {
        [SerializeField]
        private UIAnimionDriver driver = UIAnimionDriver.Animator;

        [Title("UI Animation Clips")]
        [SerializeField, Required, AssetsOnly, ShowIf("driver", UIAnimionDriver.Animator)]
        private AnimationClip UIEnterClip;

        [SerializeField, Required, AssetsOnly, ShowIf("driver", UIAnimionDriver.Animator)]
        private AnimationClip UILeaveClip;

        [SerializeField]
        [Title("UI Animation Settings"), ShowInInspector, ShowIf("driver", UIAnimionDriver.Tweener)]
        private UIAnimationType enterAnimType = UIAnimationType.Fly_Left;

        [SerializeField]
        [ShowInInspector, PropertyRange(0, 1), ShowIf("driver", UIAnimionDriver.Tweener)]
        private float enterDuration = 0.40f;

        [SerializeField]
        [ShowInInspector, ShowIf("driver", UIAnimionDriver.Tweener)]
        private UIAnimationType exitAnimType = UIAnimationType.Fly_Right;

        [SerializeField]
        [ShowInInspector, PropertyRange(0, 1), ShowIf("driver", UIAnimionDriver.Tweener)]
        private float exitDuration = 0.40f;

        public override Task BuildShowTask()
        {
            return getShowTask();
        }

        public override Task BuildHideTask()
        {
            return getHideTask();
        }

        private UAnimatorOverrideController animatorOverrideController
        {
            get
            {
                var _controller = this.Get<UAnimatorOverrideController>();
                if (_controller is null)
                {
                    _controller = this.AddComponent<UAnimatorOverrideController>();
                }
                return _controller;
            }
        }

        void Reset()
        {
            UIEnterClip = Resources.Load<AnimationClip>("Animations/UI/UIShow");
            UILeaveClip = Resources.Load<AnimationClip>("Animations/UI/UIHide");
        }

        protected override void OnUIAttached()
        {
            recordOriginTransform();
            if (driver == UIAnimionDriver.Animator)
            {
                animatorOverrideController.runtimeAnimatorController =
                    Resources.Load<RuntimeAnimatorController>(
                        "Animations/Controllers/UI_Controller"
                    );
                List<KeyValuePair<AnimationClip, AnimationClip>> _clips =
                    new List<KeyValuePair<AnimationClip, AnimationClip>>();
                new AnimatorOverrideController(
                    animatorOverrideController.runtimeAnimatorController
                ).GetOverrides(_clips);
                animatorOverrideController.animationClipPairs = _clips
                    .Select(
                        _clip =>
                            new AnimationClipPair
                            {
                                originalClip = _clip.Key,
                                overrideClip =
                                    _clip.Key.name == "UIShow" ? UIEnterClip : UILeaveClip
                            }
                    )
                    .ToList();
                animatorOverrideController.Apply();
            }
        }

        private void onUIAttached()
        {
            recordOriginTransform();
        }

        private Task getShowTask()
        {
            if (driver == UIAnimionDriver.Animator)
            {
                // this.Get<Animator> ().SetFloat ("ShowSpeed", enterAnimation.Speed);
                return Observable
                    .Create<Unit>(_observer =>
                    {
                        this.Get<Animator>()
                            .PlayAnimation(
                                "Show",
                                _animator =>
                                {
                                    _observer.OnNext(Unit.Default);
                                    _observer.OnCompleted();
                                }
                            );
                        return new CancellationTokenSource();
                    })
                    .ToTask();
            }
            else if (driver == UIAnimionDriver.Tweener)
            {
                return Observable
                    .Create<Unit>(_observer =>
                    {
                        newFadeTween(enterAnimType, 1, enterDuration)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                _observer.OnNext(Unit.Default);
                                _observer.OnCompleted();
                            });
                        return new CancellationTokenSource();
                    })
                    .ToTask();
            }
            return Task.CompletedTask;
        }

        private Task getHideTask()
        {
            if (driver == UIAnimionDriver.Animator)
            {
                // this.Get<Animator> ().SetFloat ("HideSpeed", exitAnimation.Speed);
                return Observable
                    .Create<Unit>(_observer =>
                    {
                        this.Get<Animator>()
                            .PlayAnimation(
                                "Hide",
                                _animator =>
                                {
                                    _observer.OnNext(Unit.Default);
                                    _observer.OnCompleted();
                                }
                            );
                        return new CancellationTokenSource();
                    })
                    .ToTask();
            }
            else if (driver == UIAnimionDriver.Tweener)
            {
                return Observable
                    .Create<Unit>(_observer =>
                    {
                        newFadeTween(exitAnimType, 2, exitDuration)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                _observer.OnNext(Unit.Default);
                                _observer.OnCompleted();
                            });
                        return new CancellationTokenSource();
                    })
                    .ToTask();
            }
            return Task.CompletedTask;
        }

        private Vector2 m_originAnchoredPosition = Vector2.zero;
        private Vector3 m_originLocalScale = Vector3.zero;

        void recordOriginTransform()
        {
            var _rectTransform = this.Get<RectTransform>();
            m_originAnchoredPosition = _rectTransform.anchoredPosition;
            m_originLocalScale = _rectTransform.localScale;
        }

        Tween newFadeTween(UIAnimationType _type, int InDir, float _duration)
        {
            var _typeNumber = (int)_type;
            var _rectTransform = this.Get<RectTransform>();

            if (_typeNumber < 110)
            {
                if (InDir == 1)
                {
                    _rectTransform.anchoredPosition = getPosition(_type);
                    _rectTransform.localScale = m_originLocalScale;
                    return _rectTransform.DOAnchorPos(m_originAnchoredPosition, _duration);
                }
                else
                {
                    //_rectTransform.anchoredPosition = m_originAnchoredPosition;
                    return _rectTransform.DOAnchorPos(getPosition(_type), _duration);
                }
            }
            else if (_typeNumber < 120)
            {
                if (InDir == 1)
                {
                    _rectTransform.localScale = Vector3.zero;
                    _rectTransform.anchoredPosition = m_originAnchoredPosition;
                    return _rectTransform.DOScale(m_originLocalScale, _duration);
                }
                else
                {
                    //_rectTransform.localScale = Vector3.one;
                    return _rectTransform.DOScale(Vector3.zero, _duration);
                }
            }
            return null;
        }

        Vector2 getPosition(UIAnimationType type)
        {
            var _rectTransform = this.Get<RectTransform>();
            switch (type)
            {
                case UIAnimationType.Fly_Up:
                    return new Vector2(m_originAnchoredPosition.x, _rectTransform.rect.height);
                case UIAnimationType.Fly_Right:
                    return new Vector2(_rectTransform.rect.width, m_originAnchoredPosition.y);
                case UIAnimationType.Fly_Down:
                    return new Vector2(m_originAnchoredPosition.x, -_rectTransform.rect.height);
                case UIAnimationType.Fly_Left:
                    return new Vector2(-_rectTransform.rect.width, m_originAnchoredPosition.y);
                case UIAnimationType.Zoom:
                    return new Vector2(0, 0);
                default:
                    break;
            }
            return Vector2.zero;
        }
    }
}
