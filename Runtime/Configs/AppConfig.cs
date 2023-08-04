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
        [XmlAttribute()]
        public bool Activate = false;

        [XmlAttribute()]
        public int PosX = 0;

        [XmlAttribute()]
        public int PosY = 0;

        [XmlAttribute()]
        public int Width = -1;

        [XmlAttribute()]
        public int Height = -1;

        [XmlAttribute()]
        public FullScreenMode Mode = FullScreenMode.FullScreenWindow;

        [DefaultValueAttribute(false)]
        [XmlAttribute]
        public bool UseTitleBar = false;

        [DefaultValueAttribute(false)]
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
    }

    public class URect
    {
        public URect() { }

        public URect(float x, float y, float w, float h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        // 摘要:
        //     X component of the vector.
        public float x;

        //
        // 摘要:
        //     Y component of the vector.
        public float y;

        //
        // 摘要:
        //     Z component of the vector.
        public float w;

        //
        // 摘要:
        //     W component of the vector.
        public float h;
    }

    public class SetWindowPos
    {
        public HWndInsertAfter HWndInsertAfter = HWndInsertAfter.HWND_TOPMOST;
        public URect SWP_Rect = new URect(0, 0, 1920, 1080);
        public List<SetWindowPosFlags> SWPFlags = new List<SetWindowPosFlags>() { };

        public void RefreshParameters()
        {
            if (SWPFlags.Count <= 0)
            {
                SWPFlags = new List<SetWindowPosFlags>() { SetWindowPosFlags.SWP_SHOWWINDOW };
            }
        }
    }

    // public class SetWindowLong
    // {
    //     public SetWindowLongIndex Index = SetWindowLongIndex.GWL_STYLE;
    //     public List<GWL_STYLE> GWL_Styles = new List<GWL_STYLE> { };
    //     public List<GWL_EXSTYLE> GWL_EXStyles = new List<GWL_EXSTYLE>();

    //     public void RefreshParameters()
    //     {
    //         if (GWL_Styles.Count <= 0)
    //         {
    //             GWL_Styles = new List<GWL_STYLE> { GWL_STYLE.WS_POPUP };
    //         }
    //         if (GWL_EXStyles.Count <= 0)
    //         {
    //             GWL_EXStyles = new List<GWL_EXSTYLE>();
    //         }
    //     }

    //     [YamlIgnore]
    //     [XmlIgnore]
    //     public long NewValue
    //     {
    //         get
    //         {
    //             if (Index == SetWindowLongIndex.GWL_STYLE)
    //             {
    //                 return (long)GWL_Styles.Aggregate((_flags, _current) => _flags | _current);
    //             }
    //             else if (Index == SetWindowLongIndex.GWL_EXSTYLE)
    //             {
    //                 return (long)GWL_EXStyles.Aggregate((_flags, _current) => _flags | _current);
    //             }
    //             return 0;
    //         }
    //     }
    // }

    // public class KeepWindowTop
    // {
    //     [XmlAttribute]
    //     public float Interval = 0f;

    //     [XmlAttribute]
    //     public bool SetWindowPos = false;

    //     [XmlAttribute]
    //     public bool SetWindowLong = false;
    //     public SetWindowPos SetWindowPosFunction = new SetWindowPos();
    //     public SetWindowLong SetWindowLongFunction = new SetWindowLong();

    //     public void RefreshParameters()
    //     {
    //         SetWindowLongFunction.RefreshParameters();
    //         SetWindowLongFunction.RefreshParameters();
    //     }
    // }

    public class AppConfig : UConfig
    {
        protected override string Comment()
        {
            return @"";
        }

        YamlDotNet.Core.Events.Comment _comment = new YamlDotNet.Core.Events.Comment(
            "AppConfig",
            true
        );

        //public KeepWindowTop KeepWindowTop = new KeepWindowTop();
        public ScreenConfig PrimaryScreen = new ScreenConfig() { KeepTop = true };

        [XmlArray("Displays")]
        [XmlArrayItem("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig>();

        protected override void OnLoaded()
        {
            PrimaryScreen.RefreshParameters();
            //KeepWindowTop.RefreshParameters();
            this.Serialize();
            executeWindowSettings();
            activeAllDisplays();
        }

        public async void ResetPrimaryScreen()
        {
            bool _fullScreen =
                (
                    PrimaryScreen.Mode == FullScreenMode.ExclusiveFullScreen
                    || PrimaryScreen.Mode == FullScreenMode.FullScreenWindow
                )
                    ? true
                    : false;
            Screen.SetResolution(PrimaryScreen.Width, PrimaryScreen.Height, _fullScreen);
            if (!_fullScreen)
            {
                // List<DisplayInfo> _displays = new List<DisplayInfo>();
                // Screen.GetDisplayLayout(_displays);
                // Screen.MoveMainWindowTo(
                //     _displays.First(),
                //     new Vector2Int(PrimaryScreen.PosX, PrimaryScreen.PosY)
                // );
                await Observable.Timer(TimeSpan.FromMilliseconds(150));
                var _currentWindow = WinAPI.CurrentWindow();
                var _longStyle = WinAPI.GetWindowLong(
                    _currentWindow,
                    (int)SetWindowLongIndex.GWL_STYLE
                );
                if (!PrimaryScreen.UseTitleBar)
                    _longStyle &= ~(int)GWL_STYLE.WS_CAPTION;
                else
                    _longStyle |= (int)GWL_STYLE.WS_CAPTION;
                WinAPI.SetWindowLong(
                    _currentWindow,
                    (int)SetWindowLongIndex.GWL_STYLE,
                    (uint)_longStyle
                );
            }

            await Observable.Timer(TimeSpan.FromMilliseconds(150));
            var _ret = WinAPI.SetWindowPos(
                WinAPI.CurrentWindow(),
                PrimaryScreen.KeepTop
                    ? (int)HWndInsertAfter.HWND_TOPMOST
                    : (int)HWndInsertAfter.HWND_NOTOPMOST,
                PrimaryScreen.PosX,
                PrimaryScreen.PosY,
                PrimaryScreen.Width,
                PrimaryScreen.Height,
                SetWindowPosFlags.SWP_SHOWWINDOW
            );
        }

        private void executeWindowSettings()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ResetPrimaryScreen();
            keepWindowTop ();
#endif
        }

        private void keepWindowTop()
        {
            // if (KeepWindowTop.Interval <= 0)
            //     return;

            // var _window = WinAPI.CurrentWindow();
            // Action _syncWindowSettings = () =>
            // {
            //     if (KeepWindowTop.SetWindowLong)
            //     {
            //         WinAPI.SetWindowLong(
            //             _window,
            //             (int)KeepWindowTop.SetWindowLongFunction.Index,
            //             (UInt32)KeepWindowTop.SetWindowLongFunction.NewValue
            //         );
            //     }

            //     if (KeepWindowTop.SetWindowPos)
            //     {
            //         WinAPI.SetWindowPos(
            //             _window,
            //             (int)KeepWindowTop.SetWindowPosFunction.HWndInsertAfter,
            //             (int)KeepWindowTop.SetWindowPosFunction.SWP_Rect.x,
            //             (int)KeepWindowTop.SetWindowPosFunction.SWP_Rect.y,
            //             (int)KeepWindowTop.SetWindowPosFunction.SWP_Rect.w,
            //             (int)KeepWindowTop.SetWindowPosFunction.SWP_Rect.h,
            //             KeepWindowTop.SetWindowPosFunction.SWPFlags.Aggregate(
            //                 (_flags, _current) => _flags | _current
            //             )
            //         );
            //     }
            // };

            // _syncWindowSettings();
            // Observable
            //     .Interval(TimeSpan.FromSeconds(KeepWindowTop.Interval))
            //     .Subscribe(_ =>
            //     {
            //         _syncWindowSettings();
            //     });
        }

        private void activeAllDisplays()
        {
            if (Displays.Count <= 0)
            {
                Displays = Display.displays.Select(_ => new ScreenConfig()).ToList();
                this.Serialize();
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
                    this.Serialize();
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
