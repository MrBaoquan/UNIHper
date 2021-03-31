using System.IO;
using UnityEngine;
using System;


namespace UNIHper
{
    
public static class ULog
{
    const string configName = "NLog.config.xml";
    const string logFileName = "${shortdate}.log";
    
    static string LogFileDir{
        get{
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Logs");
        }
    }
    static string LogFilePath {
        get{
            return Path.Combine(LogFileDir, logFileName);
        }
    }

    public static void Debug(string InMessage){   
#if UNITY_EDITOR
        UnityEngine.Debug.Log(InMessage);
#endif
        NLogger.Debug(InMessage);
    }

    public static void Debug(object InMessage){
#if UNITY_EDITOR
        UnityEngine.Debug.Log(InMessage);
#endif
        NLogger.Debug(InMessage);
    }

    public static void Debug(string InFormat, params object[] InParams)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.LogFormat(InFormat, InParams);
#endif
        NLogger.Debug(InFormat, InParams);
    }

    public static void Warning(string InMessage){   
#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning(InMessage);
#endif
        NLogger.Warn(InMessage);
    }

    public static void Warning(string InFormat, params object[] InParams){   
#if UNITY_EDITOR
        UnityEngine.Debug.LogWarningFormat(InFormat, InParams);
#endif
        NLogger.Warn(InFormat, InParams);
    }

    public static void Error(string InMessage){   
#if UNITY_EDITOR
        UnityEngine.Debug.LogError(InMessage);
#endif
        NLogger.Error(InMessage);
    }

    public static void Error(string InFormat, params object[] InParams){   
#if UNITY_EDITOR
        UnityEngine.Debug.LogErrorFormat(InFormat, InParams);
#endif
        NLogger.Error(InFormat, InParams);
    }

    public static void Error(Exception InEx, string InMessage=""){   
#if UNITY_EDITOR
        UnityEngine.Debug.LogError(InMessage);
#endif
        NLogger.Error(InEx, InMessage);
    }

    

    public static void Initialize()
    {
        NLogger.LogFileDir = LogFileDir;
        NLogger.Initialize();
    }

    public static void Uninitialize()
    {
        NLogger.Uninitialize();
    }

}


}
