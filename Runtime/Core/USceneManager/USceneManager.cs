using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UNIHper.UI;

namespace UNIHper
{
    public class SceneManager : Singleton<SceneManager>
    {
        private UnityEvent<Scene> m_onSceneLoaded = new UnityEvent<Scene>();

        internal void Awake()
        {
            SceneScriptManager.Instance.TriggerOnAwake(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        internal async Task Initialize()
        {
            UIManager.Instance.OnEnterScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            SceneScriptManager.Instance.TriggerOnStart(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Application.quitting += () =>
            {
                SceneScriptManager.Instance.TriggerOnDestroy(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            };
            await Task.CompletedTask;
        }

        public IObservable<Scene> OnNewSceneLoadedAsObservable()
        {
            return m_onSceneLoaded.AsObservable();
        }

        public void LoadSceneAsync(
            string sceneName,
            System.Action<float> progress = null,
            System.Action completed = null
        )
        {
            UNIHperEntry.Instance.StartCoroutine(IE_LoadScene(sceneName, progress, completed));
        }

        internal IEnumerator IE_LoadScene(
            string InSceneName,
            System.Action<float> InProgress,
            System.Action InCompleted
        )
        {
            string _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // 1. 触发场景脚本->销毁事件
            if (!isCurrentScene(InSceneName))
            {
                SceneScriptManager.Instance.TriggerOnDestroy(_currentSceneName);
            }
            // 2. 卸载旧场景动态资源,加载新场景动态资源
            ResourceManager.Instance.UnloadSceneResources(_currentSceneName);

            var _task = ResourceManager.Instance.LoadSceneResources(InSceneName);
            yield return new WaitUntil(() => _task.IsCompleted);

            // 3. Unity 开始加载场景
            AsyncOperation _async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(InSceneName);
            _async.allowSceneActivation = false;
            while (!_async.isDone)
            {
                InProgress?.Invoke(_async.progress);
                if (_async.progress >= 0.9f)
                {
                    if (!_async.allowSceneActivation)
                    {
                        _async.allowSceneActivation = true;
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            // 4. 通知加载场景完成事件
            InCompleted?.Invoke();

            // 5. 通知UIManager 进入新场景事件
            UIManager.Instance.OnEnterScene(InSceneName);

            // 6. 通知场景脚本 OnStart 事件
            SceneScriptManager.Instance.TriggerOnAwake(InSceneName);
            SceneScriptManager.Instance.TriggerOnStart(InSceneName);

            m_onSceneLoaded.Invoke(UnityEngine.SceneManagement.SceneManager.GetSceneByName(InSceneName));
            yield return null;
        }

        public Scene Current
        {
            get { return UnityEngine.SceneManagement.SceneManager.GetActiveScene(); }
        }

        private bool isCurrentScene(string sceneName)
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName;
        }
    }
}
