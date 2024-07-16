using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using DNHper;
using UnityEngine;
using UnityEngine.Networking;

namespace UNIHper.Network
{
    using UniRx;

    public class HttpResponse
    {
        public UnityWebRequest WebRequest;

        /// <summary>
        /// 下载速度
        /// </summary>
        public int DownloadSpeed = 0;

        /// <summary>
        /// 实时下载进度
        /// </summary>
        public float DownloadProgress = 0.0f;

        public ulong FileSize = 0;
        public ulong DownloadedBytes = 0;
        public bool isDone = false;
    }

    public static class HttpRequest
    {
        /// <summary>
        /// HTTP Get request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IObservable<string> Get(string url)
        {
            // convert coroutine to IObservable
            return Observable
                .FromCoroutine<string>(
                    (observer, cancellationToken) => GetCore(url, observer, cancellationToken)
                )
                .Select(_response => _response);
        }

        static IEnumerator GetCore(
            string url,
            IObserver<string> observer,
            CancellationToken cancellationToken
        )
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = url.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        observer.OnError(new Exception(webRequest.error));
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        observer.OnError(new Exception(webRequest.error));
                        break;
                    case UnityWebRequest.Result.Success:
                        observer.OnNext(webRequest.downloadHandler.text);
                        observer.OnCompleted();
                        break;
                }
            }
        }

        /// <summary>
        /// HTTP Get request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IObservable<HttpResponse> Download(string url, string savePath)
        {
            return Observable.FromCoroutine<HttpResponse>(
                (observer, cancellationToken) =>
                    DownloadCore(url, savePath, true, observer, cancellationToken)
            );
        }

        ///
        /// 注意: 在安卓平台有内存限制，不能下载大文件      TODO: 大文件支持
        // Note: IObserver's basic scheme is "OnNext* (OnError | Oncompleted)?"
        static IEnumerator DownloadCore(
            string url,
            string savePath,
            bool updateSpeed,
            IObserver<HttpResponse> observer,
            CancellationToken cancellationToken
        )
        {
            UnityWebRequest webRequest = new UnityWebRequest(url);
            webRequest.method = UnityWebRequest.kHttpVerbGET;
            var _downloadHandler = new DownloadHandlerFile(savePath);
            _downloadHandler.removeFileOnAbort = true;
            webRequest.downloadHandler = _downloadHandler;

            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            }

            //yield return webRequest.SendWebRequest ();
            var _requestAction = webRequest.SendWebRequest();
            // 开始下载时间
            float _startTime = Time.time;
            // 上一次下载字节数
            ulong _lastDownloadedBytes = 0;

            float _lastSpeedTime = Time.time;
            float _lastSpeed = 0;

            IDisposable _updateHandler = null;
            if (updateSpeed)
            {
                _updateHandler = Observable
                    .Interval(TimeSpan.FromMilliseconds(100))
                    .Subscribe(_ =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _updateHandler.Dispose();
                            webRequest.Dispose();
                            webRequest = null;
                            return;
                        }

                        var _fileSize = webRequest.GetResponseHeader("Content-Length");
                        if (_fileSize == null)
                            return;

                        var _speedDelta = Time.time - _lastSpeedTime;
                        if (_speedDelta <= 0)
                            return;

                        float _speed =
                            _speedDelta >= 1.0
                                ? (
                                    (webRequest.downloadedBytes - _lastDownloadedBytes)
                                    / _speedDelta
                                )
                                : _lastSpeed;
                        if (_speed != _lastSpeed)
                        {
                            _lastDownloadedBytes = webRequest.downloadedBytes;
                            _lastSpeedTime = Time.time;
                            _lastSpeed = _speed;
                        }

                        observer.OnNext(
                            new HttpResponse
                            {
                                DownloadProgress = webRequest.downloadProgress,
                                WebRequest = webRequest,
                                DownloadSpeed = Mathf.CeilToInt(_speed),
                                FileSize = (ulong)_fileSize.Parse2Int(),
                                DownloadedBytes = webRequest.downloadedBytes,
                                isDone = webRequest.isDone
                            }
                        );
                    });
            }

            // Request and wait for the desired page.
            yield return _requestAction;

            if (updateSpeed && _updateHandler != null)
                _updateHandler.Dispose();

            if (webRequest != null)
            {
                string[] pages = url.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        observer.OnError(
                            new Exception(pages[page] + ": Error: " + webRequest.error)
                        );
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        observer.OnError(
                            new Exception(pages[page] + ": HTTP Error: " + webRequest.error)
                        );
                        break;
                    case UnityWebRequest.Result.Success:
                        var _fileSize = webRequest.GetResponseHeader("Content-Length");
                        observer.OnNext(
                            new HttpResponse
                            {
                                DownloadProgress = webRequest.downloadProgress,
                                WebRequest = webRequest,
                                DownloadSpeed = Mathf.CeilToInt(
                                    webRequest.downloadedBytes / (Time.time - _startTime)
                                ), // 下载平均速度
                                DownloadedBytes = webRequest.downloadedBytes,
                                FileSize = (ulong)_fileSize.Parse2Int(),
                                isDone = webRequest.isDone
                            }
                        );
                        observer.OnCompleted();
                        break;
                }
                webRequest.Dispose();
            }
        }

        public static IObservable<string> Post(string url, string postData)
        {
            // convert coroutine to IObservable
            return Observable
                .FromCoroutine<string>(
                    (observer, cancellationToken) =>
                        PostCore(url, postData, observer, cancellationToken)
                )
                .Select(_response => _response);
        }

        static IEnumerator PostCore(
            string url,
            string postData,
            IObserver<string> observer,
            CancellationToken cancellationToken
        )
        {
            using (
                UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            )
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(postData));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();

                string[] pages = url.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        observer.OnError(new Exception(webRequest.error));
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        observer.OnError(new Exception(webRequest.error));
                        break;
                    case UnityWebRequest.Result.Success:
                        observer.OnNext(webRequest.downloadHandler.text);
                        observer.OnCompleted();
                        break;
                }
            }
        }
    }
}
