using System;
using System.Linq;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UNIHper.Network;
using UNIHper.UI;

namespace UNIHper
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-50000)]
    public class UNIHperEntry : SingletonBehaviourDontDestroy<UNIHperEntry>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void initialize()
        {
            if (!UNIHperSettings.AutoInitIfNotStarted)
                return;

#if UNITY_2023_1_OR_NEWER
            var _entry = GameObject.FindFirstObjectByType<UNIHperEntry>();
#else
            var _entry = GameObject.FindObjectOfType<UNIHperEntry>();
#endif

            if (_entry is not null)
                return;

            var _unihperEntry = Resources.Load<GameObject>("UNIHper/Prefabs/UNIHper");

            if (_unihperEntry is null)
            {
                Debug.LogWarning("UNIHperEntry not found, Please click UNIHper/Initialize menu to create UNIHperEntry.");
                return;
            }
            var _unihperEntryGO = GameObject.Instantiate(_unihperEntry);
            _unihperEntryGO.name = "__UNIHper";
        }

        internal IObservable<Unit> OnInitializedAsObservable()
        {
            return isInitialized.Value ? Observable.Return(Unit.Default) : isInitialized.Where(_isInit => _isInit).AsUnitObservable();
        }

        ReactiveProperty<bool> isInitialized = new ReactiveProperty<bool>(false);

        private async void Awake()
        {
            if (isInitialized.Value)
                return;
            if (Instance != this)
            {
                DestroyImmediate(this.gameObject);
                return;
            }
            UNILogger.Initialize();
            SRDebug.Init();
            Debug.Log("UNIHper.Awake");
            DontDestroyOnLoad(this.gameObject);

            // 创建音频管理脚本
            GameObject _audioManager = new GameObject("AudioManager");
            _audioManager.transform.parent = this.transform;
            await _audioManager.AddComponent<AudioManager>().Initialize();

            AssemblyConfig.Refresh();
#if UNITY_2023_1_OR_NEWER
            var _eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
#else
            var _eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
#endif
            if (_eventSystem is null || !_eventSystem.gameObject.activeSelf || !_eventSystem.enabled)
            {
                CreateDefaultEventSystem();
            }

            SceneManager.Instance.Awake();

            // 基础组件初始化
            await ConfigManager.Instance.Initialize();
            await ResourceManager.Instance.Initialize();
            await UIManager.Instance.Initialize();

            this.Initialize();
            Framework.Instance.Initialize();

            await TimerManager.Instance.Initialize();
            await UNetManager.Instance.Initialize();
            await SceneManager.Instance.Initialize();

            isInitialized.Value = true;
        }

        private void CreateDefaultEventSystem()
        {
            var go = new GameObject("EventSystem (Created by UNIHper)");
            go.transform.SetParent(this.transform);
            go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            AddInputSystem(go);
#elif ENABLE_LEGACY_INPUT_MANAGER || (!ENABLE_INPUT_SYSTEM && !UNITY_2019_3_OR_NEWER)
            AddLegacyInputSystem(go);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void AddInputSystem(GameObject go)
        {
            var _inputModule = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            _inputModule.pointerBehavior = UnityEngine.InputSystem.UI.UIPointerBehavior.AllPointersAsIs;
            // Disable/re-enable to force some initialization.
            // fix for input not being recognized until component is toggled off then on
            go.SetActive(false);
            go.SetActive(true);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER || (!ENABLE_INPUT_SYSTEM && !UNITY_2019_3_OR_NEWER)
        private void AddLegacyInputSystem(GameObject go)
        {
            go.AddComponent<StandaloneInputModule>();
        }
#endif

        private void Initialize()
        {
            if (UNIHperSettings.ShowTapEffect)
            {
                TapEffect.Instance.Initialize();
            }
            if (UNIHperSettings.ShowPanEffect)
            {
                PanEffect.Instance.Initialize();
            }
        }

        private void OnApplicationQuit()
        {
            ConfigManager.Instance.CleanUp();
            UIManager.Instance.CleanUp();
            ResourceManager.Instance.CleanUp();
            Debug.Log("Application Quit");
            UNILogger.CleanUp();
        }
    }
};
