using System;
using System.Collections;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
namespace UNIHper {

    public class HttpResponse {
        public UnityWebRequest WebRequest;
        /// <summary>
        /// 下载速度
        /// </summary>
        public float DownloadSpeed = 0.0f;
        /// <summary>
        /// 实时下载进度
        /// </summary>
        public float DownloadProgress = 0.0f;

        public ulong DownloadedBytes = 0;
        public bool isDone = false;
    }
    public static class HttpRequest {
        /// <summary>
        /// HTTP Get request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IObservable<string> Get (string url) {
            // convert coroutine to IObservable
            return Observable.FromCoroutine<HttpResponse> ((observer, cancellationToken) => GetCore (url, false, observer, cancellationToken))
                .Select (_response => _response.WebRequest.downloadHandler.text);
        }

        /// <summary>
        /// HTTP Get request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IObservable<HttpResponse> Download (string url) {
            return Observable.FromCoroutine<HttpResponse> ((observer, cancellationToken) => GetCore (url, true, observer, cancellationToken));
        }

        // IObserver is a callback publisher
        // Note: IObserver's basic scheme is "OnNext* (OnError | Oncompleted)?" 
        static IEnumerator GetCore (string url, bool updateSpeed, IObserver<HttpResponse> observer, CancellationToken cancellationToken) {

            using (UnityWebRequest webRequest = UnityWebRequest.Get (url)) {
                //yield return webRequest.SendWebRequest ();
                var _requestAction = webRequest.SendWebRequest ();
                // 开始下载时间
                float _startTime = Time.time;
                // 上一次更新时间
                float _lastUpdateTime = Time.time;
                // 上一次下载字节数
                ulong _lastDownloadedBytes = 0;
                IDisposable _updateHandler = null;
                if (updateSpeed) {
                    _updateHandler = Observable.Interval (TimeSpan.FromMilliseconds (100)).Subscribe (_ => {
                        float _delta = Time.time - _lastUpdateTime;
                        if (_delta <= 0) return;
                        float _speed = ((webRequest.downloadedBytes - _lastDownloadedBytes) / _delta) / 1024.0f / 1024.0f;
                        _lastDownloadedBytes = webRequest.downloadedBytes;
                        _lastUpdateTime = Time.time;
                        observer.OnNext (new HttpResponse {
                            DownloadProgress = webRequest.downloadProgress,
                                WebRequest = webRequest,
                                DownloadSpeed = _speed,
                                DownloadedBytes = webRequest.downloadedBytes,
                                isDone = webRequest.isDone
                        });
                    });

                }

                // Request and wait for the desired page.
                yield return _requestAction;
                if (updateSpeed)
                    _updateHandler.Dispose ();

                string[] pages = url.Split ('/');
                int page = pages.Length - 1;

                switch (webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        observer.OnError (new Exception (pages[page] + ": Error: " + webRequest.error));
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        observer.OnError (new Exception (pages[page] + ": HTTP Error: " + webRequest.error));
                        break;
                    case UnityWebRequest.Result.Success:
                        observer.OnNext (new HttpResponse {
                            DownloadProgress = webRequest.downloadProgress,
                                WebRequest = webRequest,
                                DownloadSpeed = webRequest.downloadedBytes / (Time.time - _startTime) / 1024.0f / 1024.0f, // 下载平均速度
                                DownloadedBytes = webRequest.downloadedBytes,
                                isDone = webRequest.isDone
                        });
                        observer.OnCompleted ();
                        break;
                }
            }
        }
    }

}