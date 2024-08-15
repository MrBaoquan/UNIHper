using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;
using System.ComponentModel;

namespace UNIHper
{
    public class UNIHperSettings : ScriptableObject
    {
        private static UNIHperSettings instance = null;
        public static UNIHperSettings Instance => Self();

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

        public static bool ShowPanEffect
        {
            get => Self().showPanEffect;
        }

        public static bool AutoInitIfNotStarted
        {
            get => Self().autoInitialize;
        }

        public static List<string> InvalidAppNamePrefixes
        {
            get => Self().invalidAppNamePrefixes;
        }

        public bool autoInitialize = true;

        [Title("Built-in Resources")]
        public string ResourcePath = "UNIHper/resources";
        public string UIPath = "UNIHper/uis";
        public string AssemblyPath = "UNIHper/assemblies";

        [Title("Interaction Settings")]
        public AudioClip defaultClickSound;
        public bool showTapEffect = true;
        public bool showPanEffect = true;

        [Title("Workflow Settings")]
        [Tooltip(
            "Generate default GameMain assembly or not, Please initialize UNIHper again if you change this value."
        )]
        public bool UseAssembly = true;

        [Space]
        public List<string> invalidAppNamePrefixes = new List<string> { "unihper_template" };

        // 仓库排除文件路径
        public List<string> SVNExcludedPaths = new List<string>();

        [Title("Other Settings"), LabelText("Show Framework Log")]
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
