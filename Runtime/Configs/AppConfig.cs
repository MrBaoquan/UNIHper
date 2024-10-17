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

    public class AppConfig : UConfig
    {
        protected override string Comment()
        {
            return @"";
        }

        [XmlAttribute]
        public float LongTimeNoOperationTimeout = 300;

        public ScreenConfig PrimaryScreen = new ScreenConfig() { KeepTop = true };

        [XmlArray("Displays")]
        [XmlArrayItem("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig>();

        protected override void OnLoaded()
        {
            PrimaryScreen.RefreshParameters();

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            executeWindowSettings();
            activeAllDisplays();
#endif
        }

        public void ResetPrimaryScreen()
        {
            SetScreen(PrimaryScreen);
        }

        public async void SetScreen(ScreenConfig screenConfig)
        {
            bool _fullScreen =
                (
                    screenConfig.Mode == FullScreenMode.ExclusiveFullScreen
                    || screenConfig.Mode == FullScreenMode.FullScreenWindow
                )
                    ? true
                    : false;

            Screen.SetResolution(screenConfig.Width, screenConfig.Height, _fullScreen);

            await Observable.NextFrame();
            WinAPI.SetWindowPos(
                WinAPI.CurrentWindow(),
                screenConfig.KeepTop
                    ? (int)HWndInsertAfter.HWND_TOPMOST
                    : (int)HWndInsertAfter.HWND_NOTOPMOST,
                screenConfig.PosX,
                screenConfig.PosY,
                screenConfig.Width,
                screenConfig.Height,
                SetWindowPosFlags.SWP_SHOWWINDOW
            );

            await Observable.NextFrame();
            if (!_fullScreen)
            {
                var _currentWindow = WinAPI.CurrentWindow();
                var _longStyle = WinAPI.GetWindowLong(
                    _currentWindow,
                    (int)SetWindowLongIndex.GWL_STYLE
                );
                if (!screenConfig.UseTitleBar)
                    _longStyle &= ~(int)GWL_STYLE.WS_CAPTION;
                else
                    _longStyle |= (int)GWL_STYLE.WS_CAPTION;
                WinAPI.SetWindowLong(
                    _currentWindow,
                    (int)SetWindowLongIndex.GWL_STYLE,
                    (uint)_longStyle
                );
            }
        }

        private void executeWindowSettings()
        {
            ResetPrimaryScreen();
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
