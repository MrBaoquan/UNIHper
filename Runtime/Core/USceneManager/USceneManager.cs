using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UNIHper {

    public class USceneManager : Singleton<USceneManager> {
        internal async Task Initialize () {
            UNIHperLogger.Log ("SceneManager Initializing ...");
            UIManager.Instance.OnEnterScene (SceneManager.GetActiveScene ().name);
            SceneScriptManager.Instance.TriggerOnStart (SceneManager.GetActiveScene ().name);
            await Task.CompletedTask;
        }

        public void LoadSceneAsync (string InSceneName, System.Action<float> InProgress, System.Action InCompleted) {
            MonobehaviourUtil.Instance.StartCoroutine (IE_LoadScene (InSceneName, InProgress, InCompleted));
        }

        public IEnumerator IE_LoadScene (string InSceneName, System.Action<float> InProgress, System.Action InCompleted) {
            string _currentSceneName = SceneManager.GetActiveScene ().name;
            // 1. 触发场景脚本->销毁事件
            if (!isCurrentScene (InSceneName)) {
                SceneScriptManager.Instance.TriggerOnDestroy (_currentSceneName);
            }
            // 2. 卸载旧场景动态资源,加载新场景动态资源
            ResourceManager.Instance.UnloadSceneResources (_currentSceneName);

            var _task = ResourceManager.Instance.LoadSceneResources (InSceneName);
            yield return new WaitUntil (() => _task.IsCompleted);

            // 3. Unity 开始加载场景
            AsyncOperation _async = SceneManager.LoadSceneAsync (InSceneName);
            _async.allowSceneActivation = false;
            while (!_async.isDone) {
                InProgress (_async.progress);
                if (_async.progress >= 0.9f) {
                    if (!_async.allowSceneActivation) {
                        _async.allowSceneActivation = true;
                    }

                }
                yield return new WaitForEndOfFrame ();
            }

            // 4. 通知加载场景完成事件
            InCompleted ();

            // 5. 通知UIManager 进入新场景事件
            UIManager.Instance.OnEnterScene (InSceneName);

            // 6. 通知场景脚本 OnStart 事件
            SceneScriptManager.Instance.TriggerOnStart (InSceneName);
            yield return null;
        }

        public Scene Current {
            get {
                return SceneManager.GetActiveScene ();
            }

        }

        private bool isCurrentScene (string InSceneName) {
            return SceneManager.GetActiveScene ().name == InSceneName;
        }
    }

}