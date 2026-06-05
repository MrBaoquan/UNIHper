using System.IO;
using UnityEditor;
using UnityEngine;

namespace UNIHper.Editor
{
    /// <summary>
    /// 预制体结构导出器配置
    /// 集成到 Project Settings/UNIHper/AI Copilot 面板
    /// </summary>
    public class PrefabExporterSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Editor/PrefabExporterSettings.asset";
        private const string DefaultSourceFolderPath = "Assets/ArtAssets/UI Prefabs";
        private const string DefaultOutputFolderPath = ".ai-context/prefabs";

        #region 配置字段

        [Header("自动导出设置")]
        [Tooltip("是否启用自动导出（预制体保存时自动更新）")]
        [SerializeField]
        private bool _enableAutoExport = false;

        [Tooltip("编辑器启动时是否全量导出一次")]
        [SerializeField]
        private bool _exportOnEditorStart = false;

        [Header("路径设置")]
        [Tooltip("UI预制体源目录（相对于Assets）")]
        [SerializeField]
        private string _sourceFolderPath = DefaultSourceFolderPath;

        [Tooltip("导出目标目录（相对于项目根目录）")]
        [SerializeField]
        private string _outputFolderPath = DefaultOutputFolderPath;

        [Header("导出选项")]
        [Tooltip("是否导出 RectTransform 详细信息")]
        [SerializeField]
        private bool _exportRectTransformDetails = false;

        [Tooltip("是否导出组件的序列化属性值")]
        [SerializeField]
        private bool _exportSerializedProperties = true;

        [Tooltip("是否生成索引文件")]
        [SerializeField]
        private bool _generateIndexFile = true;

        #endregion

        #region 公开属性

        public bool EnableAutoExport => _enableAutoExport;
        public bool ExportOnEditorStart => _exportOnEditorStart;
        public string SourceFolderPath => _sourceFolderPath;
        public string OutputFolderPath => _outputFolderPath;
        public bool ExportRectTransformDetails => _exportRectTransformDetails;
        public bool ExportSerializedProperties => _exportSerializedProperties;
        public bool GenerateIndexFile => _generateIndexFile;

