using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent (typeof (Animator))]
public class UAnimatorOverrideController : MonoBehaviour {
    Animator animator;
    /**
     *    规则  必须 要和animationcontroller中要替换的动画名称匹配,且不能重名
     */
    public RuntimeAnimatorController runtimeAnimatorController = null;
    public List<AnimationClip> animationClips = new List<AnimationClip> ();

    private void OnValidate () {
        buildRefs ();
        if (runtimeAnimatorController is null) {
            Debug.LogWarning ("runtime animator controller is null");
            return;
        };
        if (animationClips.Count <= 0) {
            animationClips = runtimeAnimatorController.animationClips.ToList ();
            //animationClips = animator.runtimeAnimatorController.animationClips.ToList();   
        }
    }

    void buildRefs () {
        if (!animator) animator = GetComponent<Animator> ();
    }
    // Start is called before the first frame update
    void Awake () {
        buildRefs ();
        Apply ();
    }

    public void Apply () {
        buildRefs ();
        AnimatorOverrideController _animController = new AnimatorOverrideController (runtimeAnimatorController);
        var _animClips = new List<KeyValuePair<AnimationClip, AnimationClip>> ();
        for (int _index = 0; _index < _animController.animationClips.Length; ++_index) {
            _animClips.Add (new KeyValuePair<AnimationClip, AnimationClip> (_animController.animationClips[_index], animationClips[_index]));
        }

        _animController.ApplyOverrides (_animClips);
        animator.runtimeAnimatorController = _animController;
    }
}