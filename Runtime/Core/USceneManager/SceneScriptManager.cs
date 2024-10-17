using System;
using System.Collections;
using System.Collections.Generic;
using DNHper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UNIHper
{
    using UniRx;

    public class SceneScriptManager : Singleton<SceneScriptManager>
    {
        internal class SceneScriptData
        {
            public SceneScriptBase sceneScript;
            public IDisposable updateObserverable;

            public void OnApplicationQuit()
            {
                if (sceneScript is null)
                    return;

                var _quitMethod = sceneScript
                    .GetType()
                    .GetMethod(
                        "OnApplicationQuit",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                if (_quitMethod != null)
                {
                    _quitMethod.Invoke(sceneScript, null);
                }
            }
        }

        private Dictionary<string, SceneScriptData> sceneScripts =
            new Dictionary<string, SceneScriptData>();

        internal T GetSceneScript<T>()
            where T : SceneScriptBase
        {
            SceneScriptData _sceneScriptData;
            if (
                !sceneScripts.TryGetValue(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    out _sceneScriptData
                )
            )
            {
                return null;
            }
            return _sceneScriptData.sceneScript as T;
        }

        internal SceneScriptBase GetSceneScript(string sceneName)
        {
            SceneScriptData _sceneScriptData;
            if (!sceneScripts.TryGetValue(sceneName, out _sceneScriptData))
            {
                return null;
            }
            return _sceneScriptData.sceneScript;
        }

        internal SceneScriptData GetSceneData(string sceneName)
        {
            if (sceneScripts.ContainsKey(sceneName))
            {
                return sceneScripts[sceneName];
            }

            var _sceneScriptTypeName = sceneName + "Script";
            var _T = AssemblyConfig.GetUNIType(_sceneScriptTypeName);
            if (_T != null)
            {
                SceneScriptData _sceneScriptData = new SceneScriptData();
                var _sceneScript = Activator.CreateInstance(_T) as SceneScriptBase;
                _sceneScriptData.sceneScript = _sceneScript;
                sceneScripts.Add(sceneName, _sceneScriptData);
                return _sceneScriptData;
            }
            Debug.LogWarning("SceneScript for " + sceneName + " not found!");
            return null;
        }

        internal void TriggerOnAwake(string sceneName)
        {
            var _sceneScriptData = GetSceneData(sceneName);
            if (_sceneScriptData is null)
                return;

            var _sceneScript = _sceneScriptData.sceneScript;
            if (_sceneScript != null)
            {
                var _awakeAction = _sceneScript
                    .GetType()
                    .GetMethod(
                        "Awake",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );

                if (_awakeAction != null)
                {
                    _awakeAction.Invoke(_sceneScript, null);
                }
            }
        }

        internal void TriggerOnStart(string sceneName)
        {
            var _sceneScriptData = GetSceneData(sceneName);
            if (_sceneScriptData is null)
                return;

            var _sceneScript = _sceneScriptData.sceneScript;

            if (_sceneScript != null)
            {
                // 添加 SceneScript.OnQuit 事件监听
                Application.quitting += _sceneScriptData.OnApplicationQuit;

                var _startAction = _sceneScript
                    .GetType()
                    .GetMethod(
                        "Start",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                var _updateAction = _sceneScript
                    .GetType()
                    .GetMethod(
                        "Update",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                // 添加 SceneScript.OnUpdate 事件监听
                _sceneScriptData.updateObserverable = Observable
                    .EveryUpdate()
                    .Subscribe(_ =>
                    {
                        _updateAction?.Invoke(_sceneScript, null);
                    });
                _sceneScript.isSceneReady.Value = true;
                _startAction?.Invoke(_sceneScript, null);
            }
            else
            {
                Debug.LogWarningFormat("Can not find scene script: {0}", sceneName + "Script");
            }
        }

        internal void TriggerOnDestroy(string InSceneName)
        {
            SceneScriptData _sceneScriptData;
            if (sceneScripts.TryGetValue(InSceneName, out _sceneScriptData))
            {
                if (_sceneScriptData.updateObserverable != null)
                {
                    // 取消 SceneScript.OnUpdate 事件监听
                    _sceneScriptData.updateObserverable.Dispose();
                    _sceneScriptData.updateObserverable = null;
                }
                var _sceneScript = _sceneScriptData.sceneScript;
                var _destroyFunc = _sceneScript
                    .GetType()
                    .GetMethod(
                        "OnDestroy",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                if (_destroyFunc != null)
                    _destroyFunc.Invoke(_sceneScript, null);

                // 取消 Application.OnQuit 事件监听
                Application.quitting -= _sceneScriptData.OnApplicationQuit;
                _sceneScriptData.sceneScript = null;
                sceneScripts.Remove(InSceneName);
            }
        }
    }
}
