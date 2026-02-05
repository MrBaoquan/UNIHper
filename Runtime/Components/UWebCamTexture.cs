using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    using UniRx;

    /// <summary>
    /// [已弃用] 请使用 LiveCameraService 代替
    /// </summary>
    [Obsolete("UWebCamTexture 已弃用，请使用 LiveCameraService 代替。", false)]
    public class UWebCamTexture : MonoBehaviour
    {
        private UnityEvent<WebCamTexture> onCapture = new UnityEvent<WebCamTexture>();
        private UnityEvent<WebCamTexture> onReady = new UnityEvent<WebCamTexture>();

        private void Start() { }

        public UWebCamTexture StartCapture(int width = 640, int height = 480)
        {
            var _device = WebCamTexture.devices.FirstOrDefault();
            WebCamTexture _webcamTexture = new WebCamTexture(_device.name, width, height, 30);
            _webcamTexture.Play();
            onReady.Invoke(_webcamTexture);

            Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
                    if (_webcamTexture.didUpdateThisFrame)
                    {
                        onCapture.Invoke(_webcamTexture);
                    }
                })
                .AddTo(this);
            return this;
        }

        public IObservable<WebCamTexture> OnCaptureAsObserable()
        {
            return onCapture.AsObservable();
        }

        public IObservable<WebCamTexture> OnReadyAsObservable()
        {
            return onReady.AsObservable();
        }
    }
}
