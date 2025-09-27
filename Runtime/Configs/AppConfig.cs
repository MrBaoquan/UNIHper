using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using DNHper;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    public class ScreenConfig
    {
        [DefaultValueAttribute(false)]
        [XmlAttribute]
        public bool Activate = false;

        [XmlAttribute]
        public int PosX = 0;

        [XmlAttribute]
        public int PosY = 0;

        [XmlAttribute]
        public int Width = -1;

        [XmlAttribute]
        public int Height = -1;

        [XmlAttribute]
        public FullScreenMode Mode = FullScreenMode.FullScreenWindow;

        [XmlIgnore]
        public bool IsFullScreen
        {
            get => Mode == FullScreenMode.ExclusiveFullScreen || Mode == FullScreenMode.FullScreenWindow;
        }

        [XmlAttribute]
        public bool UseTitleBar = false;

        [XmlAttribute]
        public bool KeepTop = false;

        public void RefreshParameters()
        {
            if (Width == -1 || Height == -1)
            {
                Width = Screen.width;
                Height = Screen.height;
            }
        }

        public ScreenConfig ShallowCopy()
        {
            return (ScreenConfig)MemberwiseClone();
        }

        public override string ToString()
        {
            return $"PosX: {PosX}, PosY: {PosY}, Width: {Width}, Height: {Height}, Mode: {Mode}, UseTitleBar: {UseTitleBar}, KeepTop: {KeepTop}";
        }
    }

    public class MouseSettings
    {
        [XmlAttribute]
        public bool Enable = false;

        [XmlAttribute]
        public bool ShowCursor = true;

        [XmlAttribute]
        public bool ClickAfterMove = true;

        public SerializableVector2 Position = new SerializableVector2(-1, -1);

        [XmlAttribute]
        public float ResetInterval = 0;
    }

    public class AppConfig : UConfig
    {
        protected override string Comment()
        {
            return @"
            LongTimeNoOperationTimeout: 长时间无操作超时时间
            ResetPrimaryScreenInterval: 重新应用屏幕设置间隔时间
            MouseControl: 鼠标控制
                Enable: 启用鼠标控制
                ShowCursor: 是否显示鼠标
                Position: 要设置的鼠标位置
                ResetInterval: 鼠标位置重置间隔时间(s) 0 仅在程序启动时设置一次
                ClickAfterMove: 鼠标移动后是否模拟鼠标左键点击事件

            PrimaryScreen: 单屏幕时，程序的分辨率设置
            Displays: 屏幕设置 (启用多屏幕时的分辨率设置)
            ";
        }

        [XmlAttribute]
        public float LongTimeNoOperationTimeout = 300;

        [XmlAttribute]
        public float ResetPrimaryScreenInterval = 0;

        public MouseSettings MouseControl = new MouseSettings();

        public ScreenConfig PrimaryScreen = new ScreenConfig() { KeepTop = true };

        [XmlArray("Displays")]
        [XmlArrayItem("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig>();

        protected override void OnLoaded()
        {
            PrimaryScreen.RefreshParameters();

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN


            activeAllDisplays();
            executeWindowSettings();

            applyMouseSettings();

            if(ResetPrimaryScreenInterval > 0)
            {
                Observable.Interval(TimeSpan.FromSeconds(ResetPrimaryScreenInterval))
                    .Subscribe(_ => ResetPrimaryScreen()).AddTo(UNIHperEntry.Instance);
            }
#endif
        }

        public void ResetPrimaryScreen()
        {
            SetScreen(PrimaryScreen);
        }

        public async void SetScreen(ScreenConfig screenConfig)
        {
            Screen.SetResolution(screenConfig.Width, screenConfig.Height, screenConfig.IsFullScreen);

            if (!screenConfig.IsFullScreen)
            {
                await Observable.NextFrame();
                var _currentWindow = WinAPI.CurrentWindow();
                var _longStyle = WinAPI.GetWindowLong(_currentWindow, (int)SetWindowLongIndex.GWL_STYLE);
                if (!screenConfig.UseTitleBar)
                    _longStyle &= ~(int)GWL_STYLE.WS_CAPTION;
                else
                    _longStyle |= (int)GWL_STYLE.WS_CAPTION;
                WinAPI.SetWindowLong(_currentWindow, (int)SetWindowLongIndex.GWL_STYLE, (uint)_longStyle);
            }

            await Observable.NextFrame();
            WinAPI.SetWindowPos(
                WinAPI.CurrentWindow(),
                screenConfig.KeepTop ? (int)HWndInsertAfter.HWND_TOPMOST : (int)HWndInsertAfter.HWND_NOTOPMOST,
                screenConfig.PosX,
                screenConfig.PosY,
                screenConfig.Width,
                screenConfig.Height,
                SetWindowPosFlags.SWP_SHOWWINDOW
            );
        }

        private void executeWindowSettings()
        {
            ResetPrimaryScreen();
        }

        private void applyMouseSettings()
        {
            if (MouseControl.Enable)
            {
                WinAPI.SetCursorPos((int)MouseControl.Position.x, (int)MouseControl.Position.y);

                if (MouseControl.ResetInterval > 0)
                {
                    Observable
                        .Interval(TimeSpan.FromSeconds(MouseControl.ResetInterval))
                        .Subscribe(_ =>
                        {
                            WinAPI.SetCursorPos((int)MouseControl.Position.x, (int)MouseControl.Position.y);
                            WinAPI.ShowCursor(MouseControl.ShowCursor);
                            if (MouseControl.ClickAfterMove)
                            {
                                WinAPI.ClickLeftMouseButton();
                            }
                        })
                        .AddTo(UNIHperEntry.Instance);
                }
            }
        }

        private void activeAllDisplays()
        {
            if (Displays.Count <= 0)
            {
                Displays = Display.displays.Select(_ => new ScreenConfig()).ToList();
                this.Save();
            }

            int _index = 0;
            Displays.ForEach(_displayConfig =>
            {
                if (_index >= Display.displays.Length)
                    return;
                var _display = Display.displays[_index];
                if (_displayConfig.Width == -1 || _displayConfig.Height == -1)
                {
                    _displayConfig.Width = _display.systemWidth;
                    _displayConfig.Height = _display.systemHeight;
                    this.Save();
                }

                if (_displayConfig.Activate)
                {
                    Display.displays[_index].Activate();
                    Display.displays[_index].SetParams(
                        _displayConfig.Width,
                        _displayConfig.Height,
                        _displayConfig.PosX,
                        _displayConfig.PosY
                    );
                }
                _index++;
            });
        }
    }
}
