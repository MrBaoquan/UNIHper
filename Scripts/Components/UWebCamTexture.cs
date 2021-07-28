using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper {

    public class UWebCamTexture : MonoBehaviour {
        private UnityEvent<WebCamTexture> onCapture = new UnityEvent<WebCamTexture> ();
        private UnityEvent<WebCamTexture> onReady = new UnityEvent<WebCamTexture> ();
        private void Start () {
            var _device = WebCamTexture.devices.FirstOrDefault ();
            WebCamTexture _webcamTexture = new WebCamTexture (_device.name, 640, 480, 30);
            _webcamTexture.Play ();
            onReady.Invoke (_webcamTexture);

            Observable.EveryUpdate ().Subscribe (_ => {
                if (_webcamTexture.didUpdateThisFrame) {
                    onCapture.Invoke (_webcamTexture);
                }
            }).AddTo (this);
        }

        public IObservable<WebCamTexture> OnCaptureAsObserable () {
            return onCapture.AsObservable ();
        }

        public IObservable<WebCamTexture> OnReadyAsObservable () {
            return onReady.AsObservable ();
        }
    }

}