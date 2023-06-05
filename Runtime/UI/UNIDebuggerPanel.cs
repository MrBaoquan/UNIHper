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

        this.Get<ButtonManager>("panel_resolution/btn_apply")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                var _width = this.Get<CustomInputField>("panel_resolution/input_width")
                    .inputText.text.Parse2Int();
                var _height = this.Get<CustomInputField>("panel_resolution/input_height")
                    .inputText.text.Parse2Int();
                var _fullScreen = this.Get<SwitchManager>(
                    "panel_resolution/switch_fullscreen"
                ).isOn;
                //Screen.SetResolution(_width, _height, _fullScreen);
                var _appConfig = Managements.Config.Get<AppConfig>();
                _appConfig.PrimaryScreen.Width = _width;
                _appConfig.PrimaryScreen.Height = _height;
                _appConfig.PrimaryScreen.Mode = _fullScreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
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

        this.Get<CustomInputField>("panel_resolution/input_width").inputText.text =
            _screenWidth.ToString();
        this.Get<CustomInputField>("panel_resolution/input_height").inputText.text =
            _screenHeight.ToString();
        this.Get<SwitchManager>("panel_resolution/switch_fullscreen").isOn =
            _fullScreen == FullScreenMode.FullScreenWindow;
        this.Get<SwitchManager>("panel_resolution/switch_fullscreen").UpdateUI();
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
