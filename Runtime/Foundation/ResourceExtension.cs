using System.IO;
using System;
using System.Collections;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace UNIHper
{
    public static class ResourceExtension
    {
        public static IObservable<Texture2D> LoadTexture2D(
            this ResourceManager resourceManager,
            string filePath
        )
        {
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Application.streamingAssetsPath, filePath);
            }
            return Observable
                .FromCoroutine<Texture2D>(
                    (_observer, _cancellationToken) =>
                        LoadTexture2D(filePath, _observer, _cancellationToken)
                )
                .Catch<Texture2D, Exception>(
                    (_ex) =>
                    {
                        Debug.LogError($"LoadTexture2D Error:{_ex.Message}, filePath:{filePath}");
                        return Observable.Return<Texture2D>(null);
                    }
                );
        }

        public static IObservable<AudioClip> LoadAudioClip(
            this ResourceManager resourceManager,
            string filePath,
            AudioType InAudioType = AudioType.UNKNOWN
        )
        {
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Application.streamingAssetsPath, filePath);
            }
            return Observable
                .FromCoroutine<AudioClip>(
                    (_observer, _cancellationToken) =>
                        LoadAudioClip(filePath, _observer, _cancellationToken, InAudioType)
                )
                .Catch<AudioClip, Exception>(
                    (_ex) =>
                    {
                        Debug.LogError($"LoadAudioClip Error:{_ex.Message}, filePath:{filePath}");
                        return Observable.Return<AudioClip>(null);
                    }
                );
        }

        // 加载外部音频文件
        private static IEnumerator LoadAudioClip(
            string InPath,
            IObserver<AudioClip> observer,
            CancellationToken cancellationToken,
            AudioType InAudioType = AudioType.UNKNOWN
        )
        {
            if (InAudioType == AudioType.UNKNOWN)
            {
                var _fileExtension = System.IO.Path.GetExtension(InPath);
                if (_fileExtension == ".mp3")
                {
                    InAudioType = AudioType.MPEG;
                }
                else if (_fileExtension == ".wav")
                {
                    InAudioType = AudioType.WAV;
                }
            }

            using (
                UnityWebRequest _www = UnityWebRequestMultimedia.GetAudioClip(InPath, InAudioType)
            )
            {
                yield return _www.SendWebRequest();
#if UNITY_2021_1_OR_NEWER
                if (_www.result == UnityWebRequest.Result.ConnectionError)
                {
#else
                if (_www.isNetworkError)
                {
#endif

                    Debug.LogError(_www.error);
                    observer.OnError(new Exception(_www.error));
                }
                else
                {
                    try
                    {
                        var _audioClip = DownloadHandlerAudioClip.GetContent(_www);
                        _audioClip.name = System.IO.Path.GetFileNameWithoutExtension(InPath);
                        observer.OnNext(_audioClip);
                        observer.OnCompleted();
                    }
                    catch (System.Exception e)
                    {
                        observer.OnError(e);
                    }
                }
            }
        }

        // 加载外部图片资源
        private static IEnumerator LoadTexture2D(
            string InPath,
            IObserver<Texture2D> observer,
            CancellationToken cancellationToken
        )
        {
            using (UnityWebRequest _www = UnityWebRequestTexture.GetTexture(InPath))
            {
                yield return _www.SendWebRequest();
#if UNITY_2021_1_OR_NEWER
                if (_www.result == UnityWebRequest.Result.ConnectionError)
                {
#else
                if (_www.isNetworkError)
                {
#endif
                    Debug.LogError(_www.error);
                    observer.OnError(new Exception(_www.error));
                }
                else
                {
                    try
                    {
                        var _texture = DownloadHandlerTexture.GetContent(_www);
                        _texture.name = System.IO.Path.GetFileNameWithoutExtension(InPath);
                        observer.OnNext(_texture);
                        observer.OnCompleted();
                    }
                    catch (Exception _e)
                    {
                        observer.OnError(new Exception(_e.Message));
                    }
                }
            }
        }
    }
}
