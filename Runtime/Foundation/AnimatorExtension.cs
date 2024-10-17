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

        // csharpier-ignore
        public static void Play(this Animator animator, string stateName, Action<Animator> onCallback = null)
        {
            if(animator.gameObject.activeInHierarchy == false)
                return;
            
            animator.Play(stateName, 0, 0);
            syncPlayState(animator);
            
            Observable
                .NextFrame()
                .Subscribe(_ =>
                {
                    float _duration = animator.GetCurrentAnimatorStateInfo(0).length;
                    if(_duration <= 0){
                        Debug.LogWarning("Animator duration is zero.");
                        onCallback?.Invoke(animator);
                        return;
                    }
                    Observable
                        .Timer(TimeSpan.FromSeconds(_duration))
                        .Subscribe(_1 =>
                        {
                            onCallback?.Invoke(animator);
                        });
                });
        }

        public static bool IsState(this Animator animator, string stateName, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }

        public static void Stop(this Animator animator) { }

        // private methods
        private static void syncPlayState(Animator animator)
        {
            //animator.SetBool ("Stop", false);
        }

        private static void syncStopState(Animator animator)
        {
            //animator.SetBool ("Stop", true);
        }
    }
}
