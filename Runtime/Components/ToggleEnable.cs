using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UNIHper
{
    public class ToggleEnable : MonoBehaviour
    {
        public bool Default = false;
#if ENABLE_INPUT_SYSTEM
        public Key EnableKey = Key.F10;
#else
        public KeyCode EnableKey = KeyCode.F10;
#endif

        // Start is called before the first frame update
        void Start()
        {
            gameObject.SetActive(Default);
            Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current[EnableKey].wasPressedThisFrame)
                    {
#else
                    if (Input.GetKeyDown(EnableKey))
                    {
#endif
                        gameObject.SetActive(!gameObject.activeInHierarchy);
                    }
                })
                .AddTo(this);
        }

        // Update is called once per frame
        void Update() { }
    }
}
