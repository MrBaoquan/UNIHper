using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEditor.PackageManager.UI;

namespace UNIHper.Editor
{
    public class UNIHperEditor : UnityEditor.Editor
    {
        const string sceneEntryName = "SceneEntry";

        [InitializeOnLoadMethod]
        public static void OnLoad()
        {
            EditorSceneManager.newSceneCreated += NewSceneCreatedCallback;
            EditorSceneManager.sceneSaved += SceneSaved;

            var _assets = "Packages/com.parful.unihper/Assets";
            if (!AddressableUtil.IsEntryExist(_assets))
            {
                AddressableUtil.AddToLabel("com.parful.unihper", _assets);
            }
        }

        [MenuItem("UNIHper/Help/View Documentation", priority = 900)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://parful.gitbook.io/unihper-docs");
        }

        [MenuItem("UNIHper/Help/Check for Updates", priority = 901)]
        public static void CheckForUpdates()
        {
            // 打开
            Window.Open("com.parful.unihper");
        }

        private static void NewSceneCreatedCallback(Scene scene, NewSceneSetup setup, NewSceneMode mode) { }

        [MenuItem("UNIHper/Settings", priority = 1000)]
        static void FindResource()
        {
            string path = "Assets/Resources/UNIHperSettings.asset";
            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UNIHperSettings));
            if (obj != null)
            {
                Selection.activeObject = obj;
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError("please click UNIHper initialize first");
            }
        }

        private static void SceneSaved(Scene scene)
        {
            if (scene.name == sceneEntryName) // 只自动创建SceneEntryScript脚本
                CodeTemplateGenerator.CreateSceneScriptIfNotExists(scene.name);
        }

        [MenuItem("UNIHper/Initialize", priority = 0)]
        public static void Initialize()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var _startupScenePath = AssetDatabase
                .FindAssets($"{sceneEntryName} t:Scene", null)
                .Select(_ => AssetDatabase.GUIDToAssetPath(_))
                .Where(_ => Path.GetFileNameWithoutExtension(_) == sceneEntryName)
                .FirstOrDefault();

            // 0. 默认场景设置
            if (_startupScenePath == default(string))
            {
                _startupScenePath = $"Assets/Scenes/{sceneEntryName}.unity";

                Debug.Log($"New scene {sceneEntryName} created");
                var _sceneEntry = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (!Directory.Exists(UNIPaths.ProjectAssetPath("Scenes")))
                {
                    Directory.CreateDirectory(UNIPaths.ProjectAssetPath("Scenes"));
                }

                EditorSceneManager.SaveScene(_sceneEntry, _startupScenePath);
            }
            else
            {
                var _activeScene = EditorSceneManager.GetActiveScene();
                if (_activeScene.name != sceneEntryName)
                {
                    EditorSceneManager.OpenScene(_startupScenePath, OpenSceneMode.Single);
                    Debug.Log($"Scene {sceneEntryName} opened");
                }
            }

            var _sceneBuildSettings = EditorBuildSettings.scenes.ToList();
            _sceneBuildSettings.RemoveAll(_ => AssetDatabase.LoadAssetAtPath<SceneAsset>(_.path) == null);

            if (!_sceneBuildSettings.Exists(_ => _.path == _startupScenePath))
            {
                _sceneBuildSettings.Insert(0, new EditorBuildSettingsScene(_startupScenePath, true));
            }
            EditorBuildSettings.scenes = _sceneBuildSettings.ToArray();

            // 1. 复制  UNIHper.prefab

