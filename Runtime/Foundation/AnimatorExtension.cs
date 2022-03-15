using System.Linq;
using System;
using UnityEngine;

using UniRx;

namespace UNIHper
{

public static class AnimatorExtension
{
    
    public static AnimationClip GetClip(this Animator animator, string InName){
        var _animClips = animator.runtimeAnimatorController.animationClips;
         return _animClips.Where(_=>{
            return _.name==InName;
         }).FirstOrDefault();
    }

    public static void PlayAnimation(this Animator animator, string InState, Action<Animator> InCallback=null){
        animator.Play(InState);
        syncPlayState(animator);

        if(InCallback is null) return;
        Observable.NextFrame().Subscribe(_=>{
            float _duration = animator.GetCurrentAnimatorStateInfo(0).length + 0.1f;

            Observable.Interval(TimeSpan.FromSeconds(_duration))
                .First()
                .Subscribe(_1=>{
                    if(InCallback!=null) InCallback(animator);
                });
        });
    }

    public static bool IsState(this Animator animator, string InState, int InLayer = 0){
        return animator.GetCurrentAnimatorStateInfo(InLayer).IsName(InState);
    }

    public static void StopAnimation(this Animator animator){
        syncStopState(animator);
    }

    // private methods
    private static void syncPlayState(Animator animator){
        animator.SetBool("Stop",false);
    }

    private static void syncStopState(Animator animator){
        animator.SetBool("Stop",true);
    }
}

}