using System;
using System.Linq;
using DNHper;
using UniRx;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UNIHper
{
    [DisallowMultipleComponent]
    public class UNIHperEntry : SingletonBehaviour<UNIHperEntry>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void initialize()
        {
            var _entry = GameObject.FindObjectOfType<UNIHperEntry>();
            if (_entry is not null)
                return;

            var _unihperEntry = Resources.Load<GameObject>("UNIHper/Prefabs/UNIHper");

            if (_unihperEntry is null)
            {
                Debug.LogWarning(
                    "UNIHperEntry not found, Please click UNIHper/Initialize menu to create UNIHperEntry."
                );
                return;
            }
            var _unihperEntryGO = GameObject.Instantiate(_unihperEntry);
            _unihperEntryGO.name = "__UNIHper";
        }

        private async void Awake()
        {
            if (UNIHperEntry.Instance != this)
            {
                GameObject.Destroy(this.gameObject);
                return;
            }
            Debug.Log("UNIHper.Awake");
            DontDestroyOnLoad(this.gameObject);
            ULog.Initialize();

            GameObject _utilGO = new GameObject("UNIHperUtils");
            _utilGO.transform.parent = this.transform;
            _utilGO.AddComponent(typeof(MonobehaviourUtil));

            // 创建音频管理脚本
            GameObject _audioManager = new GameObject("AudioManager");
            _audioManager.transform.parent = this.transform;
            await _audioManager.AddComponent<UAudioManager>().Initialize();

            AssemblyConfig.Refresh();

            // 1. 配置文件
            await Managements.Config.Initialize();
            // 2. 初始化资源管理类
            await ResourceManager.Instance.Initialize();
            // 3. 初始化 UI管理类
            await UIManager.Instance.Initialize();
            // 4. 初始化场景管理类
            await USceneManager.Instance.Initialize();
            // 5. 初始化Timer管理类
            await UTimerManager.Instance.Initialize();
            // 6. 初始化网络模块
            await UNetManager.Instance.Initialize();
            this.Initialize();
        }

        private void Initialize()
        {
            if (UNIHperSettings.ShowTapEffect)
            {
                TapEffect.Instance.Initialize();
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
                return;
            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.sKey.wasPressedThisFrame)
            {
                Managements.Config.SerializeAll();
                Debug.Log("Save config successfully.");
            }
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Managements.UI.Get<UNIDebuggerPanel>().Toggle();
            }

            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                if (SRDebug.Instance.IsDebugPanelVisible)
                    SRDebug.Instance.HideDebugPanel();
                else
                    SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Console);
            }
#else
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
            {
                Managements.Config.SerializeAll();
                Debug.Log("Save config successfully.");
            }
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Managements.UI.Get<UNIDebuggerPanel>().Toggle();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (SRDebug.Instance.IsDebugPanelVisible)
                    SRDebug.Instance.HideDebugPanel();
                else
                    SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Console);
            }
#endif
        }

        private void OnDestroy()
        {
            Debug.Log("UNIHper.OnDestroy");
        }

        private void OnApplicationQuit()
        {
            ConfigManager.Instance.CleanUp();
            UIManager.Instance.CleanUp();
            ResourceManager.Instance.CleanUp();
            ULog.Shutdown();
            Debug.Log("Application Quit");
        }
    }
};
