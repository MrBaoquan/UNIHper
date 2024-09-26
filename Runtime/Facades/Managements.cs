using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UNIHper.Network;
using UNIHper.UI;
using System;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.SceneManagement;

namespace UNIHper
{
    public static class Managements
    {
        public static readonly ConfigManager Config = ConfigManager.Instance;
        public static readonly UIManager UI = UIManager.Instance;
        public static readonly ResourceManager Resource = ResourceManager.Instance;
        public static readonly SceneManager Scene = SceneManager.Instance;
        public static readonly UNetManager Network = UNetManager.Instance;
        public static readonly Framework Framework = Framework.Instance;
        public static AudioManager Audio => AudioManager.Instance;
        public static readonly EventManager Event = EventManager.Instance;
        public static readonly TimerManager Timer = TimerManager.Instance;

        public static T SceneScript<T>()
            where T : SceneScriptBase => SceneScriptManager.Instance.GetSceneScript<T>();
    }

    public static class SceneMgr
    {
        public static SceneManager Instance => SceneManager.Instance;
        public static Scene Current => Instance.Current;

        public static T SceneScript<T>()
            where T : SceneScriptBase => SceneScriptManager.Instance.GetSceneScript<T>();

        public static IObservable<Scene> OnNewSceneLoadedAsObservable() =>
            Instance.OnNewSceneLoadedAsObservable();

        public static void LoadSceneAsync(
            string sceneName,
            System.Action<float> progress = null,
            System.Action completed = null
        ) => Instance.LoadSceneAsync(sceneName, progress, completed);
    }

    public static class UIMgr
    {
        public static UIManager Instance => UIManager.Instance;

        /// <summary>
        /// Get the UI of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>()
            where T : UIBase => Instance.Get<T>();

        /// <summary>
        /// Show the UI of the specified type. If it is already showing, do nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="InCallback"></param>
        /// <returns></returns>
        public static T Show<T>(Action<T> InCallback = null)
            where T : UIBase => Instance.Show<T>(InCallback);

        /// <summary>
        /// Hide the UI of the specified type. If it is not showing, do nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Hide<T>()
            where T : UIBase => Instance.Hide<T>();

        /// <summary>
        /// Check if the UI of the specified type is showing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsShowing<T>()
            where T : UIBase => Instance.IsShowing<T>();

        /// <summary>
        /// Toggle the UI of the specified type. If it is showing, hide it. If it is hidden, show it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Toggle<T>()
            where T : UIBase => Instance.Toggle<T>();

        /// <summary>
        /// Hide all UI.
        /// </summary>
        public static void HideAll() => Instance.HideAll();

        public static void SetRenderMode(
            RenderMode renderMode,
            string canvasKey = UIManager.CANVAS_DEFAULT
        ) => Instance.SetRenderMode(renderMode, canvasKey);

        /// <summary>
        /// Stash all active UI.
        /// </summary>
        public static void StashActiveUI() => Instance.StashActiveUI();

        /// <summary>
        /// Pop all stashed UI.
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
        public static AssetBundle AppendAssetBundle(string assetBundleName) =>
            Instance.AppendAssetBundle(assetBundleName);

        public static List<T> GetLabelAssets<T>(string labelName)
            where T : UnityEngine.Object => Instance.GetLabelAssets<T>(labelName);

        public static Task<IEnumerable<Texture2D>> AppendTexture2Ds(
            IEnumerable<string> texturePaths
        ) => Instance.AppendTexture2Ds(texturePaths);

        public static Task<IEnumerable<AudioClip>> AppendAudioClips(
            IEnumerable<string> audioPaths
        ) => Instance.AppendAudioClips(audioPaths);

        public static Task<IEnumerable<AudioClip>> AppendAudioClips(
            string audioDir,
            string searchPattern = "*.wav|*.mp3",
            SearchOption searchOption = SearchOption.AllDirectories
        ) => Instance.AppendAudioClips(audioDir, searchPattern, searchOption);

        public static Task<AudioClip> AppendAudioClip(string audioPath) =>
            Instance.AppendAudioClip(audioPath);

        public static IObservable<IEnumerable<Texture2D>> LoadTexture2Ds(
            IEnumerable<string> texturePaths
        ) => Instance.LoadTexture2Ds(texturePaths);

        public static IObservable<IList<Texture2D>> LoadTexture2Ds(
            string textureDir,
            string searchPattern = "*.png|*.jpg|*.jpeg",
            SearchOption searchOption = SearchOption.TopDirectoryOnly
        ) => Instance.LoadTexture2Ds(textureDir, searchPattern, searchOption);

        public static Task<Texture2D> AppendTexture2D(string texturePath) =>
            Instance.AppendTexture2D(texturePath);
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

        public static void SerializeAll() => Instance.SerializeAll();
    }

    public static class AudioMgr
    {
        public static AudioManager Instance => AudioManager.Instance;

        public static AudioSource PlayMusic(
            AudioClip InMusic,
            float InVolume = 1.0f,
            bool bLoop = true,
            int Index = 0
        ) => Instance.PlayMusic(InMusic, InVolume, bLoop, Index);

        public static AudioSource PlayMusic(
            string InMusic,
            float InVolume = 1.0f,
            bool bLoop = true,
            int Index = 0
        ) => Instance.PlayMusic(InMusic, InVolume, bLoop, Index);

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

        public static IDisposable Delay(float delayInSeconds, Action callback) =>
            Instance.Delay(delayInSeconds, callback);

        public static Task Delay(float delayInSeconds) => Instance.Delay(delayInSeconds);

        public static Countdown Countdown(float durationInSecons, float tickInterval = 1) =>
            Instance.Countdown(durationInSecons, tickInterval);

        public static Task NextFrame() => Instance.NextFrame();

        public static IDisposable NextFrame(Action callback) => Instance.NextFrame(callback);
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
