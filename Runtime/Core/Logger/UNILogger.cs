using System;
using System.IO;
using DNHper;
using UnityEngine;

namespace UNIHper
{
    public static class UNILogger
    {
        static string LogFileDir
        {
            get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs"); }
        }

        public static void Debug(string InMessage)
        {
            NLogger.Debug(InMessage);
        }

        public static void Debug(object InMessage)
        {
            NLogger.Debug(InMessage);
        }

        public static void Debug(string InFormat, params object[] InParams)
        {
            NLogger.Debug(InFormat, InParams);
        }

        public static void Warning(string InMessage)
        {
            NLogger.Warn(InMessage);
        }

        public static void Warning(string InFormat, params object[] InParams)
        {
            NLogger.Warn(InFormat, InParams);
        }

        public static void Error(string InMessage)
        {
            NLogger.Error(InMessage);
        }

        public static void Error(string InFormat, params object[] InParams)
        {
            NLogger.Error(InFormat, InParams);
        }

        public static void Error(Exception InEx, string InMessage = "")
        {
            NLogger.Error(InEx, InMessage);
        }

        public static void Initialize()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            NLogger.LogFileName = "Player.log";
            NLogger.LogFileDir = LogFileDir;
            NLogger.Initialize();
            Application.logMessageReceivedThreaded += HandleLog;
#endif
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    NLogger.Info(message);
                    break;
                case LogType.Error:
                    NLogger.Error(message);
                    NLogger.Error(stackTrace);
                    break;
                case LogType.Warning:
                    break;
            }
        }

        public static void CleanUp()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Application.logMessageReceivedThreaded -= HandleLog;
            NLogger.Shutdown();
#endif
        }
    }
}
