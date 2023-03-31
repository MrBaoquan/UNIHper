using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UNIHper {

    public class UNIHperEditor : Editor {
        const string bundleName = "com.parful.unihper";
        const string sceneEntryName = "SceneEntry";

        /// <summary>
        /// 包相关路径
        /// </summary>
        /// <value></value>
        private static string packageRoot {
            get => Path.GetFullPath ($@"Packages\{bundleName}");
        }

        private static string packageResourcesDir {
            get => Path.Combine (packageRoot, "Resources");
        }

        private static string packageConfigDir {
            get => Path.Combine (packageRoot, @"Resources\Configs");
        }

        private static string packageTemplatesDir {
            get => Path.Combine (packageRoot, @"Editor\Templates");
        }

        /// <summary>
        /// 项目相关路径
        /// </summary>
        /// <value></value>
        private static string ProjectRoot {
            get => Path.GetDirectoryName (Application.dataPath);
        }
        private static string ProjectAssetRoot {
            get => Application.dataPath;
        }

        private static string ProjectConfigDir {
            get => Path.Combine (ProjectAssetRoot, @"Resources\UNIHper");
        }

        [InitializeOnLoadMethod]
        public static void OnLoad () {
            EditorSceneManager.newSceneCreated += NewSceneCreatedCallback;
            EditorSceneManager.sceneSaved += SceneSaved;
        }

        private static void NewSceneCreatedCallback (Scene scene, NewSceneSetup setup, NewSceneMode mode) {

        }

        [MenuItem ("UNIHper/Settings", false, 100)]
        static void FindResource () {
            string path = "Assets/Resources/UNIHperConfig.asset";
            var obj = AssetDatabase.LoadAssetAtPath (path, typeof (UNIHperConfig));
            if (obj != null) {
                Selection.activeObject = obj;
                AssetDatabase.Refresh ();
            }
        }

        private static void SceneSaved (Scene scene) {
            if (scene.name == sceneEntryName) // 只自动创建SceneEntryScript脚本
                CodeTemplateGenerator.CreateSceneScriptIfNotExists (scene.name);
        }

        [MenuItem ("UNIHper/Initialize", priority = -1)]
        public static void CreateDefault () {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
            var _startupScenePath = AssetDatabase.FindAssets ($"{sceneEntryName} t:Scene", null)
                .Select (_ => AssetDatabase.GUIDToAssetPath (_))
                .Where (_ => Path.GetFileNameWithoutExtension (_) == sceneEntryName)
                .FirstOrDefault ();

            if (_startupScenePath == default (string)) {
                Debug.Log ($"New scene {sceneEntryName} created");
                var _sceneEntry = EditorSceneManager.NewScene (NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (!Directory.Exists (Path.Combine (ProjectAssetRoot, "Scenes"))) {
                    Directory.CreateDirectory (Path.Combine (ProjectAssetRoot, "Scenes"));
                }
                EditorSceneManager.SaveScene (_sceneEntry, string.Format ("Assets/Scenes/{0}.unity", sceneEntryName));
            } else {
                var _activeScene = EditorSceneManager.GetActiveScene ();
                if (_activeScene.name != sceneEntryName) {
                    EditorSceneManager.OpenScene (_startupScenePath, OpenSceneMode.Single);
                    Debug.Log ($"Scene {sceneEntryName} opened");
                }
            }

            // 1. 复制  UNIHper.prefab
            Component[] _objs = FindObjectsOfType (Type.GetType ("UNIHper.UNIHperEntry, UNIHper, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")) as Component[];
            if (_objs.Length > 1) {
                _objs.Skip (1)
                    .Select (_UNIHper => _UNIHper.gameObject)
                    .ToList ()
                    .ForEach (_UNIHperGO => {
                        Debug.Log ("Destory UNIHperEntry: " + _UNIHperGO.name);
                        DestroyImmediate (_UNIHperGO, true);
                    });
            } else if (_objs.Length <= 0) {
                string _UNIHperPrefabPath = $@"Packages\{bundleName}\Resources\Prefabs\UNIHper.prefab";
                var _projectUnihperPrefabPath = "Assets/Resources/UNIHper/UNIHper.prefab";
                if (!File.Exists (Path.Combine (ProjectRoot, _projectUnihperPrefabPath))) {
                    AssetDatabase.CopyAsset (_UNIHperPrefabPath, _projectUnihperPrefabPath);
                }
                UnityEngine.Object _UNIHperPrefab = AssetDatabase.LoadAssetAtPath (_projectUnihperPrefabPath, typeof (GameObject));

                GameObject _newUNIHper = PrefabUtility.InstantiatePrefab (_UNIHperPrefab) as GameObject;
                _newUNIHper.name = "__UNIHper";
            }

            // 2.   复制 配置文件
            if (!Directory.Exists (UNIHperEditor.ProjectConfigDir)) {
                Directory.CreateDirectory (UNIHperEditor.ProjectConfigDir);
            }

            string _dstResPath = Path.Combine (ProjectConfigDir, "resources.json");
            if (!File.Exists (_dstResPath)) {
                File.Copy (Path.Combine (packageConfigDir, "res.json"), _dstResPath);
            }

            string _dstUIPath = Path.Combine (ProjectConfigDir, "uis.json");
            if (!File.Exists (_dstUIPath)) {
                File.Copy (Path.Combine (packageConfigDir, "ui.json"), _dstUIPath);
            }

            string _dstREADME = Path.Combine (ProjectConfigDir, "README.md");
            if (!File.Exists (_dstREADME)) {
                File.Copy (Path.Combine (packageConfigDir, "README.md"), _dstREADME);
            }

            string _dstAssembliesConfigPath = Path.Combine (ProjectConfigDir, "assemblies.json");
            if (!File.Exists (_dstAssembliesConfigPath)) {
                File.Copy (Path.Combine (packageTemplatesDir, "AssembliesTemplate.txt"), _dstAssembliesConfigPath);
            }

            string _configPath = "Assets/Resources/UNIHperConfig.asset";
            if (!File.Exists (Path.Combine (ProjectRoot, _configPath))) {
                var _configInstance = AssetDatabase.LoadAssetAtPath (_configPath, typeof (UNIHperConfig));
                if (_configInstance == null) {
                    var _configAsset = ScriptableObject.CreateInstance<UNIHperConfig> ();
                    AssetDatabase.CreateAsset (_configAsset, _configPath);
                }
            }

            // 做一些项目结构
            List<string> _frame_dirs = new List<string> {
                Path.Combine (ProjectAssetRoot, "Develop/Scripts"), // 脚本目录
                Path.Combine (ProjectAssetRoot, "Develop/Scripts/UIs"), // UI脚本
                Path.Combine (ProjectAssetRoot, "Develop/Scripts/Configs"), // 配置文件
                Path.Combine (ProjectAssetRoot, "Develop/Scripts/Game"), // 游戏逻辑
                Path.Combine (ProjectAssetRoot, "ArtAssets"), // 美术资源
                //Path.Combine (ProjectAssetRoot, "Resources/Textures"), // 贴图资源
                //Path.Combine (ProjectAssetRoot, "Resources/Prefabs/UIs/SceneEntry"), // 入口场景UI
            };

            _frame_dirs.ForEach (_path => {
                if (!Directory.Exists (_path)) {
                    Directory.CreateDirectory (_path);
                };
            });

            // 3.   创建程序集定义文件
            string _dstAssemblyPath = Path.Combine (Path.GetFullPath ("Assets/Develop/Scripts"), "GameMain.asmdef");
            if (!File.Exists (_dstAssemblyPath)) {
                File.Copy (Path.Combine (packageTemplatesDir, "GameMainAssembly.txt"), _dstAssemblyPath);
            }

            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
            Debug.Log ("UNIHper framework initalize successful.");

        }
    }

}