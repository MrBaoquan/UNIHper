using System;
using System.Linq;
using DNHper;
using UniRx;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/*
 * File: UNIHperEntry.cs
 * File Created: 2019-10-11 08:50:03
 * Author: MrBaoquan (mrma617@gmail.com)
 * -----
 * Last Modified: 2019-10-25 10:06:12 am
 * Modified By: MrBaoquan (mrma617@gmail.com>)
 * -----
 * Copyright 2019 - 2019 mrma617@gmail.com
 */

namespace UNIHper {
    public class UNIHperEntry : SingletonBehaviour<UNIHperEntry> {
        private async void Awake () {
            if (UNIHperEntry.Instance != this) {
                GameObject.Destroy (this.gameObject);
                return;
            }
            Debug.Log ("UNIHper.Awake");
            DontDestroyOnLoad (this.gameObject);
            ULog.Initialize ();

            GameObject _utilGO = new GameObject ("UNIHperUtils");
            _utilGO.transform.parent = this.transform;
            _utilGO.AddComponent (typeof (MonobehaviourUtil));

            AssemblyConfig.Refresh ();

            // 1. 配置文件
            await Managements.Config.Initialize ();
            // 2. 初始化资源管理类
            await ResourceManager.Instance.Initialize ();
            // 3. 初始化 UI管理类
            await UIManager.Instance.Initialize ();
            // 4. 初始化场景管理类
            await USceneManager.Instance.Initialize ();
            // 5. 初始化Timer管理类
            await UTimerManager.Instance.Initialize ();
            // 6. 初始化网络模块
            await UNetManager.Instance.Initialize ();
            this.Initialize ();
        }

        private void Initialize () { }

        private void Update () {
            if (Keyboard.current == null) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.sKey.wasPressedThisFrame) {
                Managements.Config.SerializeAll ();
                Debug.Log ("Save config successfully.");
            }
            if (Keyboard.current.f12Key.wasPressedThisFrame) {
                Managements.UI.Get<UNIDebuggerPanel> ().Toggle ();
            }
#else
            if (Input.GetKey (KeyCode.LeftShift) && Input.GetKeyDown (KeyCode.S)) {
                Managements.Config.SerializeAll ();
                Debug.Log ("Save config successfully.");
            }
#endif

        }

        private void OnDestroy () {
            Debug.Log ("UNIHper.OnDestroy");
        }

        private void OnApplicationQuit () {
            Debug.Log ("application quit");
            ULog.Uninitialize ();
        }

    }

};