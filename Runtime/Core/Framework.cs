using UnityEngine;
using DNHper;
using System;
using DigitalRubyShared;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UNIHper
{
    using UniRx;
    using UNIHper.UI;

    public class Framework : Singleton<Framework>
    {
        private Canvas _effectCanvas = null;
        public Canvas TopmostCanvas
        {
            get
            {
                if (_effectCanvas != null)
                {
                    return _effectCanvas;
                }
                _effectCanvas = new GameObject("Topmost Canvas").AddComponent<Canvas>();
                _effectCanvas.transform.SetParent(UNIHperEntry.Instance.transform);
                _effectCanvas.transform.SetSiblingIndex(1);

                _effectCanvas.gameObject.AddComponent<GraphicRaycaster>();
                _effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _effectCanvas.sortingOrder = 110;

                return _effectCanvas;
            }
        }

        internal void Initialize()
        {
            longTimeNoOperation.SetTimeout(
                Managements.Config.Get<AppConfig>().LongTimeNoOperationTimeout
            );
            enableConsolePanel();
        }

        ReactiveProperty<bool> debugModeEnabled = new ReactiveProperty<bool>(false);

        public IObservable<bool> OnToggleDebugAsObservable()
        {
            triggerImage.raycastTarget = true;
            return debugModeEnabled;
        }

        private Image triggerImage = null;

        private void enableConsolePanel()
        {
            Observable
                .EveryUpdate()
                .Subscribe(_ => queryShortcutEvents())
                .AddTo(UNIHperEntry.Instance);
            SRDebug.Instance.IsTriggerEnabled = false;
            debugModeEnabled.Subscribe(_enable =>
            {
                SRDebug.Instance.IsTriggerEnabled = _enable;
                if (_enable)
                {
                    Debug.LogError("Debug mode enabled.");
                }
            });

            var _triggerObject = new GameObject("__debugMode_trigger");
            _triggerObject.transform.SetParent(TopmostCanvas.transform);
            _triggerObject.transform.SetAsLastSibling();
            var _rectTrans = _triggerObject.AddComponent<RectTransform>();
            triggerImage = _triggerObject.AddComponent<Image>();
            triggerImage.color = Color.clear;
            triggerImage.raycastTarget = false;

            _rectTrans.localScale = Vector3.one;
            _rectTrans.sizeDelta = new Vector2(100, 100);
            _rectTrans.pivot = Vector2.one;
            _rectTrans.anchorMin = Vector2.one;
            _rectTrans.anchorMax = Vector2.one;
            _rectTrans.anchoredPosition = Vector3.zero;

            var _tapGesture = new TapGestureRecognizer();
            _tapGesture.NumberOfTapsRequired = 3;
            _tapGesture.PlatformSpecificView = _triggerObject;
            _tapGesture.StateUpdated += (
                _gesture =>
                {
                    if (_gesture.State == GestureRecognizerState.Ended)
                    {
                        debugModeEnabled.Value = !debugModeEnabled.Value;
                    }
                }
            );
            FingersScript.Instance.AddGesture(_tapGesture);
            FingersScript.Instance.ShowTouches = false;
        }

        private void queryShortcutEvents()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
                return;
            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.sKey.wasPressedThisFrame)
            {
                Managements.Config.SaveAll();
                Debug.Log("Save config successfully.");
            }
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Managements.UI.Get<UNIDebuggerPanel>().Toggle();
            }

            if (Keyboard.current.altKey.isPressed && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                SRDebug.Instance.DockConsole.IsVisible = !SRDebug.Instance.DockConsole.IsVisible;
                if (SRDebug.Instance.DockConsole.IsVisible)
                    SRDebug.Instance.HideDebugPanel();
            }
            else if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                if (SRDebug.Instance.IsDebugPanelVisible)
                    SRDebug.Instance.HideDebugPanel();
                else
                    SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Console);
            }
#else
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
            {
                Managements.Config.SaveAll();
                Debug.Log("Save config successfully.");
            }
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Managements.UI.Get<UNIDebuggerPanel>().Toggle();
            }

            if (
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                && Input.GetKeyDown(KeyCode.F10)
            )
            {
                SRDebug.Instance.DockConsole.IsVisible = !SRDebug.Instance.DockConsole.IsVisible;
                if (SRDebug.Instance.DockConsole.IsVisible)
                    SRDebug.Instance.HideDebugPanel();
            }
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                if (SRDebug.Instance.IsDebugPanelVisible)
                    SRDebug.Instance.HideDebugPanel();
                else
                    SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Console);
            }
#endif
        }

        private LongTimeNoOperation longTimeNoOperation = new LongTimeNoOperation(120);

        public IObservable<Unit> OnInitializedAsObservable()
        {
            return UNIHperEntry.Instance.OnInitializedAsObservable();
        }

        public LongTimeNoOperation LongTimeNoOperation => longTimeNoOperation;

        public LongTimeNoOperation SetLongTimeNoOperationTimeout(float timeout)
        {
            return longTimeNoOperation.SetTimeout(timeout);
        }

        public LongTimeNoOperation ResetLongTimeNoOperation()
        {
            return longTimeNoOperation.ResetOperation();
        }

        public IObservable<Unit> OnLongTimeNoOperationAsObservable()
        {
            return longTimeNoOperation.OnLongTimeNoOperationAsObservable();
        }

        public IObservable<Unit> OnResetLongTimeOperationAsObservable()
        {
            return longTimeNoOperation.OnResetOperationAsObservable();
        }
    }
}
