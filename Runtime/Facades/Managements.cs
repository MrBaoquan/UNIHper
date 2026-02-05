using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UNIHper.Network;
using UNIHper.UI;
using System;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.SceneManagement;
using UniRx;

namespace UNIHper
{
    public static class Managements
    {
        public static readonly ConfigManager Config = ConfigManager.Instance;

        /// <summary>
        /// 统一 UI 管理器 - 自动检测并路由 UGUI 和 UI Toolkit
        /// </summary>
        public static readonly UnifiedUIFacade UI = UnifiedUIFacade.Instance;

        /// <summary>
        /// UGUI 管理器（直接访问）
        /// </summary>
        public static UIManager UGUI => UIManager.Instance;

        /// <summary>
        /// UI Toolkit 管理器（直接访问）
        /// </summary>
        public static UIToolkitManager UIToolkit => UIToolkitManager.Instance;

        public static readonly ResourceManager Resource = ResourceManager.Instance;
        public static readonly SceneManager Scene = SceneManager.Instance;
        public static readonly UNetManager Network = UNetManager.Instance;
        public static readonly Framework Framework = Framework.Instance;
        public static AudioManager Audio => AudioManager.Instance;
        public static readonly EventManager Event = EventManager.Instance;
        public static readonly TimerManager Timer = TimerManager.Instance;
        public static readonly DisposableManager Disposable = DisposableManager.Instance;

        public static T SceneScript<T>()
            where T : SceneScriptBase => SceneScriptManager.Instance.GetSceneScript<T>();
    }

    public static class SceneMgr
    {
        public static SceneManager Instance => SceneManager.Instance;
        public static Scene Current => Instance.Current;

        public static T SceneScript<T>()
            where T : SceneScriptBase => SceneScriptManager.Instance.GetSceneScript<T>();

        public static IObservable<Scene> OnNewSceneLoadedAsObservable() => Instance.OnNewSceneLoadedAsObservable();

        public static void LoadSceneAsync(string sceneName, System.Action<float> progress = null, System.Action completed = null) =>
            Instance.LoadSceneAsync(sceneName, progress, completed);
    }

    public static class UIMgr
    {
        /// <summary>
        /// UGUI 管理器实例（直接访问）
        /// </summary>
        public static UIManager Instance => UIManager.Instance;

        /// <summary>
        /// 统一 UI 门面（自动检测 UGUI / UI Toolkit）
        /// </summary>
        public static UnifiedUIFacade Unified => UnifiedUIFacade.Instance;

        /// <summary>
        /// 获取 UI 实例 - 自动检测类型
        /// </summary>
        public static T Get<T>(int instanceID = 0)
            where T : class, IUIComponent => Unified.Get<T>(instanceID);

        /// <summary>
        /// 显示 UI - 自动检测类型
        /// </summary>
        public static T Show<T>(bool bForceNotify = false)
            where T : class, IUIComponent => Unified.Show<T>(bForceNotify);

        /// <summary>
        /// 隐藏 UI - 自动检测类型
        /// </summary>
        public static T Hide<T>(bool bForceNotify = false)
            where T : class, IUIComponent => Unified.Hide<T>(bForceNotify);

        /// <summary>
        /// 检查 UI 是否正在显示 - 自动检测类型
        /// </summary>
        public static bool IsShowing<T>(int instanceID = 0)
            where T : class, IUIComponent => Unified.IsShowing<T>(instanceID);

        /// <summary>
        /// 切换 UI 显示/隐藏 - 自动检测类型
        /// </summary>
        public static T Toggle<T>()
            where T : class, IUIComponent => Unified.Toggle<T>();

        /// <summary>
        /// 隐藏所有 UI（UGUI 和 UI Toolkit）
        /// </summary>
        public static void HideAll() => Unified.HideAll();

        /// <summary>
        /// 设置渲染模式（仅 UGUI）
        /// </summary>
        public static void SetRenderMode(RenderMode renderMode, string canvasKey = UIManager.CANVAS_DEFAULT) =>
            Instance.SetRenderMode(renderMode, canvasKey);

        /// <summary>
        /// 暂存当前活动的 UI
        /// </summary>
        public static void StashActiveUI() => Instance.StashActiveUI();

        /// <summary>
        /// 恢复暂存的 UI
        /// </summary>
        public static void UnstashActiveUI() => Instance.PopStashedUI();
    }

    public static class ResMgr
    {
        public static ResourceManager Instance => ResourceManager.Instance;

        /// <summary>
        /// Get the resource of the specified asset name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static T Get<T>(string assetName)
            where T : UnityEngine.Object => Instance.Get<T>(assetName);

