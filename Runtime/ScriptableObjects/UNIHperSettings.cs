using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
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
                instance = Resources.Load<UNIHperSettings>("UNIHperSettings") ?? ScriptableObject.CreateInstance<UNIHperSettings>();
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

        #region UI Toolkit Static Properties

        /// <summary>
        /// UI Toolkit 默认 PanelSettings
        /// </summary>
        public static PanelSettings UIToolkitPanelSettings => Self().uiToolkitPanelSettings;

        /// <summary>
        /// UI Toolkit 默认字体
        /// </summary>
        public static Font UIToolkitDefaultFont => Self().uiToolkitDefaultFont;

        /// <summary>
        /// UI Toolkit 默认样式表
        /// </summary>
        public static StyleSheet UIToolkitDefaultStyleSheet => Self().uiToolkitDefaultStyleSheet;

        /// <summary>
        /// 是否自动应用默认字体
        /// </summary>
        public static bool UIToolkitAutoApplyFont => Self().uiToolkitAutoApplyFont;

        #endregion

        public bool autoInitialize = true;

        [Title("Built-in Resources")]
        public string ResourcePath = "UNIHper/resources";
        public string UIPath = "UNIHper/uis";
        public string AssemblyPath = "UNIHper/assemblies";

        [Title("Interaction Settings")]
        public AudioClip defaultClickSound;
        public bool showTapEffect = false;
        public bool showPanEffect = false;

        [Title("UI Toolkit Settings")]
        [Tooltip("UI Toolkit 默认 PanelSettings，为空时使用内置默认值")]
        public PanelSettings uiToolkitPanelSettings;

        [Tooltip("UI Toolkit 默认字体，用于显示中文等非 ASCII 字符")]
        public Font uiToolkitDefaultFont;

        [Tooltip("UI Toolkit 默认样式表，包含字体等基础样式")]
        public StyleSheet uiToolkitDefaultStyleSheet;

        [Tooltip("是否自动为所有 UI Toolkit 页面应用默认字体")]
        public bool uiToolkitAutoApplyFont = true;

        [Title("Workflow Settings")]
        [Tooltip("Generate default GameMain assembly or not, Please initialize UNIHper again if you change this value.")]
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
            var _assemblies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(_textAsset.text);
            var _currentAssembly = assemblyName;
            if (!_assemblies.Contains(_currentAssembly))
            {
                _assemblies.Add(_currentAssembly);
                var _newAssemblyContent = Newtonsoft.Json.JsonConvert.SerializeObject(_assemblies);
                System.IO.File.WriteAllText(UnityEditor.AssetDatabase.GetAssetPath(_textAsset), _newAssemblyContent);
                UnityEditor.EditorUtility.SetDirty(_textAsset);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
    }
}
