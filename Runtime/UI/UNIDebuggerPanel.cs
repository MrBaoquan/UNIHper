using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using System.Collections.Generic;
using TMPro;

public class UNIDebuggerPanel : UIBase
{
    // Start is called before the first frame update
    private void Start()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        this.Get<Button>("open_folders/btn_persistentData")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                WinAPI.OpenProcess(
                    "explorer.exe",
                    Application.persistentDataPath.Replace("/", "\\") + "\\",
                    true
                );
            });

        this.Get<Button>("open_folders/btn_streamingAssets")
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

        this.Get<Toggle>("window_settings/switch_fullscreen")
            .onValueChanged.AsObservable()
            .Subscribe(_ =>
            {
                refreshMenuSettings();
            });

        this.Get<Button>("window_settings/btn_apply")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                var _posX = this.Get<TMP_InputField>("window_settings/input_x").text.Parse2Int();
                var _posY = this.Get<TMP_InputField>("window_settings/input_y").text.Parse2Int();

                var _width = this.Get<TMP_InputField>("window_settings/input_width")
                    .text.Parse2Int();
                var _height = this.Get<TMP_InputField>("window_settings/input_height")
                    .text.Parse2Int();
                var _fullScreen = this.Get<Toggle>("window_settings/switch_fullscreen").isOn;
                var _useTitleBar = this.Get<Toggle>("window_settings/switch_caption").isOn;
                var _keepTop = this.Get<Toggle>("window_settings/switch_keepTop").isOn;

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

        this.Get<TMP_InputField>("window_settings/input_x").text =
            _appConfig.PrimaryScreen.PosX.ToString();

        this.Get<TMP_InputField>("window_settings/input_y").text =
            _appConfig.PrimaryScreen.PosY.ToString();

        this.Get<TMP_InputField>("window_settings/input_width").text = _screenWidth.ToString();
        this.Get<TMP_InputField>("window_settings/input_height").text = _screenHeight.ToString();

        this.Get<Toggle>("window_settings/switch_fullscreen").isOn =
            _fullScreen == FullScreenMode.FullScreenWindow;

        this.Get<Toggle>("window_settings/switch_caption").isOn = _appConfig
            .PrimaryScreen
            .UseTitleBar;

        this.Get<Toggle>("window_settings/switch_keepTop").isOn = _appConfig.PrimaryScreen.KeepTop;
    }

    private void refreshMenuSettings()
    {
        var _fullScreen = this.Get<Toggle>("window_settings/switch_fullscreen").isOn;
        this.Get("window_settings/switch_caption").SetActive(!_fullScreen);
        this.Get("window_settings/text_caption").SetActive(!_fullScreen);
        this.Get("window_settings/switch_keepTop").SetActive(!_fullScreen);
        this.Get("window_settings/text_keepTop").SetActive(!_fullScreen);
    }

    // Update is called once per frame
    private void Update() { }

    // Called when this ui is loaded
    protected override void OnLoaded() { }

    // Called when this ui is showing
    protected override void OnShown() { }

    // Called when this ui is hidden
    protected override void OnHidden() { }
}
