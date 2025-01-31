using System;
using System.Linq;
using UnityEngine;

namespace UNIHper
{
    using System.Threading;

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

        public static void SeekToFrame(
            this Animator animator,
            string stateName,
            int frame,
            Action onCompleted = null
        )
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                animator.SeekToFrame(frame);
            }
            else
            {
                animator
                    .SwitchStateAsObservable(stateName)
                    .Subscribe(_ =>
                    {
                        animator.SeekToFrame(frame);
                        onCompleted?.Invoke();
                    });
            }
        }

        public static void Seek(
            this Animator animator,
            string stateName,
            float time,
            Action onCompleted = null
        )
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                animator.Seek(time);
            }
            else
            {
                animator
                    .SwitchStateAsObservable(stateName)
                    .Subscribe(_ =>
                    {
                        animator.Seek(time);
                        onCompleted?.Invoke();
                    });
            }
        }

        public static void SeekToFrame(this Animator animator, int frame)
        {
            var _currentClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];
            var _pasueTime = frame * 1f / _currentClipInfo.clip.frameRate;

            var _normalizedTime = _pasueTime / _currentClipInfo.clip.length;

            animator.speed = 0;

            var _stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(_stateInfo.fullPathHash, 0, _normalizedTime);
        }

        public static void Seek(this Animator animator, float time)
        {
            var _currentClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];
            var _normalizedTime = time / _currentClipInfo.clip.length;
            animator.speed = 0;
            var _stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(_stateInfo.fullPathHash, 0, _normalizedTime);
        }

        public static float CurrentTime(this Animator animator) =>
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

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

        public static IObservable<Animator> SwitchStateAsObservable(
            this Animator animator,
            string stateName
        )
        {
            animator.Play(stateName, 0, 0);
            return Observable
                .EveryUpdate()
                .Where(_ => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                .First()
                .Select(_ => animator);
        }

        public static IObservable<Animator> PlayToEndAsObservable(
            this Animator animator,
            string stateName
        )
        {
            return SwitchStateAsObservable(animator, stateName)
                .SelectMany(
                    _animator =>
                        Observable
                            .EveryUpdate()
                            .Where(
                                _ => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                            )
                            .First()
                            .Select(_ => _animator)
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
