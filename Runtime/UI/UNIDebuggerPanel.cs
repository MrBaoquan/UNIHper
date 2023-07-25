using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using System.Collections.Generic;
using Michsky.MUIP;
using TMPro;

public class UNIDebuggerPanel : UIBase
{
    // Start is called before the first frame update
    private void Start()
    {
#if UNITY_STANDALONE_WIN||UNITY_EDITOR_WIN
        this.Get<ButtonManager>("open_folders/btn_persistentData")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                WinAPI.OpenProcess(
                    "explorer.exe",
                    Application.persistentDataPath.Replace("/", "\\") + "\\",
                    true
                );
            });

        this.Get<ButtonManager>("open_folders/btn_streamingAssets")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                WinAPI.OpenProcess(
                    "explorer.exe",
                    Application.streamingAssetsPath.Replace("/", "\\") + "\\",
                    true
                );
            });
#endif
        loadDefaultValues();
        refreshMenuSettings();

        this.Get<SwitchManager>("window_settings/switch_fullscreen")
            .onValueChanged.AsObservable()
            .Subscribe(_ =>
            {
                refreshMenuSettings();
            });

        this.Get<ButtonManager>("window_settings/btn_apply")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                var _posX = this.Get<CustomInputField>("window_settings/input_x")
                    .inputText.text.Parse2Int();
                var _posY = this.Get<CustomInputField>("window_settings/input_y")
                    .inputText.text.Parse2Int();

                var _width = this.Get<CustomInputField>("window_settings/input_width")
                    .inputText.text.Parse2Int();
                var _height = this.Get<CustomInputField>("window_settings/input_height")
                    .inputText.text.Parse2Int();
                var _fullScreen = this.Get<SwitchManager>("window_settings/switch_fullscreen").isOn;
                var _useTitleBar = this.Get<SwitchManager>("window_settings/switch_caption").isOn;
                var _keepTop = this.Get<SwitchManager>("window_settings/switch_keepTop").isOn;

                var _appConfig = Managements.Config.Get<AppConfig>();
                _appConfig.PrimaryScreen.PosX = _posX;
                _appConfig.PrimaryScreen.PosY = _posY;
                _appConfig.PrimaryScreen.Width = _width;
                _appConfig.PrimaryScreen.Height = _height;
                _appConfig.PrimaryScreen.Mode = _fullScreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
                _appConfig.PrimaryScreen.UseTitleBar = _useTitleBar;
                _appConfig.PrimaryScreen.KeepTop = _keepTop;
                _appConfig.Serialize();

                Managements.Config.Get<AppConfig>().ResetPrimaryScreen();
            });
    }

    private void loadDefaultValues()
    {
        var _appConfig = Managements.Config.Get<AppConfig>();
        var _screenWidth = _appConfig.PrimaryScreen.Width;
        var _screenHeight = _appConfig.PrimaryScreen.Height;
        var _fullScreen = _appConfig.PrimaryScreen.Mode;

        if (_appConfig.PrimaryScreen.Mode != FullScreenMode.FullScreenWindow)
        {
#if UNITY_STANDALONE_WIN&&!UNITY_EDITOR
            this.Get("window_settings/switch_caption").SetActive(true);
#endif
        }

        this.Get<CustomInputField>("window_settings/input_x").inputText.text =
            _appConfig.PrimaryScreen.PosX.ToString();

        this.Get<CustomInputField>("window_settings/input_y").inputText.text =
            _appConfig.PrimaryScreen.PosY.ToString();

        this.Get<CustomInputField>("window_settings/input_width").inputText.text =
            _screenWidth.ToString();
        this.Get<CustomInputField>("window_settings/input_height").inputText.text =
            _screenHeight.ToString();

        this.Get<SwitchManager>("window_settings/switch_fullscreen").isOn =
            _fullScreen == FullScreenMode.FullScreenWindow;
        this.Get<SwitchManager>("window_settings/switch_fullscreen").UpdateUI();

        this.Get<SwitchManager>("window_settings/switch_caption").isOn = _appConfig
            .PrimaryScreen
            .UseTitleBar;
        this.Get<SwitchManager>("window_settings/switch_caption").UpdateUI();

        this.Get<SwitchManager>("window_settings/switch_keepTop").isOn = _appConfig
            .PrimaryScreen
            .KeepTop;
        this.Get<SwitchManager>("window_settings/switch_keepTop").UpdateUI();
    }

    private void refreshMenuSettings()
    {
        var _fullScreen = this.Get<SwitchManager>("window_settings/switch_fullscreen").isOn;
        this.Get("window_settings/switch_caption").SetActive(!_fullScreen);
        this.Get("window_settings/text_caption").SetActive(!_fullScreen);
        this.Get("window_settings/switch_keepTop").SetActive(!_fullScreen);
        this.Get("window_settings/text_keepTop").SetActive(!_fullScreen);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            var PrimaryScreen = Managements.Config.Get<AppConfig>().PrimaryScreen;
            var _ret = WinAPI.SetWindowPos(
                WinAPI.CurrentWindow(),
                (int)HWndInsertAfter.HWND_TOPMOST,
                PrimaryScreen.PosX,
                PrimaryScreen.PosY,
                PrimaryScreen.Width,
                PrimaryScreen.Height,
                SetWindowPosFlags.SWP_SHOWWINDOW
            );
        }
    }

    // Called when this ui is loaded
    protected override void OnLoaded() { }

    // Called when this ui is showing
    protected override void OnShown() { }

    // Called when this ui is hidden
    protected override void OnHidden() { }
}
