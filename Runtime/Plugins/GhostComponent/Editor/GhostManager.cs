/// <summary>
/// 
/// description:   manager of ghost component in unity editor
/// 
/// author:         MrBaoquan
/// create date:    2023.3.15
/// email:          mrma617@gmail.com
/// 
/// 
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UNIHper.GhostComponent.Util;

namespace UNIHper.GhostComponent {

    public static class GhostManager {

        public static IEnumerable<Component> NonBuiltinComponents (this GameObject self) {
            return self.GetComponents<Component> ()
                .Where (_component => _component && !_component.GetType ().Assembly.FullName.StartsWith ("UnityEngine"))
                .Where (_component => !GhostType.IsAssignableFrom (_component.GetType ()));
        }

        public static bool HasNonBuiltinComponents (this GameObject self) {
            return self.NonBuiltinComponents ().Count () > 0;
        }

        private static Type ghostType = null;
        public static Type GhostType {
            get {
                if (ghostType == null) {
                    var _assembly = Assembly.Load (new AssemblyName ("UNIHper.Ghost.Art, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
                    if (_assembly is null) {
                        Debug.LogWarning ("can not find assembly");
                        return null;
                    }
                    var _ghostType = _assembly.GetType ("UNIHper.GhostComponent.Art.Ghost");
                    if (_ghostType is null) {
                        Debug.LogWarning ("can not find Ghost");
                        return null;
                    }
                    ghostType = _ghostType;
                }
                return ghostType;
            }
        }

        public static FieldInfo EntityGUIDField {
            get => GhostType.GetField ("entityGUID", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static FieldInfo HasEntityField {
            get => GhostType.GetField ("hasEntity", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static GhostData GhostData { get => GhostData.Instance; }

        private static string progressBarTitle = "Ghost Panel";
        private static void ShowLoading (string info, float progress) {
            EditorUtility.DisplayProgressBar (progressBarTitle, info, progress);
        }

        [MenuItem ("UNIHper/Test Ghost Unit %g")]
        private static void Test () {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript (Selection.activeGameObject);
            // Selection.activeGameObject.AddComponent (GhostType);
        }

        private static string backupDir = "Temp/__BackupEntities";
        private static void backupEntityFolder () {
            var _projectDir = Path.GetDirectoryName (Application.dataPath);
            var _zipFile = Path.Combine (_projectDir, backupDir, DateTime.Now.ToString ("yyyyMMdd_HHmmss") + ".zip");
            if (!Directory.Exists (Path.GetDirectoryName (_zipFile))) {
                Directory.CreateDirectory (Path.GetDirectoryName (_zipFile));
            }
            try {
                if (Directory.Exists (Path.Combine (_projectDir, "Assets/GhostEntities")))
                    AssetDatabase.ExportPackage ("Assets/GhostEntities", _zipFile, ExportPackageOptions.Recurse);
            } catch (System.Exception) { }

        }

        private static void deleteEntityFolder () {
            var _failedPaths = new List<string> ();
            AssetDatabase.DeleteAssets (new [] { "Assets/GhostEntities" }, _failedPaths);
        }

        private static string selectedGameObjectPath = string.Empty;
        private static UnityEngine.Object selectObject = null;
        private static string selectedScenePath = string.Empty;
        private static string stagePrefabPath = string.Empty;

        private static PrefabStage tempStage;

        private static bool stashWorkingEnv () {
            ghostType = null;
            selectedGameObjectPath = string.Empty;
            selectObject = null;
            selectedScenePath = string.Empty;
            stagePrefabPath = string.Empty;
            if (Selection.activeGameObject)
                selectedGameObjectPath = Selection.activeGameObject.transform.GetFullPath ("/");

            var _prefabStage = PrefabStageUtility.GetCurrentPrefabStage ();
            tempStage = _prefabStage;
            if (_prefabStage) {
                stagePrefabPath = _prefabStage.assetPath;
                if (Selection.activeGameObject) {
                    var _objFullPath = Selection.activeGameObject.transform.GetFullPath ("/");
                    if (Regex.Replace (_objFullPath, @".*?/(.*)$", "$1") == Path.GetFileNameWithoutExtension (_prefabStage.assetPath)) {
                        selectedGameObjectPath = string.Empty;
                    } else {
                        selectedGameObjectPath = Regex.Replace (_objFullPath, @"(.*?/){2}(.*)$", "$2");
                    }
                }
            }

            selectObject = Selection.activeObject;
            selectedScenePath = EditorSceneManager.GetActiveScene ().path;
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
        }

        private static void restoreWorkingEnv () {
            EditorSceneManager.OpenScene (selectedScenePath, OpenSceneMode.Single);
            if (stagePrefabPath != string.Empty) {
                var _prefabStage = PrefabStageUtility.OpenPrefab (stagePrefabPath);
                if (!string.IsNullOrEmpty (selectedGameObjectPath))
                    Selection.activeGameObject = _prefabStage.prefabContentsRoot.transform.Find (selectedGameObjectPath).gameObject;
            } else if (selectObject != null && AssetDatabase.Contains (selectObject)) {
                Selection.activeObject = selectObject;
            } else if (selectedGameObjectPath != string.Empty) {
                Selection.activeGameObject = GameObject.Find (selectedGameObjectPath);
            }

            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
        }

        [MenuItem ("UNIHper/Ghost Mode/Enable")]
        public static void GenerateGhostEntities () {
            safeThrow (() => {
                ghostType = null;

                progressBarTitle = "Generate Ghost Entities";

                ShowLoading ("backup older entities", 0.1f);
                backupEntityFolder ();

                ShowLoading ("delete the entity folder", 0.3f);
                deleteEntityFolder ();

                ShowLoading ("generate scene entities", 0.4f);
                GenerateScenesEntities ();

                ShowLoading ("generate asset entities", 0.8f);
                GenerateAssetsEntities ();

                ShowLoading ("restore working environment", 1.0f);
                EditorUtility.ClearProgressBar ();

                GhostData.bGhost = true;
                EditorUtility.SetDirty (GhostData);
                AssetDatabase.SaveAssets ();

                // restoreWorkingEnv ();

                Debug.Log ("Ghost component enabled");

            });

        }

        [MenuItem ("UNIHper/Ghost Mode/Enable", true)]
        private static bool GenerateGhostEntitiesValidate () {
            return !GhostData.bGhost;
        }

        [MenuItem ("UNIHper/Ghost Mode/Disable")]
        private static void RestoreGhostEntities () {
            safeThrow (() => {
                ghostType = null;
                // if (!stashWorkingEnv ()) return;
                progressBarTitle = "Generate Ghost Entities";

                ShowLoading ("restore asset entities", 0.1f);
                RestoreAssetsEntities ();

                ShowLoading ("restore scene entities", 0.3f);
                RestoreSceneEntities ();

                ShowLoading ("restore working environment", 1.0f);

                EditorUtility.ClearProgressBar ();

                GhostData.bGhost = false;
                EditorUtility.SetDirty (GhostData);
                AssetDatabase.SaveAssets ();

                // restoreWorkingEnv ();

                Debug.Log ("Ghost component disabled");
            });

        }

        [MenuItem ("UNIHper/Ghost Mode/Disable", true)]
        private static bool RestoreGhostEntitiesValidate () {
            return GhostData.bGhost;
        }

        //TODO 遍历全部GameObject， 为有额外脚本以来的所有GameObject添加Ghost组件
        // TODO 菜单项: 1. 生成所有实体    2.  恢复所有实体    3. 清除所有幽灵组
        // TODO 控制哪些程序集的组件认定为内置组件

        [MenuItem ("UNIHper/Ghost Mode/Add Ghost For All")]
        public static void AutoGenerateGhost () {
            safeThrow (() => {
                // stashWorkingEnv ();
                // 1. Generate for all prefab game objects 
                AllPrefabComponents (typeof (Transform), (_component, _path) => {
                    if (!_component.gameObject.HasNonBuiltinComponents ()) return;
                    if (_component.gameObject.GetComponent (GhostType) == null) {
                        _component.gameObject.AddComponent (GhostType);
                    }
                    // PrefabUtility.RecordPrefabInstancePropertyModifications (_component.gameObject);
                    // AssetDatabase.SaveAssetIfDirty (_component.gameObject);
                });

                // 2. generate for all scene game objects
                AllSceneObjects (typeof (Transform), (_component, _scene) => {
                    var _sceneObj = ((Component) _component).gameObject;
                    if (!_sceneObj.HasNonBuiltinComponents ()) return;
                    if (_sceneObj.GetComponent (GhostType) == null) {
                        _sceneObj.AddComponent (GhostType);
                    }
                });

                // restoreWorkingEnv ();
            });

        }

        // [MenuItem ("UNIHper/Add Ghost For All", true)]
        // public static bool AutoGenerateGhostValidate () {

        // }

        [MenuItem ("UNIHper/Ghost Mode/Remove Ghost From All")]
        public static void ClearAllGhostComponent () {
            safeThrow (() => {
                // stashWorkingEnv ();
                AllPrefabComponents (GhostType, (_component, _path) => {
                    var _prefabObj = _component.gameObject;
                    UnityEngine.Object.DestroyImmediate (_component, true);
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript (_component.gameObject);
                    PrefabUtility.RecordPrefabInstancePropertyModifications (_prefabObj);
                    AssetDatabase.SaveAssetIfDirty (_prefabObj);
                });
                AllSceneObjects (GhostType, (_component, _scene) => {
                    var _prefabObj = ((Component) _component).gameObject;
                    UnityEngine.Object.DestroyImmediate (_component, true);
                });
                // restoreWorkingEnv ();
            });
        }

        [MenuItem ("UNIHper/Ghost Mode/Remove Ghost From All", true)]
        public static bool ClearAllGhostComponentValidate () {
            return !GhostData.bGhost;
        }

        private static void safeThrow (Action InAction) {
            if (!stashWorkingEnv ()) return;
            try {
                InAction ();
            } catch (System.IO.FileNotFoundException err) {
                Debug.LogWarning (err.Message + " please check if GhostComponent Art scripts exists");
            } catch (Exception err) {
                Debug.LogError (err.Message);
            } finally {
                restoreWorkingEnv ();
            }
        }

        /// <summary>
        /// 保存场景中幽灵的实体
        /// </summary>
        private static void GenerateScenesEntities () {
            tempAllEntityPaths.Clear ();

            AllSceneObjects (GhostType, (_item, _scene) => {
                var _ghostComponent = _item as Component;
                var _entityPath = Path.Combine ("Assets/GhostEntities/", _scene.name, _ghostComponent.transform.GetFullPath (".") + ".prefab");
                saveGhostEntity (_ghostComponent, _entityPath);
            });

        }

        private static void AllSceneObjects<T> (T goType, Action<UnityEngine.Object, UnityEngine.SceneManagement.Scene> handler) where T : System.Type {
            AssetDatabase.FindAssets ("t:Scene", new [] { "Assets" })
                .Select (_guid => AssetDatabase.GUIDToAssetPath (_guid))
                .Select (_path => (_path, AssetDatabase.LoadAssetAtPath<SceneAsset> (_path)))
                .ToList ()
                .ForEach (_item => {
                    var _scene = EditorSceneManager.OpenScene (_item._path, OpenSceneMode.Single);
                    UnityEngine.Object.FindObjectsOfType (goType)
                        .ToList ()
                        .ForEach (_go => handler (_go, _scene));
                    EditorSceneManager.SaveScene (_scene);
                });
        }

        /// <summary>
        /// 保存资源中幽灵的实体
        /// </summary>
        private static void GenerateAssetsEntities () {
            tempAllEntityPaths.Clear ();

            AllPrefabComponents (GhostType, (_ghostComponent, _prefabPath) => {
                var _entityPath = Regex.Replace (_prefabPath, "^Assets/", "Assets/GhostEntities/Assets/");
                _entityPath = Path.Combine (Path.GetDirectoryName (_entityPath), _ghostComponent.transform.GetFullPath (".") + ".prefab");
                saveGhostEntity (_ghostComponent, _entityPath);
            });

            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
        }

        private static void AllPrefabComponents<T> (T cType, Action<Component, string> handler, Func < (string _path, GameObject prefab), bool > condition = null) where T : System.Type {
            var _condition = condition??(_ => true);
            AssetDatabase.FindAssets ("t:Prefab", new [] { "Assets" })
                .Select (_guid => AssetDatabase.GUIDToAssetPath (_guid))
                .Select (_path => (_path, AssetDatabase.LoadAssetAtPath<GameObject> (_path)))
                .Where (_item => !_item._path.StartsWith ("Assets/GhostEntities"))
                .Where (_condition)
                .ToList ()
                .ForEach (_item => {
                    var _path = _item._path;
                    _item.Item2.GetComponentsInChildren (cType, true)
                        .ToList ()
                        .ForEach (_ghostComponent => {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript (_ghostComponent.gameObject);
                            handler (_ghostComponent, _path);
                        });
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript (_item.Item2);
                    PrefabUtility.RecordPrefabInstancePropertyModifications (_item.Item2);
                    PrefabUtility.SavePrefabAsset (_item.Item2);
                });
        }

        // 生成单个幽灵的实体
        public static void GenerateGhostEntity (Component ghostComponent) {
            var _prefabStage = PrefabStageUtility.GetPrefabStage (ghostComponent.gameObject);
            var _entityPath = string.Empty;
            // 1. target is in prefab edit window
            if (_prefabStage) {
                _entityPath = Path.GetDirectoryName (_prefabStage.assetPath) + @"\" + ghostComponent.transform.GetFullPath (".", _prefabStage.prefabContentsRoot.transform.parent) + ".prefab";
                _entityPath = _entityPath.Replace (@"\", "/");
                _entityPath = Regex.Replace (_entityPath, "^Assets/", "Assets/GhostEntities/Assets/");
            }

            // 2. target is scene object
            else if (!string.IsNullOrEmpty (ghostComponent.gameObject.scene.name)) {
                _entityPath = Path.Combine ("Assets/GhostEntities/", ghostComponent.gameObject.scene.name, ghostComponent.transform.GetFullPath (".") + ".prefab");
            }

            // 3. target is asset
            else if (AssetDatabase.Contains (ghostComponent.gameObject)) {
                _entityPath = AssetDatabase.GetAssetPath (ghostComponent.gameObject);
                _entityPath = Regex.Replace (_entityPath, "^Assets/", "Assets/GhostEntities/Assets/");
            }

            saveGhostEntity (ghostComponent, _entityPath, true);
            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
        }

        // 恢复单个幽灵实体
        public static void RestoreGhostEntity (Component ghostComponent) {
            restoreGhostEntity (ghostComponent);
        }

        private static List<string> tempAllEntityPaths = new List<string> ();

        /// <summary>
        /// 保存单个实体
        /// </summary>
        /// <param name="ghostEntity"></param>
        /// <param name="entityPath"></param>
        private static void saveGhostEntity (Component ghostComponent, string entityPath, bool forceOverwrite = false) {

            if (!(bool) HasEntityField.GetValue (ghostComponent)) { //当前组件已经是幽灵模式则不进行后续操作
                return;
            }

            if (!forceOverwrite) {
                if (tempAllEntityPaths.Contains (entityPath)) {
                    entityPath = AssetDatabase.GenerateUniqueAssetPath (entityPath);
                }

                tempAllEntityPaths.Add (entityPath);
            }

            if (!Directory.Exists (Path.GetDirectoryName (entityPath))) {
                Directory.CreateDirectory (Path.GetDirectoryName (entityPath));
            }

            var ghostEntity = ghostComponent.gameObject;
            var _newEntityPrefab = GameObject.Instantiate (ghostEntity, ghostEntity.transform.position, ghostEntity.transform.rotation, null);
            UnityEngine.Object.DestroyImmediate (_newEntityPrefab.GetComponent (GhostType));

            // 移除非内置组件
            var _nonBuiltinComponents = ghostEntity.NonBuiltinComponents ().ToList ();

            while (_nonBuiltinComponents.Count > 0) {
                var _component = _nonBuiltinComponents
                    .Where (_component => _component.gameObject.CanDestroy (_component.GetType ()))
                    .First ();
                _nonBuiltinComponents.Remove (_component);
                UnityEngine.Object.DestroyImmediate (_component, true);
            };

            // 移除实体子组件
            Enumerable.Range (0, _newEntityPrefab.transform.childCount)
                .Select (_idx => _newEntityPrefab.transform.GetChild (_idx).gameObject)
                .ToList ()
                .ForEach (_child => {
                    UnityEngine.Object.DestroyImmediate (_child);
                });

            PrefabUtility.SaveAsPrefabAsset (_newEntityPrefab, entityPath, out bool success);
            EntityGUIDField.SetValue (ghostComponent, AssetDatabase.AssetPathToGUID (entityPath));
            HasEntityField.SetValue (ghostComponent, false);
            if (PrefabUtility.IsPartOfAnyPrefab (ghostComponent)) {
                PrefabUtility.RecordPrefabInstancePropertyModifications (ghostComponent);
            }
            AssetDatabase.SaveAssetIfDirty (ghostEntity);

            UnityEngine.Object.DestroyImmediate (_newEntityPrefab);
        }

        private static void RestoreSceneEntities () {
            EditorSceneManager.SaveOpenScenes ();
            AssetDatabase.FindAssets ("t:Scene", new [] { "Assets" })
                .Select (_guid => AssetDatabase.GUIDToAssetPath (_guid))
                .Select (_path => (_path, AssetDatabase.LoadAssetAtPath<SceneAsset> (_path)))
                .ToList ()
                .ForEach (_item => {
                    var _scene = EditorSceneManager.OpenScene (_item._path, OpenSceneMode.Single);
                    UnityEngine.Object.FindObjectsOfType (GhostType)
                        .OfType<Component> ()
                        .ToList ()
                        .ForEach (_ghostComponent => restoreGhostEntity (_ghostComponent));
                    EditorSceneManager.SaveScene (_scene);
                });
        }

        /// <summary>
        /// 恢复资源目录中幽灵的实体
        /// </summary>
        private static void RestoreAssetsEntities () {
            AssetDatabase.FindAssets ("t:Prefab", new [] { "Assets" })
                .Select (_guid => AssetDatabase.GUIDToAssetPath (_guid))
                .Select (_path => (_path, AssetDatabase.LoadAssetAtPath<GameObject> (_path)))
                .Where (_item => _item.Item2.GetComponent (GhostType) != null)
                .ToList ()
                .ForEach (_item => {
                    _item.Item2.GetComponentsInChildren (GhostType, true)
                        .ToList ()
                        .ForEach (_ghostComponent => restoreGhostEntity (_ghostComponent));
                    PrefabUtility.SavePrefabAsset (_item.Item2);
                });
        }

        /// <summary>
        /// 恢复单个实体
        /// </summary>
        /// <param name="ghostComponent"></param>
        public static void restoreGhostEntity (Component ghostComponent) {
            var _originalGhostObject = ghostComponent.gameObject;

            // 如果幽灵的实体已经存在，则不再进行恢复
            if ((bool) HasEntityField.GetValue (ghostComponent)) {
                return;
            }
            HasEntityField.SetValue (ghostComponent, true);
            EditorUtility.SetDirty (ghostComponent);

            if (PrefabUtility.IsPartOfAnyPrefab (ghostComponent)) {
                PrefabUtility.RecordPrefabInstancePropertyModifications (ghostComponent);
            }
            AssetDatabase.SaveAssetIfDirty (ghostComponent);
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate ();

            var _entityPrefab = AssetDatabase.LoadAssetAtPath<GameObject> (AssetDatabase.GUIDToAssetPath (EntityGUIDField.GetValue (ghostComponent) as string));
            if (_entityPrefab is null) return;

            var _nonBuiltinComponents = _entityPrefab.NonBuiltinComponents ()
                .ToList ();
            UnityEditor.Selection.activeGameObject = _originalGhostObject;
            _nonBuiltinComponents.ForEach (_component => {
                if (_originalGhostObject.GetComponent (_component.GetType ()) == null) {
                    UnityEditorInternal.ComponentUtility.CopyComponent (_component);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew (_originalGhostObject);
                } else {
                    UnityEditorInternal.ComponentUtility.PasteComponentValues (_component);
                }
            });

            if (PrefabUtility.IsPartOfAnyPrefab (ghostComponent)) {
                PrefabUtility.RecordPrefabInstancePropertyModifications (ghostComponent);
            }
        }
    }
}