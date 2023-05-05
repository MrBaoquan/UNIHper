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
            get => Self().ResourcePath;
        }

        public static string UIConfigPath
        {
            get => Self().UIPath;
        }

        public static string AssemblyConfigPath
        {
            get => Self().AssemblyPath;
        }

        public static ConfigDriver ConfigDriver
        {
            get => Self().configDriver;
        }

        public static bool ShowDebugLog
        {
            get => Self().ShowDebugMessage;
        }

        public static AudioClip DefaultClickSound
        {
            get => Self().defaultClickSound;
        }

        [Title("UNIHper Config File Paths")]
        public string ResourcePath = "UNIHper/resources";
        public string UIPath = "UNIHper/uis";
        public string AssemblyPath = "UNIHper/assemblies";

        [Title("UNIHper Other Settings")]
        public bool ShowDebugMessage = false;
        public ConfigDriver configDriver = ConfigDriver.XML;
        public AudioClip defaultClickSound;
    }
}
