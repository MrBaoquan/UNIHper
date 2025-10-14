using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using DNHper;
using UniRx;
using UnityEngine;
using System.IO;

namespace UNIHper
{
    public class ScreenConfig
    {
        [DefaultValue(false), XmlAttribute]
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
        public bool IsFullScreen => Mode == FullScreenMode.ExclusiveFullScreen || Mode == FullScreenMode.FullScreenWindow;

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

        public ScreenConfig ShallowCopy() => (ScreenConfig)MemberwiseClone();

        public override string ToString() =>
            $"PosX: {PosX}, PosY: {PosY}, Width: {Width}, Height: {Height}, Mode: {Mode}, UseTitleBar: {UseTitleBar}, KeepTop: {KeepTop}";
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
        protected override string Comment() =>
            @"LongTimeNoOperationTimeout: 长时间无操作超时时间
            CheckDesktopResolutionInterval: 多久检测一次桌面分辨率 0 不检测
            ResetPrimaryScreenInterval: 重新应用屏幕设置间隔时间
            MouseControl: 鼠标控制(Enable/ShowCursor/Position/ResetInterval/ClickAfterMove)
            PrimaryScreen: 单屏幕时，程序的分辨率设置
            Displays: 多屏幕分辨率设置";

        [XmlAttribute]
        public float LongTimeNoOperationTimeout = 300;

        [XmlAttribute]
        public float CheckDesktopResolutionInterval = 5;

        [XmlAttribute]
        public float ResetPrimaryScreenInterval = 0;

        public MouseSettings MouseControl = new MouseSettings();
        public ScreenConfig PrimaryScreen = new ScreenConfig() { KeepTop = true };

