using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * File: UHelperEntry.cs
 * File Created: 2019-10-11 08:50:03
 * Author: MrBaoquan (mrma617@gmail.com)
 * -----
 * Last Modified: 2019-10-25 10:06:12 am
 * Modified By: MrBaoquan (mrma617@gmail.com>)
 * -----
 * Copyright 2019 - 2019 mrma617@gmail.com
 */
 
namespace UHelper
{
public class UHelperEntry : SingletonBehaviour<UHelperEntry>
{
    private AppConfig appConfig;
    private void Awake() 
    {
        Debug.Log("UHelper.Awake");
        if(UHelperEntry.Instance!=this){
            GameObject.Destroy(this);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
        
        ULog.Initialize();
        
        GameObject _utilGO = new GameObject("UHelperUtils");
        _utilGO.transform.parent = this.transform;
        _utilGO.AddComponent(typeof(MonobehaviourUtil));

        AssemblyConfig.Refresh();

        // 1. 配置文件
        Managements.Config.Initialize();
        
        // 2. 初始化资源管理类
        ResourceManager.Instance.Initialize();

        // 3. 初始化 UI管理类
        UIManager.Instance.Initialize();

        // 4. 初始化场景管理类
        USceneManager.Instance.Initialize();

        // 5. 初始化Timer管理类
        UTimerManager.Instance.Initialize();

        // 6. 初始化网络模块
        UNetManager.Instance.Initialize();


        this.Initialize();
    }

    private void Initialize()
    {
        appConfig = Managements.Config.Get<AppConfig>();
        
        activeAllDisplays();

        if(appConfig.Screen.Mode==UFullScreenMode.MinimizedWindow){
            WinAPI.ShowWindow(WindowType.SW_SHOWMINIMIZED);
        }else{
            Screen.SetResolution(appConfig.Screen.Width,appConfig.Screen.Height,(FullScreenMode)appConfig.Screen.Mode);
        }
        Debug.LogFormat("set full screen mode:{0}, width:{1}, height:{2}",appConfig.Screen.Mode,appConfig.Screen.Width,appConfig.Screen.Height);

        KeepWindowTop();

    }

    private void activeAllDisplays(){
        var _config = Managements.Config.Get<AppConfig>();
        if(_config.Displays.Count<=0){
            for(int _index=0;_index<Display.displays.Length;++_index){
                _config.Displays.Add(new ScreenConfig());
            }
            Managements.Config.Serialize<AppConfig>();
        }

        if(Display.displays.Length<=1) return;
        
        for(int _index=0;_index<Display.displays.Length;++_index){
            if(_index>=_config.Displays.Count) break;
            var _screenConfig = _config.Displays[_index];
            Display.displays[_index].Activate(_screenConfig.Width,_screenConfig.Height,_screenConfig.RefreshRate);
            Debug.LogFormat("Display {0}",_index);
        }
    }

    private void Update() {
        if(Input.GetKey(KeyCode.LeftShift)&&Input.GetKeyDown(KeyCode.S)){
            Managements.Config.SerializeAll();
            Debug.Log("Save config successfully.");
        }
    }


    private void KeepWindowTop()
    {
        if(appConfig.KeepTopWindowInterval>0){
            AppUtility.KeepWindowTop(appConfig.KeepTopWindowInterval);
        }
    }
    private void OnDestroy() {
        Debug.Log("UHelper.OnDestroy");
    }


    private void OnApplicationQuit() {
        Debug.Log("application quit");
        ULog.Uninitialize();
    }
    
}  

};
