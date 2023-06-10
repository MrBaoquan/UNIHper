using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;

namespace UNIHper
{
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

        public static bool ShowDebugLog
        {
            get => Self().ShowDebugMessage;
        }

        public static AudioClip DefaultClickSound
        {
            get => Self().defaultClickSound;
        }

        public static bool ShowTapEffect
        {
            get => Self().showTapEffect;
        }

        public static bool AutoInitIfNotStarted
        {
            get => Self().autoInitialize;
        }
        public bool autoInitialize = true;

        [Title("UNIHper Config File Paths")]
        public string ResourcePath = "UNIHper/resources";
        public string UIPath = "UNIHper/uis";
        public string AssemblyPath = "UNIHper/assemblies";

        [Title("UNIHper Interaction Settings")]
        public AudioClip defaultClickSound;
        public bool showTapEffect = true;

        [Title("UNIHper Other Settings")]
        public bool ShowDebugMessage = false;

#if UNITY_EDITOR
        public static void AddAssemblyToSettingsIfNotExists(string assemblyName)
        {
            var _textAsset = Resources.Load<TextAsset>(UNIHperSettings.AssemblyConfigPath);
            var _assemblies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(
                _textAsset.text
            );
            var _currentAssembly = assemblyName;
            if (!_assemblies.Contains(_currentAssembly))
            {
                _assemblies.Add(_currentAssembly);
                var _newAssemblyContent = Newtonsoft.Json.JsonConvert.SerializeObject(_assemblies);
                System.IO.File.WriteAllText(
                    UnityEditor.AssetDatabase.GetAssetPath(_textAsset),
                    _newAssemblyContent
                );
                UnityEditor.EditorUtility.SetDirty(_textAsset);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
    }
}
