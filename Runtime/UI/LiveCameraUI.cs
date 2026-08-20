using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UNIHper;
using UNIHper.UI;
using DNHper;

/// <summary>
/// [已弃用] 旧版 UGUI 摄像头 UI，请使用 LiveCameraToolkitUI 替代
/// 保留此类仅用于兼容旧项目中的 RawImage 摄像头画面展示
/// </summary>
[UIPage(Asset = "LiveCameraUI", Type = UIType.Normal, Order = -1)]
public class LiveCameraUI : UIBase
{
    // Called when this ui is loaded
    protected override void OnLoaded() { }

    // Called when this ui is shown
    protected override void OnShown() { }

    // Called when this ui is hidden
    protected override void OnHidden() { }
}
