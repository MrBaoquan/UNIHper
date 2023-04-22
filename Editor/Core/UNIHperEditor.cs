using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        }

        private static void NewSceneCreatedCallback(
            Scene scene,
            NewSceneSetup setup,
            NewSceneMode mode
        ) { }

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
        public static void CreateDefault()
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
                var _sceneEntry = EditorSceneManager.NewScene(
                    NewSceneSetup.DefaultGameObjects,
                    NewSceneMode.Single
                );
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
            _sceneBuildSettings.RemoveAll(
                _ => AssetDatabase.LoadAssetAtPath<SceneAsset>(_.path) == null
            );

            if (!_sceneBuildSettings.Exists(_ => _.path == _startupScenePath))
            {
                _sceneBuildSettings.Insert(
                    0,
                    new EditorBuildSettingsScene(_startupScenePath, true)
                );
            }
            EditorBuildSettings.scenes = _sceneBuildSettings.ToArray();

            // 1. 复制  UNIHper.prefab
            Component[] _objs =
                FindObjectsOfType(
                    Type.GetType(
                        "UNIHper.UNIHperEntry, UNIHper, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
                    )
                ) as Component[];
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
                    string _UNIHperPrefabPath = UNIPaths.PackagePathRelativeToProject(
                        "Resources/Prefabs/UNIHper.prefab"
                    );
                    var _tempUNIHper = GameObject.Instantiate<GameObject>(
                        AssetDatabase.LoadAssetAtPath(_UNIHperPrefabPath, typeof(GameObject))
                            as GameObject
                    );

                    PrefabUtility.SaveAsPrefabAsset(_tempUNIHper, _projectStartupPrefabPath);
                    DestroyImmediate(_tempUNIHper);
                }

                UnityEngine.Object _UNIHperPrefab = AssetDatabase.LoadAssetAtPath(
                    _projectStartupPrefabPath,
                    typeof(GameObject)
                );
                GameObject _newUNIHper =
                    PrefabUtility.InstantiatePrefab(_UNIHperPrefab) as GameObject;
                _newUNIHper.name = "__UNIHper";
            }

            // 2.   复制 配置文件
            var _projectConfigDir = UNIPaths.ProjectAssetPath("Resources/UNIHper");

            if (!Directory.Exists(_projectConfigDir))
            {
                Directory.CreateDirectory(_projectConfigDir);
            }

            var _packageConfigDir = UNIPaths.PackagePath("Resources/Configs");
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
                File.Copy(
                    Path.Combine(UNIPaths.PackagePath(@"Editor\Templates\AssembliesTemplate.txt")),
                    _dstAssembliesConfigPath
                );
            }

            string _configPath = "Assets/Resources/UNIHperSettings.asset";
            if (!File.Exists(UNIPaths.ProjectPath(_configPath)))
            {
                var _configInstance = AssetDatabase.LoadAssetAtPath(
                    _configPath,
                    typeof(UNIHperSettings)
                );

                if (_configInstance == null)
                {
                    var _configAsset = ScriptableObject.CreateInstance<UNIHperSettings>();
                    _configAsset.defaultClickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(
                        UNIPaths.PackagePathRelativeToProject(
                            "Resources/Persistence/AudioClips/click_effect_00.wav"
                        )
                    );
                    AssetDatabase.CreateAsset(_configAsset, _configPath);
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
            }.ForEach(_path =>
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
            });

            // 3.   创建程序集定义文件
            string _dstAssemblyPath = Path.Combine(
                Path.GetFullPath("Assets/Develop/Scripts"),
                "GameMain.asmdef"
            );
            if (!File.Exists(_dstAssemblyPath))
            {
                File.Copy(
                    UNIPaths.PackagePath("Editor/Templates/GameMainAssembly.txt"),
                    _dstAssemblyPath
                );
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("UNIHper framework initalize successful.");
        }
    }
}
