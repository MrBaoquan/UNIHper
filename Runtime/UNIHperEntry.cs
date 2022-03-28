using System;
using System.Linq;
using DNHper;
using UniRx;
using UnityEngine;

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

        private void Initialize () {
            var appConfig = Managements.Config.Get<AppConfig> ();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            bool _fullScreen = (
                    appConfig.PrimaryScreen.Mode == FullScreenMode.ExclusiveFullScreen ||
                    appConfig.PrimaryScreen.Mode == FullScreenMode.FullScreenWindow) ?
                true : false;
            Screen.SetResolution (appConfig.PrimaryScreen.Width, appConfig.PrimaryScreen.Height, _fullScreen);
            Debug.LogFormat ("set fullScreen:{0}, width:{1}, height:{2}",
                _fullScreen, appConfig.PrimaryScreen.Width, appConfig.PrimaryScreen.Height);

            activeAllDisplays ();
            KeepWindowTop ();
#endif

            Observable.EveryUpdate ().Subscribe (_ => {
                if (Input.GetKey (KeyCode.LeftShift) && Input.GetKeyDown (KeyCode.S)) {
                    Managements.Config.SerializeAll ();
                    Debug.Log ("Save config successfully.");
                }
            }).AddTo (this);
        }

        private void activeAllDisplays () {
            var _config = Managements.Config.Get<AppConfig> ();
            if (_config.Displays.Count <= 0) {
                _config.Displays.Clear ();
                _config.Displays = Display.displays.Select (_ => new ScreenConfig ()).ToList ();
                Managements.Config.Serialize<AppConfig> ();
            }

            int _index = 0;
            _config.Displays.ForEach (_display => {
                if (_index >= Display.displays.Length) return;
                var _displayConfig = _config.Displays[_index];

                if (_displayConfig.Activate) {
                    Display.displays[_index].Activate ();
                    Display.displays[_index].SetParams (_displayConfig.Width, _displayConfig.Height, _displayConfig.PosX, _displayConfig.PosY);
                }
                _index++;
            });
        }

        private void KeepWindowTop () {
            var _config = Managements.Config.Get<AppConfig> ();
            if (_config.KeepWindowTop.Interval <= 0) return;

            var _window = WinAPI.CurrentWindow ();
            Action _syncWindowSettings = () => {
                if (_config.KeepWindowTop.SetWindowLong) {
                    WinAPI.SetWindowLong (_window,
                        (int) _config.KeepWindowTop.SetWindowLongFunction.Index,
                        (UInt32) _config.KeepWindowTop.SetWindowLongFunction.NewValue
                    );
                }

                if (_config.KeepWindowTop.SetWindowPos) {
                    WinAPI.SetWindowPos (_window,
                        (int) _config.KeepWindowTop.SetWindowPosFunction.HWndInsertAfter,
                        (int) _config.KeepWindowTop.SetWindowPosFunction.SWP_Rect.x,
                        (int) _config.KeepWindowTop.SetWindowPosFunction.SWP_Rect.y,
                        (int) _config.KeepWindowTop.SetWindowPosFunction.SWP_Rect.w,
                        (int) _config.KeepWindowTop.SetWindowPosFunction.SWP_Rect.h,
                        _config.KeepWindowTop.SetWindowPosFunction.SWPFlags.Aggregate ((_flags, _current) => _flags | _current)
                    );
                }
            };

            _syncWindowSettings ();
            Observable.Interval (TimeSpan.FromSeconds (_config.KeepWindowTop.Interval))
                .Subscribe (_ => {
                    _syncWindowSettings ();
                });
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