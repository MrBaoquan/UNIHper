using System.Threading;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UniRx;
namespace UNIHper
{
    
public static class ResourceExtension{

    public static IObservable<Texture2D> LoadTexture2D(this ResourceManager resourceManager, string InPath){
        return Observable.FromCoroutine<Texture2D>((_observer, _cancellationToken)=>LoadTexture2D(InPath,_observer,_cancellationToken));
    }

    public static IObservable<AudioClip> LoadAudioClip(this ResourceManager resourceManager, string InPath,AudioType InAudioType){
        return Observable.FromCoroutine<AudioClip>((_observer,_cancellationToken)=>LoadAudioClip(InPath,InAudioType,_observer,_cancellationToken));
    }

    // 加载外部音频文件
    private static IEnumerator LoadAudioClip(string InPath,AudioType InAudioType,IObserver<AudioClip> observer, CancellationToken cancellationToken){
        using(UnityWebRequest _www = UnityWebRequestMultimedia.GetAudioClip(InPath,InAudioType)){
            yield return _www.SendWebRequest();
            if(_www.isNetworkError){
                Debug.LogError(_www.error);
                observer.OnError(new Exception(_www.error));
            }else{
                var _audioClip = DownloadHandlerAudioClip.GetContent(_www);
                observer.OnNext(_audioClip);
                observer.OnCompleted();
            }
        }
    }

    // 加载外部图片资源
    private static IEnumerator LoadTexture2D(string InPath, IObserver<Texture2D> observer, CancellationToken cancellationToken){
        using (UnityWebRequest _www = UnityWebRequestTexture.GetTexture(InPath))
        {
            yield return _www.SendWebRequest();
            if(_www.isNetworkError){
                Debug.LogWarning(_www.error);
                observer.OnError(new Exception(_www.error));
            }else{
                try{
                    var _texture = DownloadHandlerTexture.GetContent(_www);
                    observer.OnNext(_texture);
                    observer.OnCompleted();
                }catch(Exception _e){
                    observer.OnError(new Exception(_e.Message));
                }
                
            }
        }
    }




}


}
