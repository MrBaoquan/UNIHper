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
        const string sceneEntryName = "SceneEntry";

        [InitializeOnLoadMethod]
        public static void OnLoad () {
            EditorSceneManager.newSceneCreated += NewSceneCreatedCallback;
            EditorSceneManager.sceneSaved += SceneSaved;
        }

        private static void NewSceneCreatedCallback (Scene scene, NewSceneSetup setup, NewSceneMode mode) {

        }

        private static void SceneSaved (Scene scene) {
            CodeTemplateGenerator.CreateSceneScriptIfNotExists (scene.name);
        }

        [MenuItem ("UNIHper/Initialize", priority = 0)]
        public static void CreateDefault () {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
            var _sceneEntry = EditorSceneManager.GetSceneByName (sceneEntryName);
            if (!_sceneEntry.IsValid ()) {
                _sceneEntry = EditorSceneManager.NewScene (NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene (_sceneEntry, string.Format ("Assets/Scenes/{0}.unity", sceneEntryName));
            } else {
                var _activeScene = EditorSceneManager.GetActiveScene ();
                if (_activeScene.name != sceneEntryName) {
                    EditorSceneManager.LoadScene (sceneEntryName, LoadSceneMode.Single);
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
                string _UNIHperPrefabPath = @"Assets\UNIHper\Resources\Prefabs\UNIHper.prefab";
                UnityEngine.Object _UNIHperPrefab = AssetDatabase.LoadAssetAtPath (_UNIHperPrefabPath, typeof (GameObject));
                GameObject _newUNIHper = PrefabUtility.InstantiatePrefab (_UNIHperPrefab) as GameObject;
                _newUNIHper.name = "__UNIHper";
            }

            // 2.   复制 配置文件
            string _UNIHperConfigPath = Application.dataPath + "/UNIHper/Resources/Configs";
            string _customConfigPath = Application.dataPath + "/Resources/UNIHper";
            string _textTemplatePath = Application.dataPath + "/UNIHper/Editor/Templates";

            if (!Directory.Exists (_customConfigPath)) {
                Directory.CreateDirectory (_customConfigPath);
            }

            string _dstResPath = Path.Combine (_customConfigPath, "resources.json");
            if (!File.Exists (_dstResPath)) {
                File.Copy (Path.Combine (_UNIHperConfigPath, "res.json"), _dstResPath);
            }

            string _dstUIPath = Path.Combine (_customConfigPath, "uis.json");
            if (!File.Exists (_dstUIPath)) {
                File.Copy (Path.Combine (_UNIHperConfigPath, "ui.json"), _dstUIPath);
            }

            string _dstAssembliesConfigPath = Path.Combine (_customConfigPath, "assemblies.json");
            if (!File.Exists (_dstAssembliesConfigPath)) {
                File.Copy (Path.Combine (_textTemplatePath, "AssembliesTemplate.txt"), _dstAssembliesConfigPath);
            }

            // 做一些项目结构
            List<string> _frame_dirs = new List<string> {
                Path.Combine (Application.dataPath, "Develop/Scripts"),
                Path.Combine (Application.dataPath, "Develop/Scripts/UIs"),
                Path.Combine (Application.dataPath, "Develop/Scripts/Configs"),
                Path.Combine (Application.dataPath, "ArtAssets"),
                Path.Combine (Application.dataPath, "Resources/Textures"),
                Path.Combine (Application.dataPath, "Resources/Prefabs/UI/SceneEntry"),
            };

            _frame_dirs.ForEach (_path => {
                if (!Directory.Exists (_path)) {
                    Directory.CreateDirectory (_path);
                };
            });

            // 3.   创建程序集定义文件
            string _dstAssemblyPath = Path.Combine (Path.GetFullPath ("Assets/Develop/Scripts"), "GameMain.asmdef");
            if (!File.Exists (_dstAssemblyPath)) {
                File.Copy (Path.Combine (_textTemplatePath, "GameMainAssembly.txt"), _dstAssemblyPath);
            }

            AssetDatabase.Refresh ();
            Debug.Log ("UNIHper framework initalize successful.");
            //AssetDatabase.LoadAssetAtPath()
            //MonoScript _UNIHperScript = MonoScript.FromMonoBehaviour(UNIHper.UNIHperEntry);
        }
    }

}