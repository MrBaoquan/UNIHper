using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using System.Collections.Generic;

public class UNIDebuggerPanel : UIBase
{
    // Start is called before the first frame update
    private void Start()
    {
        this.Get<Button>("options/btn_resetResolution")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                Managements.Config.Get<AppConfig>().Delete();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                var _process = System.Diagnostics.Process.GetCurrentProcess();
                var _executor = _process.MainModule.FileName;
                WinAPI.OpenProcess(
                    "cmd.exe",
                    $"/C taskkill /f /pid {_process.Id} && ping 127.0.0.1 -n 3 >nul && {_executor}",
                    true
                );
#endif
            });

#if UNITY_STANDALONE_WIN||UNITY_EDITOR_WIN
        this.Get<Button>("options/btn_persistentData")
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                WinAPI.OpenProcess(
                    "explorer.exe",
                    Application.persistentDataPath.Replace("/", "\\") + "\\",
                    true
                );
            });

        this.Get<Button>("options/btn_streamingAssets")
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

        // var _screenWidth = 1920;
        // var _screenHeight = 1080;
        // this.Get<UInput>("Input_width")
        //     .OnValueChangedAsObservable()
        //     .Subscribe(_ => _screenWidth = _.Parse2Int());
        // this.Get<UInput>("Input_height")
        //     .OnValueChangedAsObservable()
        //     .Subscribe(_ => _screenHeight = _.Parse2Int());

        // var _fullScreen = true;
        // this.Get<Toggle>("toggle_fullScreen")
        //     .OnValueChangedAsObservable()
        //     .Subscribe(_ =>
        //     {
        //         _fullScreen = _;
        //         Screen.SetResolution(_screenWidth, _screenHeight, _fullScreen);
        //     });

        // this.Get<Button>("btn_resolutionConfirm")
        //     .OnClickAsObservable()
        //     .Subscribe(_ =>
        //     {
        //         Screen.SetResolution(_screenWidth, _screenHeight, true);

        //         var _layouts = new List<DisplayInfo>();
        //         Screen.GetDisplayLayout(_layouts);
        //         _layouts.ForEach(_layout =>
        //         {
        //             Debug.Log(
        //                 _layout.width + "x" + _layout.height + " " + _layout.refreshRate + "Hz"
        //             );
        //             Debug.Log("workArea: " + _layout.workArea);

        //             _layout.workArea = new RectInt(100, 100, 1280, 720);
        //         });
        //     });
    }

    // Update is called once per frame
    private void Update() { }

    // Called when this ui is loaded
    protected override void OnLoaded() { }

    // Called when this ui is showing
    protected override void OnShowed() { }

    // Called when this ui is hidden
    protected override void OnHidden() { }
}