        /// <summary>
        /// Get all resources of the specified asset name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static List<T> GetMany<T>(string assetName)
            where T : UnityEngine.Object => Instance.GetMany<T>(assetName);

        /// <summary>
        /// Check if the resource of the specified asset name exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static bool Exists<T>(string assetName)
            where T : UnityEngine.Object => Instance.Exists<T>(assetName);

        /// <summary>
        /// Add a config file to the config manager.
        /// </summary>
        /// <param name="configPath"></param>
        public static void AddConfig(string configPath) => Instance.AddConfig(configPath);

        /// <summary>
        /// Append an asset bundle to the resource manager.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        public static AssetBundle AppendAssetBundle(string assetBundleName) => Instance.AppendAssetBundle(assetBundleName);

        public static List<T> GetLabelAssets<T>(string labelName)
            where T : UnityEngine.Object => Instance.GetLabelAssets<T>(labelName);

        public static IObservable<List<Texture2D>> AppendTexture2Ds(IEnumerable<string> texturePaths) =>
            Instance.AppendTexture2Ds(texturePaths);

        public static IObservable<List<AudioClip>> AppendAudioClips(IEnumerable<string> audioPaths) =>
            Instance.AppendAudioClips(audioPaths);

        public static IObservable<IEnumerable<AudioClip>> AppendAudioClips(
            string audioDir,
            string searchPattern = "*.wav|*.mp3",
            SearchOption searchOption = SearchOption.AllDirectories
        ) => Instance.AppendAudioClips(audioDir, searchPattern, searchOption);

        public static IObservable<AudioClip> AppendAudioClip(string audioPath) => Instance.AppendAudioClip(audioPath);

        public static IObservable<IEnumerable<Texture2D>> LoadTexture2Ds(IEnumerable<string> texturePaths) =>
            Instance.LoadTexture2Ds(texturePaths);

        public static IObservable<IList<Texture2D>> LoadTexture2Ds(
            string textureDir,
            string searchPattern = "*.png|*.jpg|*.jpeg",
            SearchOption searchOption = SearchOption.TopDirectoryOnly
        ) => Instance.LoadTexture2Ds(textureDir, searchPattern, searchOption);

        public static IObservable<Texture2D> AppendTexture2D(string texturePath) => Instance.AppendTexture2D(texturePath);
    }

    public static class CfgMgr
    {
        public static ConfigManager Instance => ConfigManager.Instance;

        public static T Get<T>()
            where T : UConfig => Instance.Get<T>();

        public static bool Serialize<T>()
            where T : UConfig => Instance.Save<T>();

        public static bool Save<T>()
            where T : UConfig => Instance.Save<T>();

        public static T Reload<T>()
            where T : UConfig => Instance.Reload<T>();

        public static void SerializeAll() => Instance.SaveAll();
    }

    public static class AudioMgr
    {
        public static AudioManager Instance => AudioManager.Instance;

        public static AudioSource PlayMusic(AudioClip InMusic, float InVolume = 1.0f, bool bLoop = true, int Index = 0) =>
            Instance.PlayMusic(InMusic, InVolume, bLoop, Index);

        public static AudioSource PlayMusic(string InMusic, float InVolume = 1.0f, bool bLoop = true, int Index = 0) =>
            Instance.PlayMusic(InMusic, InVolume, bLoop, Index);

        public static void PlayMusic(int Index = 0) => Instance.PlayMusic(Index);

        public static void PauseMusic(int Index = 0) => Instance.PauseMusic(Index);

        public static void StopMusic(int Index = 0) => Instance.StopMusic(Index);

        public static void PlayEffect(AudioClip effect, float InVolume = 1.0f, int Index = 0) =>
            Instance.PlayEffect(effect, InVolume, Index);

        public static void PlayEffect(string effectName, float volume = 1.0f, int index = 0) =>
            Instance.PlayEffect(effectName, volume, index);

        public static void StopEffect(int index = 0) => Instance.StopEffect(index);

        public static AudioPlayer MusicPlayer => Instance.MusicPlayer;
        public static AudioPlayer EffectPlayer => Instance.EffectPlayer;
    }

    public static class TimerMgr
    {
        public static TimerManager Instance => TimerManager.Instance;

        #region Delay - 延时操作

        /// <summary>
        /// 创建延时 Observable（Rx 风格）
        /// </summary>
        public static IObservable<long> Delay(float delayInSeconds) => Instance.Delay(delayInSeconds);

