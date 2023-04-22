#pragma warning disable 0414

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DigitalRubyShared;
using PathologicalGames;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
using UniRx;
using Sirenix.OdinInspector;
using UnityEditor;

namespace UNIHper
{
    enum TapArea
    {
        EmptyArea,
        FullScreen,
    }

    public class TapEffect : SingletonBehaviour<TapEffect>
    {
        [
            SerializeField,
            Tooltip(
                "where the tap effect will be instantiated, if null, it will be instantiated in the first Canvas in the scene"
            )
        ]
        private Canvas _canvas;
        private Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = GameObject.FindObjectOfType<Canvas>();
                    if (_canvas == null)
                        _canvas = new GameObject("TapEffectCanvas").AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                return _canvas;
            }
        }

        [SerializeField, Tooltip("The area where the tap effect can be instantiated")]
        private TapArea tapArea = TapArea.FullScreen;

        [Title("Tap Effect Settings ")]
        [SerializeField]
        private bool customPrefab = false;

        [SerializeField, ShowIf("@this.customPrefab==false")]
        public AnimationClip effectClip;

        [SerializeField, ShowIf("@this.customPrefab==false")]
        public Vector2 effectSize = new Vector2(50, 50);

        [
            SerializeField,
            ShowIf("customPrefab"),
            Tooltip(
                "The prefab of the tap effect, it should have an Animator component and an Image component, and the Animator should have a state named 'Idle'"
            )
        ]
        private GameObject _effectPrefab;
        private GameObject effectPrefab
        {
            get
            {
                if (_effectPrefab == null)
                {
                    _effectPrefab = new GameObject("TapEffect");
                    var _animator = _effectPrefab.AddComponent<Animator>();
                    var _rectTransform = _effectPrefab.AddComponent<RectTransform>();
                    _rectTransform.sizeDelta = effectSize;

                    var _image = _effectPrefab.AddComponent<Image>();
                    _image.raycastTarget = false;

                    var _controller = _effectPrefab.AddComponent<UAnimatorOverrideController>();
                    _controller.runtimeAnimatorController =
                        Resources.Load<RuntimeAnimatorController>(
                            "Animations/Controllers/OneClip_Idle"
                        );

                    _controller.animationClipPairs =
                        _controller.runtimeAnimatorController.animationClips
                            .Select(
                                _animtionClip =>
                                    new AnimationClipPair
                                    {
                                        originalClip = _animtionClip,
                                        overrideClip =
                                            _animtionClip.name == "Idle"
                                                ? effectClip
                                                : _animtionClip
                                    }
                            )
                            .ToList();
                }
                return _effectPrefab;
            }
        }

        private void Reset()
        {
#if UNITY_EDITOR
            effectClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Packages/com.parful.unihper/Resources/Animations/TapEffect.anim"
            );
#endif
        }

        private void showEffect(Vector2 position)
        {
            var _spawnPool = PoolManager.Pools["TapEffect"];
            var _effect = _spawnPool.Spawn("TapEffect", canvas.transform);
            _effect.transform.position = position;
            _effect.transform.SetAsLastSibling();
            _effect.SetActive(true);
            _effect
                .Get<Animator>()
                .Play(
                    "Idle",
                    _ =>
                    {
                        if (_effect != null)
                            _spawnPool.Despawn(_effect.transform);
                    }
                );
        }

        void Awake()
        {
            var _rectTrans = effectPrefab.Get<RectTransform>();
            _rectTrans.anchorMin = Vector2.zero;
            _rectTrans.anchorMax = Vector2.zero;

            var _tapSpawnPool = PoolManager.Pools.Create("TapEffect");
            effectPrefab.transform.parent = _tapSpawnPool.transform;

            _tapSpawnPool.CreatePrefabPool(new PrefabPool(effectPrefab.transform));

            if (tapArea == TapArea.EmptyArea)
            {
                var _tapGesture = new TapGestureRecognizer();
                FingersScript.Instance.TreatMousePointerAsFinger = true;
                _tapGesture.AllowSimultaneousExecutionWithAllGestures();

                _tapGesture.StateUpdated += (gesture) =>
                {
                    if (gesture.State == GestureRecognizerState.Ended)
                        showEffect(new Vector2(gesture.FocusX, gesture.FocusY));
                };
                FingersScript.Instance.AddGesture(_tapGesture);
            }
            else if (tapArea == TapArea.FullScreen)
            {
                MultipleTouchManager.Instance
                    .OnFingerDownAsObservable()
                    .Subscribe(_ =>
                    {
                        showEffect(_.screenPosition);
                    })
                    .AddTo(this);
            }
        }
    }
}