        [XmlElement("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig>();

        public bool ShouldSerializeDisplays() => Displays != null && Displays.Count > 0;

        private Subject<Vector2> OnDisplayResolutionChanged = new Subject<Vector2>();
        private CompositeDisposable _disposables = new CompositeDisposable();
        private IntPtr _cachedWindowHandle = IntPtr.Zero;

        public IObservable<Vector2> OnExtendedDesktopResolutionChangedAsObservable() => OnDisplayResolutionChanged;

        protected override void OnLoaded()
        {
            PrimaryScreen.RefreshParameters();
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            checkDisplayResolution();
            activeAllDisplays();
            executeWindowSettings();
            applyMouseSettings();
            createUtilShortcuts();

            if (ResetPrimaryScreenInterval > 0)
                Observable.Interval(TimeSpan.FromSeconds(ResetPrimaryScreenInterval))
                    .Subscribe(_ => ResetPrimaryScreen())
                    .AddTo(UNIHperEntry.Instance);
#endif
        }

        private void createUtilShortcuts()
        {
            string projectPath = PathUtils.GetProjectPath();
            string streamingAssetsPath = Application.streamingAssetsPath;
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // 创建 Configs 快捷方式（项目根目录 → StreamingAssets/Configs）
            CreateRelativeShortcut(
                shortcutName: "Configs",
                shortcutDir: projectPath,
                targetPath: PathUtils.GetStreamingAssetsPath("Configs"),
                description: "StreamingAssets/Configs 目录快速访问"
            );

            // 创建 Exe 快捷方式（StreamingAssets → Exe）
            CreateRelativeShortcut(
                shortcutName: Application.productName,
                shortcutDir: streamingAssetsPath,
                targetPath: exePath,
                workingDirectory: Path.GetDirectoryName(exePath),
                description: $"启动 {Application.productName}",
                iconPath: exePath
            );
        }

        /// <summary>
        /// 创建使用相对路径的快捷方式
        /// </summary>
        public void CreateRelativeShortcut(
            string shortcutName,
            string shortcutDir,
            string targetPath,
            string workingDirectory = "",
            string description = "",
            string iconPath = ""
        )
        {
            try
            {
                if (!PathUtils.PathExists(targetPath))
                {
                    Debug.LogWarning($"目标路径不存在: {targetPath}");
                    return;
                }

                string shortcutPath = Path.Combine(shortcutDir, $"{shortcutName}.lnk");

                // 检查现有快捷方式
                if (WinAPI.Shortcut.ShortcutExists(shortcutPath))
                {
                    string existingTarget = WinAPI.Shortcut.GetShortcutTarget(shortcutPath);
                    if (existingTarget?.Equals(targetPath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return;
                    }
                    WinAPI.Shortcut.DeleteShortcut(shortcutPath);
                }

                bool success = WinAPI.Shortcut.CreateShortcut(
                    shortcutPath: shortcutPath,
                    targetPath: targetPath,
                    workingDirectory: workingDirectory,
                    description: description,
                    iconPath: iconPath,
                    useRelativePath: true
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建 {shortcutName} 快捷方式失败: {ex.Message}");
            }
        }

        private void checkDisplayResolution()
        {
            var currentDesktopSize = WinAPI.GetExtendedDesktopSize();
            if (CheckDesktopResolutionInterval <= 0)
                return;

            Observable
                .Interval(TimeSpan.FromSeconds(CheckDesktopResolutionInterval))
                .Subscribe(_ =>
                {
                    var newDesktopSize = WinAPI.GetExtendedDesktopSize();
                    if (newDesktopSize != currentDesktopSize)
                    {
                        Debug.LogWarning(
                            $"Extended Desktop Resolution Changed: {newDesktopSize.TotalWidth} x {newDesktopSize.TotalHeight}"
                        );
                        WinAPI.GetAllMonitorsResolution().ForEach(m => Debug.Log($"Monitor: {m.Width} x {m.Height} @ ({m.Left}, {m.Top})"));
                        currentDesktopSize = newDesktopSize;
                        OnDisplayResolutionChanged.OnNext(new Vector2(newDesktopSize.TotalWidth, newDesktopSize.TotalHeight));
                        ResetPrimaryScreen();
                    }
                })
                .AddTo(_disposables);
        }

        public void ResetPrimaryScreen() => SetScreen(PrimaryScreen);

        private IntPtr GetCurrentWindow() =>
            _cachedWindowHandle == IntPtr.Zero ? (_cachedWindowHandle = WinAPI.CurrentWindow()) : _cachedWindowHandle;

        public async void SetScreen(ScreenConfig screenConfig)
        {
            try
            {
                Screen.SetResolution(screenConfig.Width, screenConfig.Height, screenConfig.IsFullScreen);
                await Observable.NextFrame();

                var window = GetCurrentWindow();
                if (window == IntPtr.Zero)
                {
                    Debug.LogError("无法获取窗口句柄");
                    return;
                }

                if (!screenConfig.IsFullScreen)
                {
                    var style = WinAPI.GetWindowLong(window, (int)SetWindowLongIndex.GWL_STYLE);
                    style = screenConfig.UseTitleBar ? style | (int)GWL_STYLE.WS_CAPTION : style & ~(int)GWL_STYLE.WS_CAPTION;
                    WinAPI.SetWindowLong(window, (int)SetWindowLongIndex.GWL_STYLE, (uint)style);
                }

                WinAPI.SetWindowPos(
                    window,
                    screenConfig.KeepTop ? (int)HWndInsertAfter.HWND_TOPMOST : (int)HWndInsertAfter.HWND_NOTOPMOST,
                    screenConfig.PosX,
                    screenConfig.PosY,
                    screenConfig.Width,
                    screenConfig.Height,
                    SetWindowPosFlags.SWP_SHOWWINDOW
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"设置屏幕失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void executeWindowSettings() => ResetPrimaryScreen();

        private void applyMouseSettings()
        {
            if (!MouseControl.Enable)
                return;

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
                            WinAPI.ClickLeftMouseButton();
                    })
                    .AddTo(_disposables);
            }
        }

        private void activeAllDisplays()
        {
            if (Displays.Count == 0)
            {
                Displays = Display.displays.Select(_ => new ScreenConfig()).ToList();
                this.Save();
            }

            for (int i = 0; i < Math.Min(Displays.Count, Display.displays.Length); i++)
            {
                var config = Displays[i];
                var display = Display.displays[i];

                if (config.Width == -1 || config.Height == -1)
                {
                    config.Width = display.systemWidth;
                    config.Height = display.systemHeight;
                }

                if (config.Activate)
                {
                    display.Activate();
                    display.SetParams(config.Width, config.Height, config.PosX, config.PosY);
                }
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _disposables?.Dispose();
            OnDisplayResolutionChanged?.Dispose();
        }
    }
}