        /// <summary>
        /// 延时执行回调
        /// </summary>
        public static IDisposable Delay(float delayInSeconds, Action callback) => Instance.Delay(delayInSeconds, callback);

        /// <summary>
        /// 延时执行回调，可通过 key 取消（替换模式）
        /// </summary>
        public static IDisposable Delay(float delayInSeconds, Action callback, string key) => Instance.Delay(delayInSeconds, callback, key);

        /// <summary>
        /// 延时（异步等待）
        /// </summary>
        public static Task DelayAsync(float delayInSeconds) => Instance.DelayAsync(delayInSeconds);

        #endregion

        #region Interval - 间隔重复操作

        /// <summary>
        /// 创建间隔重复 Observable（Rx 风格）
        /// </summary>
        public static IObservable<long> Interval(float intervalInSeconds) => Instance.Interval(intervalInSeconds);

        /// <summary>
        /// 间隔重复执行回调
        /// </summary>
        public static IDisposable Interval(float intervalInSeconds, Action callback) => Instance.Interval(intervalInSeconds, callback);

        /// <summary>
        /// 间隔重复执行回调（带计数）
        /// </summary>
        public static IDisposable Interval(float intervalInSeconds, Action<long> callback) =>
            Instance.Interval(intervalInSeconds, callback);

        /// <summary>
        /// 间隔重复执行回调，可通过 key 取消
        /// </summary>
        public static IDisposable Interval(float intervalInSeconds, Action callback, string key) =>
            Instance.Interval(intervalInSeconds, callback, key);

        #endregion

        #region Timeout - 超时操作

        /// <summary>
        /// 创建超时 Observable，持续触发进度更新
        /// </summary>
        public static IObservable<float> Timeout(float durationInSeconds, float updateInterval = 0.05f) =>
            Instance.Timeout(durationInSeconds, updateInterval);

        /// <summary>
        /// 超时执行，带进度更新和完成回调
        /// </summary>
        public static IDisposable Timeout(
            float durationInSeconds,
            Action<float> onUpdate,
            Action onCompleted,
            float updateInterval = 0.05f
        ) => Instance.Timeout(durationInSeconds, onUpdate, onCompleted, updateInterval);

        #endregion

        #region Cancel - 取消操作

        /// <summary>
        /// 取消指定 key 的定时操作
        /// </summary>
        public static void Cancel(string key) => Instance.Cancel(key);

        /// <summary>
        /// 检查指定 key 的定时是否正在进行
        /// </summary>
        public static bool IsPending(string key) => Instance.IsPending(key);

        #endregion

        #region Countdown - 倒计时

        /// <summary>
        /// 创建倒计时 Observable
        /// </summary>
        public static IObservable<float> CountdownObservable(float durationInSeconds, float tickInterval = 1f) =>
            Instance.CountdownObservable(durationInSeconds, tickInterval);

        /// <summary>
        /// 创建 Countdown 对象
        /// </summary>
        public static Countdown Countdown(float durationInSeconds, float tickInterval = 1) =>
            Instance.Countdown(durationInSeconds, tickInterval);

        #endregion

        #region NextFrame - 下一帧操作

        /// <summary>
        /// 创建下一帧 Observable
        /// </summary>
        public static IObservable<Unit> NextFrameObservable() => Instance.NextFrameObservable();

        /// <summary>
        /// 下一帧执行回调
        /// </summary>
        public static IDisposable NextFrame(Action callback) => Instance.NextFrame(callback);

        /// <summary>
        /// 下一帧（异步等待）
        /// </summary>
        public static Task NextFrameAsync() => Instance.NextFrameAsync();

        #endregion

        #region Throttle & Debounce - 节流与防抖

        /// <summary>
        /// 创建节流函数
        /// </summary>
        public static Action Throttle(float intervalInSeconds, Action callback) => Instance.Throttle(intervalInSeconds, callback);

        /// <summary>
        /// 创建防抖函数
        /// </summary>
        public static Action Debounce(float delayInSeconds, Action callback) => Instance.Debounce(delayInSeconds, callback);

        #endregion
    }

    public static class EventMgr
    {
        public static EventManager Instance => EventManager.Instance;

        public static void Register<T>(Action<T> InDelegate)
            where T : UEvent => Instance.Register(InDelegate);

        public static void Unregister<T>(Action<T> InDelegate)
            where T : UEvent => Instance.Unregister(InDelegate);

        public static void Unregister<T>()
            where T : UEvent => Instance.Unregister<T>();

        public static void Fire(UEvent InEvent) => Instance.Fire(InEvent);
    }
}