#if UNITY_2023_1_OR_NEWER
            Component[] _objs =
                FindObjectsByType(
                    Type.GetType("UNIHper.UNIHperEntry, UNIHper, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    FindObjectsSortMode.None
                ) as Component[];
#else
            Component[] _objs =
                FindObjectsOfType(Type.GetType("UNIHper.UNIHperEntry, UNIHper, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
                as Component[];
#endif
            if (_objs.Length > 1)
            {
                _objs
                    .Skip(1)
                    .Select(_UNIHper => _UNIHper.gameObject)
                    .ToList()
                    .ForEach(_UNIHperGO =>
                    {
                        Debug.Log("Destory UNIHperEntry: " + _UNIHperGO.name);
                        DestroyImmediate(_UNIHperGO, true);
                    });
            }
            else if (_objs.Length <= 0)
            {
                var _projectStartupPrefabPath = "Assets/Resources/UNIHper/Prefabs/UNIHper.prefab";
                // csharpier-ignore
                var _fullProjectStartupPrefabPath =  UNIPaths.ProjectPath(_projectStartupPrefabPath);

                if (!Directory.Exists(Path.GetDirectoryName(_fullProjectStartupPrefabPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_fullProjectStartupPrefabPath));
                }
                if (!File.Exists(UNIPaths.ProjectPath(_projectStartupPrefabPath)))
                {
                    string _UNIHperPrefabPath = UNIPaths.PackagePath("Assets/UI Prefabs/UNIHper.prefab");
                    var _tempUNIHper = GameObject.Instantiate<GameObject>(
                        AssetDatabase.LoadAssetAtPath(_UNIHperPrefabPath, typeof(GameObject)) as GameObject
                    );

                    PrefabUtility.SaveAsPrefabAsset(_tempUNIHper, _projectStartupPrefabPath);
                    DestroyImmediate(_tempUNIHper);
                }

                UnityEngine.Object _UNIHperPrefab = AssetDatabase.LoadAssetAtPath(_projectStartupPrefabPath, typeof(GameObject));
                GameObject _newUNIHper = PrefabUtility.InstantiatePrefab(_UNIHperPrefab) as GameObject;
                _newUNIHper.name = "__UNIHper";
                var _activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(_activeScene);
                EditorSceneManager.SaveScene(_activeScene);
            }

            // 2.   复制 配置文件
            var _projectConfigDir = UNIPaths.ProjectAssetPath("Resources/UNIHper");

            if (!Directory.Exists(_projectConfigDir))
            {
                Directory.CreateDirectory(_projectConfigDir);
            }

            var _packageConfigDir = UNIPaths.PackagePath("Assets/Configs");
            string _dstResPath = Path.Combine(_projectConfigDir, "resources.json");
            if (!File.Exists(_dstResPath))
            {
                File.Copy(Path.Combine(_packageConfigDir, "res.json"), _dstResPath);
            }

            string _dstUIPath = Path.Combine(_projectConfigDir, "uis.json");
            if (!File.Exists(_dstUIPath))
            {
                File.Copy(Path.Combine(_packageConfigDir, "ui.json"), _dstUIPath);
            }

            string _dstREADME = Path.Combine(_projectConfigDir, "README.md");
            if (!File.Exists(_dstREADME))
            {
                File.Copy(Path.Combine(_packageConfigDir, "README.md"), _dstREADME);
            }

            string _dstAssembliesConfigPath = Path.Combine(_projectConfigDir, "assemblies.json");
            if (!File.Exists(_dstAssembliesConfigPath))
            {
                File.Copy(Path.Combine(UNIPaths.PackagePath("Editor/Templates/AssembliesTemplate.txt")), _dstAssembliesConfigPath);
            }

            string _configPath = "Assets/Resources/UNIHperSettings.asset";
            if (!File.Exists(UNIPaths.ProjectPath(_configPath)))
            {
                var _configInstance = AssetDatabase.LoadAssetAtPath(_configPath, typeof(UNIHperSettings));

                if (_configInstance == null)
                {
                    var _configAsset = ScriptableObject.CreateInstance<UNIHperSettings>();
                    _configAsset.defaultClickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(
                        UNIPaths.PackagePath("Assets/Audios/click_effect_00.wav")
                    );

                    // 设置 UI Toolkit 默认资源
                    SetupUIToolkitDefaultAssets(_configAsset);

                    AssetDatabase.CreateAsset(_configAsset, _configPath);
                }
            }
            else
            {
                // 已存在配置文件，检查并更新 UI Toolkit 默认资源
                var existingConfig = AssetDatabase.LoadAssetAtPath<UNIHperSettings>(_configPath);
                if (existingConfig != null)
                {
                    bool needSave = SetupUIToolkitDefaultAssets(existingConfig);
                    if (needSave)
                    {
                        EditorUtility.SetDirty(existingConfig);
                    }
                }
            }

            var _projectAssetRoot = UNIPaths.ProjectAssetPath("Assets");
            // 做一些项目结构
            new List<string>
            {
                UNIPaths.ProjectAssetPath("Develop/Scripts"),
                UNIPaths.ProjectAssetPath("Develop/Scripts/UIs"),
                UNIPaths.ProjectAssetPath("Develop/Scripts/Configs"),
                UNIPaths.ProjectAssetPath("Develop/Scripts/Game"),
                UNIPaths.ProjectAssetPath("ArtAssets"),
                UNIPaths.ProjectAssetPath("StreamingAssets"),
            }.ForEach(_path =>
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
            });

            // 3.   创建程序集定义文件
            string _dstAssemblyPath = Path.Combine(Path.GetFullPath("Assets/Develop/Scripts"), "GameMain.asmdef");

            if (UNIHperSettings.Instance.UseAssembly && !File.Exists(_dstAssemblyPath))
            {
                CodeTemplateGenerator.CreateGameMainAssemblyIfNotExists();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00ff00>UNIHper framework initialized successfully</color>");

            AddressableUtil.LoadOrCreateAddressableSettings();
            importTMPEssentialResourcesIfNotExists();

            ScriptCompileReloadTools.ManualReload();
        }

        private static void importTMPEssentialResourcesIfNotExists()
        {
            string[] _settings = AssetDatabase.FindAssets("t:TMP_Settings");
            if (_settings.Length > 0)
                return;
            string packageFullPath = TMP_EditorUtility.packageFullPath;

            //TMP Menu import way: TMP_PackageUtilities.ImportProjectResourcesMenu();

            AssetDatabase.ImportPackage(packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage", false);
        }

        /// <summary>
        /// 设置 UI Toolkit 默认资源
        /// </summary>
        /// <param name="config">UNIHperSettings 配置</param>
        /// <returns>是否有更改</returns>
        private static bool SetupUIToolkitDefaultAssets(UNIHperSettings config)
        {
            bool hasChanges = false;

            // 1. 设置默认字体
            if (config.uiToolkitDefaultFont == null)
            {
                var defaultFont = AssetDatabase.LoadAssetAtPath<Font>(UNIPaths.PackagePath("Assets/Fonts/AlibabaPuHuiTi-2-55-Regular.ttf"));
                if (defaultFont != null)
                {
                    config.uiToolkitDefaultFont = defaultFont;
                    hasChanges = true;
                    Debug.Log("[UNIHper] 已设置 UI Toolkit 默认字体: AlibabaPuHuiTi-2-55-Regular");
                }
            }

            // 2. 设置默认样式表
            if (config.uiToolkitDefaultStyleSheet == null)
            {
                var defaultStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    UNIPaths.PackagePath("Assets/UIToolkit/DefaultUIToolkit.uss")
                );
                if (defaultStyleSheet != null)
                {
                    config.uiToolkitDefaultStyleSheet = defaultStyleSheet;
                    hasChanges = true;
                    Debug.Log("[UNIHper] 已设置 UI Toolkit 默认样式表: DefaultUIToolkit.uss");
                }
            }

            // 3. 复制或创建 PanelSettings 到项目目录
            string projectPanelSettingsPath = "Assets/Resources/UNIHper/UIToolkit/DefaultPanelSettings.asset";
            EnsureDirectoryExists(Path.GetDirectoryName(UNIPaths.ProjectPath(projectPanelSettingsPath)));

            if (config.uiToolkitPanelSettings == null)
            {
                // 检查项目中是否已存在
                var existingPanelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(projectPanelSettingsPath);
                if (existingPanelSettings != null)
                {
                    config.uiToolkitPanelSettings = existingPanelSettings;
                    hasChanges = true;
                }
                else
                {
                    // 创建新的 PanelSettings
                    var newPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                    newPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    newPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
                    newPanelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                    newPanelSettings.match = 0.5f;
                    newPanelSettings.sortingOrder = 1000; // 确保在 UGUI 之上

                    AssetDatabase.CreateAsset(newPanelSettings, projectPanelSettingsPath);
                    config.uiToolkitPanelSettings = newPanelSettings;
                    hasChanges = true;
                    Debug.Log("[UNIHper] 已创建 UI Toolkit 默认 PanelSettings");
                }
            }

            // 4. 复制框架内置 UI 资源到 Resources 目录
            CopyBuiltInUIToolkitAssets();

            return hasChanges;
        }

        /// <summary>
        /// 复制框架内置的 UI Toolkit 资源到项目 Resources 目录
        /// </summary>
        private static void CopyBuiltInUIToolkitAssets()
        {
            string targetDir = "Assets/Resources/UNIHper/UIToolkit";
            EnsureDirectoryExists(UNIPaths.ProjectPath(targetDir));

            // 需要复制的内置 UI 资源
            var builtInAssets = new[] { ("UNIDebuggerPanel.uxml", "UNIDebuggerPanel"), ("UNIDebuggerPanel.uss", "UNIDebuggerPanel"), };

            foreach (var (fileName, _) in builtInAssets)
            {
                string sourcePath = UNIPaths.PackagePath($"Assets/UIToolkit/{fileName}");
                string targetPath = $"{targetDir}/{fileName}";

                // 如果目标不存在或源文件更新，则复制
                if (!File.Exists(UNIPaths.ProjectPath(targetPath)))
                {
                    if (File.Exists(UNIPaths.ProjectPath(sourcePath)))
                    {
                        AssetDatabase.CopyAsset(sourcePath, targetPath);
                        Debug.Log($"[UNIHper] 已复制 UI 资源: {fileName}");
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
