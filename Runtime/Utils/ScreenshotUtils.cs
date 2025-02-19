using System;
using System.Collections;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    public class ScreenshotUtils
    {
        public static IObservable<Texture2D> TakeScreenshotAsObservable()
        {
            return TakeScreenshotAsObservable(new Rect(0, 0, Screen.width, Screen.height));
        }

        public static IObservable<Texture2D> TakeScreenshotAsObservable(Rect rect)
        {
            return Observable
                .EveryEndOfFrame()
                .First()
                .Select(_ =>
                {
                    var tex = new Texture2D(
                        (int)rect.width,
                        (int)rect.height,
                        TextureFormat.RGB24,
                        false
                    );
                    tex.ReadPixels(rect, 0, 0);
                    tex.Apply();
                    return tex;
                });
        }
    }
}
