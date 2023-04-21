using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace UNIHper
{
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

        // csharpier-ignore
        public static void Play(this Animator animator, string InState, Action<Animator> InCallback = null)
        {
            if(animator.gameObject.activeInHierarchy == false)
                return;
            
            animator.Play(InState, 0, 0);
            syncPlayState(animator);
            
            Observable
                .NextFrame()
                .Subscribe(_ =>
                {
                    float _duration = animator.GetCurrentAnimatorStateInfo(0).length;
                    if(_duration <= 0){
                        InCallback?.Invoke(animator);
                        return;
                    }
                    Observable
                        .Timer(TimeSpan.FromSeconds(_duration))
                        .Subscribe(_1 =>
                        {
                            InCallback?.Invoke(animator);
                        });
                });
        }

        public static bool IsState(this Animator animator, string InState, int InLayer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(InLayer).IsName(InState);
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
