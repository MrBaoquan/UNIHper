using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UNIHper.Network;
using UNIHper.UI;
using System;
using System.Threading.Tasks;
using System.IO;

namespace UNIHper
{
    public static class Managements
    {
        public static readonly ConfigManager Config = ConfigManager.Instance;
        public static readonly UIManager UI = UIManager.Instance;
        public static readonly ResourceManager Resource = ResourceManager.Instance;
        public static readonly USceneManager Scene = USceneManager.Instance;
        public static readonly UNetManager Network = UNetManager.Instance;
        public static readonly Framework Framework = Framework.Instance;
        public static UAudioManager Audio => UAudioManager.Instance;
        public static readonly UEventManager Event = UEventManager.Instance;
        public static readonly TimerManager Timer = TimerManager.Instance;

        public static T SceneScript<T>()
            where T : SceneScriptBase => SceneScriptManager.Instance.GetSceneScript<T>();
    }

    public static class UIMgr
    {
        public static UIManager Instance => UIManager.Instance;

        public static T Get<T>()
            where T : UIBase => Instance.Get<T>();

        public static T Show<T>(Action<T> InCallback = null)
            where T : UIBase => Instance.Show<T>(InCallback);

        public static T Hide<T>()
            where T : UIBase => Instance.Hide<T>();

        public static bool IsShowing<T>()
            where T : UIBase => Instance.IsShowing<T>();

        public static T Toggle<T>()
            where T : UIBase => Instance.Toggle<T>();

        public static void HideAll() => Instance.HideAll();

        public static void SetRenderMode(
            RenderMode renderMode,
            string canvasKey = UIManager.CANVAS_DEFAULT
        ) => Instance.SetRenderMode(renderMode, canvasKey);

        public static void StashActiveUI() => Instance.StashActiveUI();

        public static void UnstashActiveUI() => Instance.PopStashedUI();
    }

    public static class ResMgr
    {
        public static ResourceManager Instance => ResourceManager.Instance;

        public static T Get<T>(string assetName)
            where T : UnityEngine.Object => Instance.Get<T>(assetName);

        public static List<T> GetMany<T>(string assetName)
            where T : UnityEngine.Object => Instance.GetMany<T>(assetName);

        public static bool Exists<T>(string assetName)
            where T : UnityEngine.Object => Instance.Exists<T>(assetName);

        public static void AddConfig(string configPath) => Instance.AddConfig(configPath);

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
}
