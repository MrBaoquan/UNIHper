using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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

        public static string ResourceConfigPath => Self().ResourcePath;

        public static string UIConfigPath => Self().UIPath;

        public static string AssemblyConfigPath => Self().AssemblyPath;

        public static bool ShowDebugLog => Self().ShowDebugMessage;

        public static AudioClip DefaultClickSound => Self().defaultClickSound;

        public static bool ShowTapEffect => Self().showTapEffect;

        public static bool ShowPanEffect => Self().showPanEffect;

        public static bool AutoInitIfNotStarted => Self().autoInitialize;

        public static List<string> InvalidAppNamePrefixes => Self().invalidAppNamePrefixes;

        public bool autoInitialize = true;

        [Title("Built-in Resources")]
        public string ResourcePath = "UNIHper/resources";
        public string UIPath = "UNIHper/uis";
        public string AssemblyPath = "UNIHper/assemblies";

        [Title("Interaction Settings")]
        public AudioClip defaultClickSound;
        public bool showTapEffect = false;
        public bool showPanEffect = false;

        [Title("Workflow Settings")]
        [Tooltip(
            "Generate default GameMain assembly or not, Please initialize UNIHper again if you change this value."
        )]
        public bool UseAssembly = false;

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
