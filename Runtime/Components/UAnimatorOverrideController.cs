using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace UNIHper {

    [System.Serializable]
    public class AnimationClipPair {
        [VerticalGroup ("original clip")]
        [HideLabel]
        [ReadOnly]
        [TableColumnWidth (30)]
        public AnimationClip originalClip;

        [HideLabel]
        [VerticalGroup ("override clip")]
        [TableColumnWidth (60)]
        public AnimationClip overrideClip;
    }

    [RequireComponent (typeof (Animator))]
    public class UAnimatorOverrideController : MonoBehaviour {
        public bool bAutoApply = true;
        Animator animator;
        public RuntimeAnimatorController runtimeAnimatorController = null;
        private RuntimeAnimatorController _lastRuntimeController = null;

        private AnimatorOverrideController overrideController = null;

        [TableList]
        public List<AnimationClipPair> animationClipPairs = new List<AnimationClipPair> ();

        private void Reset () {
            buildRefs ();
            if (runtimeAnimatorController is null) {
                return;
            };
            if (_lastRuntimeController == runtimeAnimatorController) return;

            _lastRuntimeController = runtimeAnimatorController;
            overrideController = new AnimatorOverrideController (runtimeAnimatorController);
            var _clips = new List<KeyValuePair<AnimationClip, AnimationClip>> ();
            overrideController.GetOverrides (_clips);
            animationClipPairs = _clips
                .Select (_clip => new AnimationClipPair { originalClip = _clip.Key, overrideClip = _clip.Value })
                .ToList ();
        }

        void buildRefs () {
            if (!animator) animator = GetComponent<Animator> ();
        }
        // Start is called before the first frame update
        void Awake () {
            if (bAutoApply) Apply ();
        }

        public void Apply () {
            buildRefs ();
            if (runtimeAnimatorController is null) return;

            overrideController = new AnimatorOverrideController (runtimeAnimatorController);
            var _clips = new List<KeyValuePair<AnimationClip, AnimationClip>> ();
            overrideController.GetOverrides (_clips);
            animationClipPairs = _clips.Select (_clip => new AnimationClipPair { originalClip = _clip.Key, overrideClip = _clip.Value }).ToList ();
            overrideController.ApplyOverrides (animationClipPairs.ToDictionary (_ => _.originalClip, _ => _.overrideClip).ToList ());
            animator.runtimeAnimatorController = overrideController;
        }
    }

}