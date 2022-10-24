using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
public class UNIDebuggerPanel : UIBase {
    // Start is called before the first frame update
    private void Start () {
        this.Get<Button> ("options/btn_resetResolution")
            .OnClickAsObservable ()
            .Subscribe (_ => {
                Managements.Config.Get<AppConfig> ().Delete ();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                var _process = System.Diagnostics.Process.GetCurrentProcess ();
                var _executor = _process.MainModule.FileName;
                WinAPI.OpenProcess ("cmd.exe", $"/C taskkill /f /pid {_process.Id} && ping 127.0.0.1 -n 3 >nul && {_executor}", true);
#endif
            });

#if UNITY_STANDALONE_WIN||UNITY_EDITOR_WIN
        this.Get<Button> ("options/btn_persistentData")
            .OnClickAsObservable ()
            .Subscribe (_ => {
                WinAPI.OpenProcess ("explorer.exe", Application.persistentDataPath.Replace ("/", "\\") + "\\", true);
            });

        this.Get<Button> ("options/btn_streamingAssets")
            .OnClickAsObservable ()
            .Subscribe (_ => {
                WinAPI.OpenProcess ("explorer.exe", Application.streamingAssetsPath.Replace ("/", "\\") + "\\", true);
            });
#endif
    }

    // Update is called once per frame
    private void Update () {

    }

    // Called when this ui is loaded
    protected override void OnLoaded () {

    }

    // Called when this ui is showing
    protected override void OnShow () {

    }

    // Called when this ui is hidden
    protected override void OnHidden () {

    }
}