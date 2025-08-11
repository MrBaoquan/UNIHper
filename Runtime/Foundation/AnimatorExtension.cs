using System;
using System.Linq;
using UnityEngine;

namespace UNIHper
{
    using System.Threading.Tasks;

    using UniRx;

    public static class AnimatorExtension
    {
        public static AnimationClip GetClip(this Animator animator, string InName)
        {
            var _animClips = animator.runtimeAnimatorController.animationClips;
            return _animClips
                .Where(_ =>
                {
                    return _.name == InName;
                })
                .FirstOrDefault();
        }

        public static Task PlayAndWait(this Animator animator, string stateName)
        {
            animator.Play(stateName, 0, 0);
            return Observable
                .EveryUpdate()
                .Where(
                    _ =>
                        animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)
                        && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                )
                .First()
                .ToTask();
        }

        public static IObservable<Animator> OnReadyAsObservable(this Animator animator)
        {
            return Observable.EveryUpdate().Where(_ => animator.IsReady()).First().Select(_ => animator);
        }

        public static void SeekToFrame(this Animator animator, string stateName, int frame, Action onCompleted = null)
        {
            if (animator.IsValid() == false)
                return;

            animator
                .OnReadyAsObservable()
                .Subscribe(_ =>
                {
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                    {
                        animator.SeekToFrame(frame);
                        onCompleted?.Invoke();
                    }
                    else
                    {
                        animator
                            .SwitchAsObservable(stateName)
                            .Subscribe(_ =>
                            {
                                animator.SeekToFrame(frame);
                                onCompleted?.Invoke();
                            });
                    }
                })
                .AddTo(UNIHperEntry.Instance);
        }

        public static IObservable<Animator> SeekToFrameAsObservable(this Animator animator, int frame)
        {
            if (animator.IsValid() == false)
            {
                return Observable.Return(animator);
            }

            return animator
                .OnReadyAsObservable()
                .SelectMany(_ =>
                {
                    var _currentClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];
                    var _pasueTime = frame * 1f / _currentClipInfo.clip.frameRate;
                    return animator.SeekAsObservable(_pasueTime);
                });
        }

        public static IObservable<Animator> SeekToFrameAsObservable(this Animator animator, string stateName, int frame)
        {
            if (animator.IsValid() == false)
                return Observable.Return(animator);

            return animator
                .OnReadyAsObservable()
                .SelectMany(_ =>
                {
                    return animator.SwitchAsObservable(stateName).SelectMany(_ => animator.SeekToFrameAsObservable(frame));
                });
        }

        public static IDisposable Seek(this Animator animator, string stateName, float time, Action onCompleted = null)
        {
            if (animator.IsValid() == false)
                return Disposable.Empty;

            return animator
                .OnReadyAsObservable()
                .Subscribe(_ =>
                {
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                    {
                        animator.Seek(time, onCompleted);
                    }
                    else
                    {
                        animator
                            .SwitchAsObservable(stateName)
                            .Subscribe(_ =>
                            {
                                animator.Seek(time, onCompleted);
                                onCompleted?.Invoke();
                            });
                    }
                })
                .AddTo(UNIHperEntry.Instance);
        }

        public static bool IsReady(this Animator animator)
        {
            return animator.gameObject.activeInHierarchy;
        }

        public static bool IsValid(this Animator animator)
        {
            return animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0;
        }

        public static IDisposable SeekToFrame(this Animator animator, int frame, Action onCompleted = null)
        {
            if (animator.IsValid() == false)
                return Disposable.Empty;

            var _currentClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];
            var _pasueTime = frame * 1f / _currentClipInfo.clip.frameRate;
            // var _normalizedTime = _pasueTime / _currentClipInfo.clip.length;
            return animator.Seek(_pasueTime, onCompleted);
        }

        public static IDisposable Seek(this Animator animator, float time, Action onCompleted = null)
        {
            if (animator.IsValid() == false)
                return Disposable.Empty;

            return SeekAsObservable(animator, time).Subscribe(_ => onCompleted?.Invoke());
        }

        public static IObservable<Animator> SeekAsObservable(this Animator animator, float time) =>
            animator
                .OnReadyAsObservable()
                .SelectMany(_ =>
                {
                    var _currentClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];
                    var _normalizedTime = time / _currentClipInfo.clip.length;
                    animator.speed = 0;
                    var _stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    animator.Play(_stateInfo.fullPathHash, 0, _normalizedTime);
                    return Observable.Return(animator);
                });

        public static float CurrentTime(this Animator animator) => animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        public static void Pause(this Animator animator)
        {
            animator.speed = 0;
        }

        public static void Rewind(this Animator animator)
        {
            animator.Rebind();
            animator.Play();
        }

        public static void Play(this Animator animator)
        {
            animator.speed = 1;
        }

        public static IDisposable Switch(this Animator animator, string stateName, Action onCompleted = null)
        {
            animator.speed = 1;
            return animator.SwitchAsObservable(stateName).Subscribe(_ => onCompleted?.Invoke());
        }

        public static IObservable<Animator> SwitchAsObservable(this Animator animator, string stateName)
        {
            if (animator.IsValid() == false)
                return Observable.Return(animator);

            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                return Observable.Return(animator);

            if (animator.gameObject.activeInHierarchy == false)
            {
                return Observable.Return(animator);
            }

            animator.Play(stateName, 0, 0);
            return Observable
                .EveryUpdate()
                .Where(_ => animator.IsReady() && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                .First()
                .Select(_ => animator);
        }

        public static IDisposable PlayToEnd(this Animator animator, string stateName, Action<Animator> onCompleted = null)
        {
            return PlayToEndAsObservable(animator, stateName).Subscribe(onCompleted);
        }

        public static IDisposable PlayThenHide(this Animator animator, string stateName, Action<Animator> onCompleted = null)
        {
            return PlayToEnd(
                    animator,
                    stateName,
                    _ =>
                    {
                        animator.Rebind();
                        animator.gameObject.SetActive(false);
                        onCompleted?.Invoke(animator);
                    }
                )
                .AddTo(UNIHperEntry.Instance);
        }

        public static IDisposable PlayThenNext(this Animator animator, string stateName, string nextStateName)
        {
            return PlayToEnd(
                    animator,
                    stateName,
                    _ =>
                    {
                        animator.Play(nextStateName, 0, 0);
                    }
                )
                .AddTo(UNIHperEntry.Instance);
        }

        public static IObservable<Animator> PlayToEndAsObservable(
            this Animator animator,
            string stateName,
            Func<Animator, bool> condition = null
        )
        {
            return SwitchAsObservable(animator, stateName)
                .SelectMany(
                    _animator =>
                        Observable
                            .EveryUpdate()
                            .Where(_ => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                            .TakeWhile(_ => (condition == null || condition(_animator)))
                            .First()
                            .Select(_ => _animator)
                            .Catch<Animator, Exception>(ex =>
                            {
                                return Observable.Return<Animator>(null);
                            })
                            .Where(_ => _ != null)
                );
        }

        // csharpier-ignore
        public static IDisposable Play(this Animator animator, string stateName, Action<Animator> onCallback = null)
        {
            if(animator.gameObject.activeInHierarchy == false)
                return Disposable.Empty;
            
            return PlayToEndAsObservable(animator, stateName)
                .Subscribe(_ => onCallback?.Invoke(animator));
        }

        public static bool IsState(this Animator animator, string stateName, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }
    }
}
