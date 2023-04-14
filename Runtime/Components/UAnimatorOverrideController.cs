using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace UNIHper
{
    [System.Serializable]
    public class AnimationClipPair
    {
        [VerticalGroup("original clip")]
        [HideLabel]
        [ReadOnly]
        [TableColumnWidth(30)]
        public AnimationClip originalClip;

        [HideLabel]
        [VerticalGroup("override clip")]
        [TableColumnWidth(60)]
        public AnimationClip overrideClip;
    }

    [RequireComponent(typeof(Animator)), DisallowMultipleComponent]
    public class UAnimatorOverrideController : MonoBehaviour
    {
        Animator animator;

        [ShowInInspector, SerializeField, OnValueChanged("OnChangedController")]
        public RuntimeAnimatorController runtimeAnimatorController = null;

        private AnimatorOverrideController overrideController = null;

        [SerializeField]
        [TableList(ShowIndexLabels = false, HideToolbar = false, IsReadOnly = true)]
        public List<AnimationClipPair> animationClipPairs = new List<AnimationClipPair>();

        private void OnChangedController()
        {
            buildRefs();
            if (runtimeAnimatorController is null)
            {
                return;
            }

            overrideController = new AnimatorOverrideController(runtimeAnimatorController);
            var _clips = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(_clips);
            animationClipPairs = _clips
                .Select(
                    _clip =>
                        new AnimationClipPair
                        {
                            originalClip = _clip.Key,
                            overrideClip = _clip.Value
                        }
                )
                .ToList();
        }

        void buildRefs()
        {
            if (!animator)
                animator = GetComponent<Animator>();
        }

        // Start is called before the first frame update
        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            buildRefs();
            if (runtimeAnimatorController is null)
                return;
            overrideController = new AnimatorOverrideController(runtimeAnimatorController);
            overrideController.ApplyOverrides(
                animationClipPairs.ToDictionary(_ => _.originalClip, _ => _.overrideClip).ToList()
            );
            animator.runtimeAnimatorController = overrideController;
        }
    }
}
