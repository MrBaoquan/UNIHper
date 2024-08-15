using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UNIHper.Network;
using UNIHper.UI;

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
}
