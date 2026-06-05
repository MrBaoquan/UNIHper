using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

namespace UNIHper.Editor
{
    /// <summary>
    /// UI Toolkit 编辑器工具
    /// </summary>
    public static class UIToolkitEditorUtility
    {
        [MenuItem("UNIHper/UI Toolkit/Create Default Panel Settings", priority = 100)]
        public static void CreateDefaultPanelSettings()
        {
            // 确保 Resources 目录存在
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string panelSettingsPath = "Assets/Resources/DefaultPanelSettings.asset";

            // 检查是否已存在
            if (File.Exists(panelSettingsPath))
            {
                if (!EditorUtility.DisplayDialog("PanelSettings 已存在", "DefaultPanelSettings 资源已存在，是否覆盖？", "覆盖", "取消"))
                {
                    return;
                }
            }

            // 创建 PanelSettings
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;

            // 设置默认主题
            var defaultTheme = Resources.Load<ThemeStyleSheet>("UIPackageResources/UnityThemes/UnityDefaultRuntimeTheme");
            if (defaultTheme != null)
            {
                panelSettings.themeStyleSheet = defaultTheme;
                Debug.Log("[UIToolkit] 已设置默认主题: UnityDefaultRuntimeTheme");
            }
            else
            {
                Debug.LogWarning("[UIToolkit] 未找到默认主题，UI 可能显示异常");
            }

            // 保存资源
            AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 选中资源
            EditorGUIUtility.PingObject(panelSettings);
            Selection.activeObject = panelSettings;

            Debug.Log($"[UIToolkit] 已创建 DefaultPanelSettings: {panelSettingsPath}");
            EditorUtility.DisplayDialog("创建成功", $"DefaultPanelSettings 已创建于：\n{panelSettingsPath}", "确定");
        }

        [MenuItem("UNIHper/UI Toolkit/Open UI Builder", priority = 101)]
        public static void OpenUIBuilder()
        {
            EditorApplication.ExecuteMenuItem("Window/UI Toolkit/UI Builder");
        }

        [MenuItem("UNIHper/UI Toolkit/Documentation", priority = 102)]
        public static void OpenDocumentation()
        {
            var docPath = "Packages/com.parful.unihper/Documentation~/UIToolkit.md";
            if (File.Exists(docPath))
            {
                Application.OpenURL("file:///" + Path.GetFullPath(docPath));
            }
            else
            {
                Debug.LogWarning("[UIToolkit] 文档文件不存在");
            }
        }
    }
}
