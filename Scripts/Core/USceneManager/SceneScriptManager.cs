using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.SceneManagement;

namespace UNIHper
{
public class SceneScriptManager : Singleton<SceneScriptManager>
{
    public static void Register<T>(){
        Debug.Log(typeof(T).FullName);
    }

    public static void Register(string InType){
        Debug.Log(Type.GetType(InType).FullName);
    }

    public class SceneScriptData{
        public SceneScriptBase sceneScript;
        public IDisposable updateObserverable;
    }

    private Dictionary<string,SceneScriptData> sceneScripts = new Dictionary<string, SceneScriptData>();


    public T GetSceneScript<T>() where T : SceneScriptBase
    {
        SceneScriptData _sceneScriptData;
        if(!sceneScripts.TryGetValue(SceneManager.GetActiveScene().name, out _sceneScriptData)){
            return null;
        }
        return _sceneScriptData.sceneScript as T;
    }

    public void TriggerOnStart(string InSceneName)
    {
        SceneScriptData _sceneScriptData;
        SceneScriptBase _sceneScript = null;
        if(!sceneScripts.TryGetValue(InSceneName, out _sceneScriptData))
        {
            string _sceneScriptTypeName = InSceneName + "Script";
            Type _T = AssemblyConfig.GetUType(_sceneScriptTypeName);
            if(_T!=null)
            {
                // 重新初始化 SceneScript 实例
                _sceneScriptData = new SceneScriptData();
                _sceneScript = Activator.CreateInstance(_T) as SceneScriptBase;
                _sceneScriptData.sceneScript = _sceneScript;
                sceneScripts.Add(InSceneName,_sceneScriptData);
            }
        }

        if(_sceneScript!=null)
        {
            // 添加 SceneScript.OnQuit 事件监听
            Application.quitting += _sceneScriptData.sceneScript.OnApplicationQuit;
            // 添加 SceneScript.OnUpdate 事件监听
            _sceneScriptData.updateObserverable = Observable.EveryUpdate().Subscribe(_=>{
                _sceneScript.Update();
            });
            _sceneScript.Start();
        }else{
            Debug.LogWarningFormat("Can not find scene script: {0}",InSceneName+"Script");
        }
    }

    public void TriggerOnDestroy(string InSceneName)
    {
        SceneScriptData _sceneScriptData;
        if(sceneScripts.TryGetValue(InSceneName,out _sceneScriptData))
        {
            if(_sceneScriptData.updateObserverable!=null){
                // 取消 SceneScript.OnUpdate 事件监听
                _sceneScriptData.updateObserverable.Dispose();
                _sceneScriptData.updateObserverable = null;
            }
            

            _sceneScriptData.sceneScript.OnDestroy();
            
            // 取消 Application.OnQuit 事件监听
            Application.quitting -= _sceneScriptData.sceneScript.OnApplicationQuit;
            
            _sceneScriptData.sceneScript = null;
            sceneScripts.Remove(InSceneName);
        }
    }
}

}