        /// <summary>
        /// 获取完整的输出目录路径
        /// </summary>
        public string FullOutputPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), _outputFolderPath);

        /// <summary>
        /// 获取完整的源目录路径
        /// </summary>
        public string FullSourcePath => Path.Combine(Application.dataPath, _sourceFolderPath.Replace("Assets/", ""));

        public bool SourcePathExists => AssetDatabase.IsValidFolder(_sourceFolderPath);

        #endregion

        #region 单例

        private static PrefabExporterSettings _instance;

        public static PrefabExporterSettings Instance => GetOrCreateSettings();

        private static PrefabExporterSettings GetOrCreateSettings()
        {
            if (_instance != null)
                return _instance;

            _instance = AssetDatabase.LoadAssetAtPath<PrefabExporterSettings>(SettingsPath);
            if (_instance == null)
            {
                _instance = CreateInstance<PrefabExporterSettings>();

                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(_instance, SettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PrefabExporter] 创建配置文件: {SettingsPath}");
            }

            _instance.NormalizePaths();
            return _instance;
        }

        #endregion

        #region Settings Provider

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/UNIHper/AI Copilot", SettingsScope.Project)
            {
                label = "AI Copilot",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(Instance);
                    settings.Update();

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("预制体结构导出器", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("将 UI 预制体的层级结构导出为 Markdown 文档，方便作为 AI 编程的上下文信息。", MessageType.Info);

                    EditorGUILayout.Space(10);

                    // 自动导出设置
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_enableAutoExport"),
                        new GUIContent("启用自动导出", "预制体保存时自动更新对应的 Markdown 文件")
                    );
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_exportOnEditorStart"),
                        new GUIContent("启动时全量导出", "编辑器启动时导出所有预制体")
                    );

                    EditorGUILayout.Space(10);

                    // 路径设置
                    EditorGUILayout.PropertyField(settings.FindProperty("_sourceFolderPath"), new GUIContent("源目录", "UI 预制体所在目录"));
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_outputFolderPath"),
                        new GUIContent("输出目录", "Markdown 文件输出目录（相对于项目根目录）")
                    );

                    if (!Instance.SourcePathExists)
                    {
                        EditorGUILayout.HelpBox($"源目录不存在: {Instance.SourceFolderPath}", MessageType.Warning);
                    }

                    EditorGUILayout.Space(10);

                    // 导出选项
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_exportRectTransformDetails"),
                        new GUIContent("导出 RectTransform 详情", "包含 anchors、pivot、sizeDelta 等信息")
                    );
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_exportSerializedProperties"),
                        new GUIContent("导出组件属性值", "导出 UI 组件的关键属性值")
                    );
                    EditorGUILayout.PropertyField(
                        settings.FindProperty("_generateIndexFile"),
                        new GUIContent("生成索引文件", "生成 _index.md 列出所有已导出的预制体")
                    );

                    EditorGUILayout.Space(20);

                    // 操作按钮
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.DisabledScope(!Instance.SourcePathExists))
                    {
                        if (GUILayout.Button("导出所有 UI 预制体", GUILayout.Height(30)))
                        {
                            ApplyIfModified(settings);
                            PrefabStructureExporter.ExportAllPrefabs();
                        }
                    }
                    if (GUILayout.Button("打开输出目录", GUILayout.Height(30)))
                    {
                        ApplyIfModified(settings);
                        var path = Instance.FullOutputPath;
                        if (Directory.Exists(path))
                        {
                            EditorUtility.RevealInFinder(path);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("提示", $"目录不存在: {path}", "确定");
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(20);

                    // AI Copilot 区域
                    EditorGUILayout.LabelField("AI Copilot", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Generate AI context: export prefab structures and framework guide to .github/copilot-instructions.md for GitHub Copilot.",
                        MessageType.Info
                    );

                    EditorGUILayout.Space(10);

                    // 主操作按钮
                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("🚀 Generate Context (Alt+Q)", GUILayout.Height(40)))
                    {
                        ApplyIfModified(settings);
                        AIContextGenerator.GenerateAIContext();
                    }
                    GUI.backgroundColor = previousColor;

                    EditorGUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Open Skills Folder", GUILayout.Height(25)))
                    {
                        AIContextGenerator.OpenSkillsDirectory();
                    }
                    EditorGUILayout.EndHorizontal();

                    ApplyIfModified(settings);
                },
                keywords = new[] { "AI", "Prefab", "Export", "Copilot", "Context" }
            };
            return provider;
        }

        private static void ApplyIfModified(SerializedObject settings)
        {
            if (!settings.hasModifiedProperties)
                return;

            settings.ApplyModifiedProperties();
            Instance.NormalizePaths();
            EditorUtility.SetDirty(Instance);
        }

        private void NormalizePaths()
        {
            if (string.IsNullOrWhiteSpace(_sourceFolderPath))
                _sourceFolderPath = DefaultSourceFolderPath;

            _sourceFolderPath = _sourceFolderPath.Replace('\\', '/').Trim();
            if (!_sourceFolderPath.StartsWith("Assets/"))
            {
                _sourceFolderPath = _sourceFolderPath.StartsWith("Assets")
                    ? _sourceFolderPath
                    : $"Assets/{_sourceFolderPath.TrimStart('/')}";
            }

            if (string.IsNullOrWhiteSpace(_outputFolderPath))
                _outputFolderPath = DefaultOutputFolderPath;

            _outputFolderPath = _outputFolderPath.Replace('\\', '/').Trim();
            _outputFolderPath = _outputFolderPath.TrimStart('/');
        }

        #endregion
    }
}
