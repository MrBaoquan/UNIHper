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

namespace UNIHper
{
    public enum TapArea
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
        public Canvas RootCanvas
        {
            get { return _canvas ? _canvas : Framework.Instance.TopmostCanvas; }
            set { _canvas = value; }
        }

        [SerializeField, Tooltip("The area where the tap effect can be instantiated")]
        public TapArea TapArea = TapArea.EmptyArea;

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
                    _effectPrefab = new GameObject("TapEffectItem");
                    var _animator = _effectPrefab.AddComponent<Animator>();
                    var _rectTransform = _effectPrefab.AddComponent<RectTransform>();
                    _rectTransform.sizeDelta = effectSize;

                    var _image = _effectPrefab.AddComponent<Image>();
                    _image.raycastTarget = false;

                    var _controller = _effectPrefab.AddComponent<UAnimatorOverrideController>();
                    _controller.runtimeAnimatorController =
                        Resources.Load<RuntimeAnimatorController>(
                            "__Animations/Controllers/OneClip_Idle"
                        );

                    effectClip =
                        effectClip == null
                            ? Resources.Load<AnimationClip>("__Animations/TapEffect")
                            : effectClip;

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
                    _effectPrefab.SetActive(false);
                }
                return _effectPrefab;
            }
        }

        internal void Initialize() { }

        private void Reset()
        {
#if UNITY_EDITOR
            effectClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Packages/com.parful.unihper/Assets/Resources/__Animations/TapEffect.anim"
            );
#endif
        }

        public void ShowEffect(Vector2 position)
        {
            var _spawnPool = PoolManager.Pools["TapEffectItem"];
            var _effect = _spawnPool.Spawn("TapEffectItem", RootCanvas.transform);
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
            transform.SetParent(RootCanvas.transform);
            var _rectTrans = effectPrefab.Get<RectTransform>();
            _rectTrans.anchorMin = Vector2.zero;
            _rectTrans.anchorMax = Vector2.zero;

            var _tapSpawnPool = PoolManager.Pools.Create("TapEffectItem", gameObject);
            effectPrefab.transform.SetParent(_tapSpawnPool.transform);

            _tapSpawnPool.CreatePrefabPool(new PrefabPool(effectPrefab.transform));

            if (TapArea == TapArea.EmptyArea)
            {
                var _tapGesture = new TapGestureRecognizer();

                FingersScript.Instance.TreatMousePointerAsFinger = true;
                _tapGesture.AllowSimultaneousExecutionWithAllGestures();

                _tapGesture.StateUpdated += (gesture) =>
                {
                    if (gesture.State == GestureRecognizerState.Ended)
                        ShowEffect(new Vector2(gesture.FocusX, gesture.FocusY));
                };
                FingersScript.Instance.AddGesture(_tapGesture);
                FingersScript.Instance.ShowTouches = false;
            }
            else if (TapArea == TapArea.FullScreen)
            {
#if ENABLE_INPUT_SYSTEM
                Observable
                    .EveryUpdate()
                    .Where(
                        _ => Pointer.current != null && Pointer.current.press.wasPressedThisFrame
                    )
                    .Subscribe(_ =>
                    {
                        ShowEffect(Pointer.current.position.ReadValue());
                    })
                    .AddTo(this);
#else
                Observable
                    .EveryUpdate()
                    .Where(_ => Input.GetMouseButtonDown(0))
                    .Subscribe(_ =>
                    {
                        ShowEffect(Input.mousePosition);
                    })
                    .AddTo(this);
#endif
            }
        }
    }
}
