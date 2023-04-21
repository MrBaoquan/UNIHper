using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;

namespace UNIHper
{
    public enum ConfigDriver
    {
        XML,
        YAML
    }

    public class UNIHperSettings : ScriptableObject
    {
        private static UNIHperSettings instance = null;

        private static UNIHperSettings Self()
        {
            if (instance == null)
            {
                instance =
                    Resources.Load<UNIHperSettings>("UNIHperSettings")
                    ?? ScriptableObject.CreateInstance<UNIHperSettings>();
            }

            return instance;
        }

        public static string ResourceConfigPath
        {
            get => Self().resPath;
        }

        public static string UIConfigPath
        {
            get => Self().uiPath;
        }

        public static string AssemblyConfigPath
        {
            get => Self().assemblyPath;
        }

        public static ConfigDriver ConfigDriver
        {
            get => Self().configDriver;
        }

        public static bool ShowDebugLog
        {
            get => Self().showDebugMessage;
        }

        public static AudioClip DefaultClickSound
        {
            get => Self().defaultClickSound;
        }

        [Title("UNIHper Config File Paths")]
        public string resPath = "UNIHper/resources";
        public string uiPath = "UNIHper/uis";
        public string assemblyPath = "UNIHper/assemblies";

        public bool showDebugMessage = false;
        public ConfigDriver configDriver = ConfigDriver.XML;

        public AudioClip defaultClickSound;
    }
}
