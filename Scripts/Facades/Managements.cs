using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNIHper
{

public static class Managements
{
    public static readonly ConfigManager Config = ConfigManager.Instance;
    public static readonly UIManager UI = UIManager.Instance;
    public static readonly ResourceManager Resource = ResourceManager.Instance;
    public static readonly USceneManager Scene = USceneManager.Instance;
    public static readonly UNetManager Network = UNetManager.Instance;
    public static readonly UAudioManager Audio = UAudioManager.Instance;
    public static readonly UEventManager Event = UEventManager.Instance;
    public static readonly UTimerManager Timer = UTimerManager.Instance;

    public static T SceneScript<T>() where T : SceneScriptBase
    {
        return SceneScriptManager.Instance.GetSceneScript<T>();
    }

}

}
