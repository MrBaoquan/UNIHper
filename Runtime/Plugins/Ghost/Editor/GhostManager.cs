/*
 *  Description: Ghost Component Manager In Editor
 *  Author: MrBaoquan
 *  Date: 2023-03-21
 *  Email: mrma617@gmail.com
 */

/// <summary>
///  限制1:  脚本公开变量引用不能引用场景中的对象
///  限制2： 同一对象上的多个组件不能有多个相同组件
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
using UNIHper.Ghost.Util;

using System.Security.Cryptography;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UNIHper.Ghost.Editor
{
    public static class GhostManager
    {
        private static List<string> builtNameSpaces = new List<string>
        {
            "UnityEngine",
            "UnityEditor",
            "TMPro",
        };

        public static IEnumerable<Component> NonBuiltinComponents(this GameObject self)
        {
            return self.GetComponents<Component>()
                .Where(
                    _component =>
                        _component
                        && !builtNameSpaces.Exists(
                            _ => _component.GetType().Assembly.ToString().StartsWith(_)
                        )
                )
                .Where(_component => !GhostType.IsAssignableFrom(_component.GetType()));
        }

        public static bool HasNonBuiltinComponents(this GameObject self)
        {
            return self.NonBuiltinComponents().Count() > 0;
        }

        public static string ToForwardSlash(this string str)
        {
            return str.Replace('\\', '/');
        }

        // csharpier-ignore
        public static List<(GameObject parentObj, string prefabPath)> ParentInstanceRoots(this GameObject self)
        {
            List<GameObject> _parentRoots = new List<GameObject>();
            var _node = self.transform;
            while (_node is not null)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(_node.gameObject))
                {
                    _parentRoots.Add(_node.gameObject);
                }
                _node = _node.transform.parent ?? null;
            }
            //_parentRoots.Reverse ();
            return _parentRoots
                .Select(
                    _parentRoot =>
                        (
                            _parentRoot,
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_parentRoot)
                        )
                )
                .ToList();
        }

        private static bool DEBUG = false;

        private static void Log(object msg)
        {
            if (DEBUG)
            {
                Debug.Log(msg);
            }
        }

        private static void LogWarning(object msg)
        {
            if (DEBUG)
            {
                Debug.LogWarning(msg);
            }
        }

        private static void LogError(object msg)
        {
            if (DEBUG)
            {
                Debug.LogError(msg);
            }
        }

        private static Type ghostType = null;
        public static Type GhostType
        {
            get
            {
                if (ghostType == null)
                {
                    var _assembly = Assembly.Load(
                        new AssemblyName(
                            "UNIArt.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
                        )
                    );
                    if (_assembly is null)
                    {
                        LogWarning("can not find assembly");
                        return null;
                    }
                    var _ghostType = _assembly.GetType("UNIArt.Runtime.Ghost");
                    if (_ghostType is null)
                    {
                        LogWarning("can not find Ghost");
                        return null;
                    }
                    ghostType = _ghostType;
                }
                return ghostType;
            }
        }

        public static GhostData GhostData
        {
            get => GhostData.Instance;
        }

        private static string progressBarTitle = "Ghost Panel";

        private static void ShowLoading(string info, float progress)
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, info, progress);
        }

        private static string backupDir = "Temp/__BackupEntities";

        private static void backupEntityFolder()
        {
            var _projectDir = Path.GetDirectoryName(Application.dataPath);
            var _zipFile = Path.Combine(
                _projectDir,
                backupDir,
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip"
            );
            if (!Directory.Exists(Path.GetDirectoryName(_zipFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_zipFile));
            }
            try
            {
                if (Directory.Exists(Path.Combine(_projectDir, "Assets/GhostEntities")))
                    AssetDatabase.ExportPackage(
                        "Assets/GhostEntities",
                        _zipFile,
                        ExportPackageOptions.Recurse
                    );
            }
            catch (System.Exception) { }
        }

        private static void deleteEntityFolder()
        {
            var _failedPaths = new List<string>();
            AssetDatabase.DeleteAssets(new[] { "Assets/GhostEntities" }, _failedPaths);
        }

        private static string selectedGameObjectPath = string.Empty;
        private static UnityEngine.Object selectObject = null;
        private static string selectedScenePath = string.Empty;
        private static string stagePrefabPath = string.Empty;

        private static PrefabStage tempStage;

        private static bool stashWorkingEnv()
        {
            IsBusyNow = true;
            ghostType = null;
            selectedGameObjectPath = string.Empty;
            selectObject = null;
            selectedScenePath = string.Empty;
            stagePrefabPath = string.Empty;
            if (Selection.activeGameObject)
                selectedGameObjectPath = Selection.activeGameObject.transform.GetFullPath("/");

            var _prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            tempStage = _prefabStage;
            if (_prefabStage)
            {
                stagePrefabPath = _prefabStage.assetPath;
                if (Selection.activeGameObject)
                {
                    var _objFullPath = Selection.activeGameObject.transform.GetFullPath("/");
                    if (
                        Regex.Replace(_objFullPath, @".*?/(.*)$", "$1")
                        == Path.GetFileNameWithoutExtension(_prefabStage.assetPath)
                    )
                    {
                        selectedGameObjectPath = string.Empty;
                    }
                    else
                    {
                        selectedGameObjectPath = Regex.Replace(
                            _objFullPath,
                            @"(.*?/){2}(.*)$",
                            "$2"
                        );
                    }
                }
            }

            selectObject = Selection.activeObject;
            selectedScenePath = EditorSceneManager.GetActiveScene().path;
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static void restoreWorkingEnv()
        {
            EditorSceneManager.OpenScene(selectedScenePath, OpenSceneMode.Single);
            if (stagePrefabPath != string.Empty)
            {
                var _prefabStage = PrefabStageUtility.OpenPrefab(stagePrefabPath);
                if (!string.IsNullOrEmpty(selectedGameObjectPath))
                    Selection.activeGameObject = _prefabStage.prefabContentsRoot.transform
                        .Find(selectedGameObjectPath)
                        ?.gameObject;
            }
            else if (selectObject != null && AssetDatabase.Contains(selectObject))
            {
                Selection.activeObject = selectObject;
            }
            else if (selectedGameObjectPath != string.Empty)
            {
                Selection.activeGameObject = GameObject.Find(selectedGameObjectPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            IsBusyNow = false;
        }

        [MenuItem("UNIHper/Ghost Mode/Enable", priority = 21)]
        public static void EnableGhost()
        {
            AutoGenerateGhost();
            GenerateGhostEntities();
        }

        [MenuItem("UNIHper/Ghost Mode/Enable", true)]
        private static bool EnableGhostValidate()
        {
            return !GhostData.bGhost;
        }

        [MenuItem("UNIHper/Ghost Mode/Disable", priority = 22)]
        public static void DisableGhost()
        {
            RestoreGhostEntities();
            ClearAllGhostComponent();
        }

        [MenuItem("UNIHper/Ghost Mode/Disable", true)]
        private static bool DisableGhostValidate()
        {
            return GhostData.bGhost;
        }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Generate All Ghost Entities", priority = 33)]
        public static void GenerateGhostEntities()
        {
            safeTransaction(
                () =>
                {
                    ghostType = null;

                    progressBarTitle = "Generate Ghost Entities";
                    // GhostData.ClearGhostMetaData();
                    ShowLoading("backup older entities", 0.1f);
                    backupEntityFolder();

                    ShowLoading("delete the entity folder", 0.3f);
                    // deleteEntityFolder();

                    ShowLoading("generate asset entities", 0.4f);
                    GenerateAssetsEntities();

                    ShowLoading("generate scene entities", 0.8f);
                    GenerateScenesEntities();

                    ShowLoading("restore working environment", 1.0f);
                    EditorUtility.ClearProgressBar();

                    GhostData.bGhost = true;
                    EditorUtility.SetDirty(GhostData);
                    AssetDatabase.SaveAssets();

                    Debug.Log("Ghost mode has been activated.");
                },
                (err) =>
                {
                    EditorUtility.ClearProgressBar();
                    GhostData.bGhost = false;
                    EditorUtility.SetDirty(GhostData);
                    AssetDatabase.SaveAssets();
                }
            );
        }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Generate All Ghost Entities", true)]
        private static bool GenerateGhostEntitiesValidate()
        {
            return !GhostData.bGhost;
        }

        // [MenuItem("UNIHper/Test %g")]
        private static void Test()
        {
            var _gameObj = Selection.activeGameObject;
            Debug.Log(PrefabUtility.IsAnyPrefabInstanceRoot(_gameObj));
            //PrefabUtility.UnpackPrefabInstance(_gameObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Restore All Ghost Entities", priority = 34)]
        private static void RestoreGhostEntities()
        {
            safeTransaction(
                () =>
                {
                    GhostData.MarkAsGhost();

                    ghostType = null;
                    progressBarTitle = "Generate Ghost Entities";

                    ShowLoading("restore asset entities", 0.1f);
                    RestoreAssetsEntities();

                    ShowLoading("restore scene entities", 0.3f);
                    RestoreSceneEntities();

                    ShowLoading("restore working environment", 1.0f);

                    EditorUtility.ClearProgressBar();

                    GhostData.bGhost = false;
                    EditorUtility.SetDirty(GhostData);
                    AssetDatabase.SaveAssets();

                    Debug.Log("Ghost mode has been deactivated.");
                },
                (err) =>
                {
                    EditorUtility.ClearProgressBar();
                    GhostData.bGhost = true;
                    EditorUtility.SetDirty(GhostData);
                    AssetDatabase.SaveAssets();
                }
            );
        }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Add Ghost To All", priority = 35)]
        public static void AutoGenerateGhost()
        {
            safeTransaction(() =>
            {
                // 1. Generate for all prefab game objects
                // AllPrefabComponents(
                //     typeof(Transform),
                //     (_component, _path) => addGhostComponent(_component)
                // );
                addGhostToAssets();
                // 2. generate for all scene game objects
                AllSceneObjects(typeof(Transform), (_go, _scene) => addGhostComponent(_go));
            });
        }

        /// <summary>
        /// Add ghost component to all searched game objects
        /// </summary>
        /// <param name="searchInFolders"></param>
        private static void addGhostToAssets(string[] searchInFolders = null)
        {
            AllPrefabComponents(
                typeof(Transform),
                (_component, _path) => addGhostComponent(_component),
                null,
                searchInFolders
            );
        }

        private static void removeGhostFromAssets(string[] searchInFolders = null)
        {
            AllPrefabComponents(
                GhostType,
                (_component, _path) => removeGhostComponent(_component),
                null,
                searchInFolders
            );
        }

        private static void addGhostComponent(GameObject gameObj)
        {
            gameObj
                .ParentInstanceRoots()
                .ForEach(_ =>
                {
                    var _prefabPath = _.prefabPath;
                    var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
                    _prefab
                        .GetComponentsInParent(typeof(Transform), true)
                        .ToList()
                        .ForEach(_comp =>
                        {
                            addGhostComponent(_comp.gameObject);
                        });

                    if (!PrefabUtility.IsPartOfModelPrefab((UnityEngine.Object)gameObj))
                        PrefabUtility.SavePrefabAsset(_prefab);
                });

            if (!gameObj.HasNonBuiltinComponents())
                return;
            if (gameObj.gameObject.GetComponent(GhostType) == null)
            {
                gameObj.gameObject.AddComponent(GhostType);
            }
        }

        // [MenuItem ("UNIHper/Add Ghost For All", true)]
        // public static bool AutoGenerateGhostValidate () {

        // }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Remove Ghost From All", priority = 36)]
        public static void ClearAllGhostComponent()
        {
            safeTransaction(() =>
            {
                AllPrefabComponents(
                    GhostType,
                    (_go, _path) =>
                    {
                        removeGhostComponent(_go);
                    }
                );
                AllSceneObjects(
                    GhostType,
                    (_go, _scene) =>
                    {
                        removeGhostComponent(_go);
                    }
                );
            });
        }

        [MenuItem("UNIHper/Ghost Mode/Advanced/Remove Ghost From All", true)]
        public static bool ClearAllGhostComponentValidate()
        {
            return !GhostData.bGhost;
        }

        private static void removeGhostComponent(GameObject gameObj)
        {
            gameObj
                .ParentInstanceRoots()
                .ForEach(_parentItem =>
                {
                    var _prefabPath = _parentItem.prefabPath;
                    var _originPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
                    _originPrefab
                        .GetComponentsInChildren(GhostType, true)
                        .ToList()
                        .ForEach(
                            (_ghostComponent) =>
                            {
                                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                                    _ghostComponent.gameObject
                                );
                                UnityEngine.Object.DestroyImmediate(_ghostComponent, true);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(
                                    _originPrefab
                                );
                            }
                        );
                    if (!PrefabUtility.IsPartOfModelPrefab(_originPrefab))
                        PrefabUtility.SavePrefabAsset(_originPrefab);
                });

            var component = gameObj.GetComponent(GhostType);
            if (component == null)
                return;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(component.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications((UnityEngine.Object)gameObj);
            UnityEngine.Object.DestroyImmediate(component, true);
            AssetDatabase.SaveAssetIfDirty((UnityEngine.Object)gameObj);
        }

        private static bool IsBusyNow = false;

        private static void safeTransaction(Action InAction, Action<Exception> OnError = null)
        {
            if (!stashWorkingEnv())
                return;
            try
            {
                InAction();
            }
            catch (System.IO.FileNotFoundException err)
            {
                Debug.LogWarning(
                    err.Message + " please check if GhostComponent Art scripts exists"
                );
                OnError?.Invoke(err);
            }
            catch (Exception err)
            {
                LogError(err.Message);
                LogError(err.StackTrace);
                OnError?.Invoke(err);
            }
            finally
            {
                restoreWorkingEnv();
            }
        }

        static readonly List<string> invalidFolders = new List<string>
        {
            "AddressableAssetsData",
            "GhostEntities",
            "StreamingAssets"
        };

        private static IEnumerable<string> filterDirectories()
        {
            var _excludeDirs = GhostData.excludeDirectories.Concat(invalidFolders).ToList();
            return new DirectoryInfo(Application.dataPath)
                .GetDirectories()
                .Where(_dir => !_excludeDirs.Contains(_dir.Name))
                .Select(_dir => Path.Combine("Assets", _dir.Name).ToForwardSlash());
        }

        /// <summary>
        /// 保存场景中幽灵的实体
        /// </summary>
        private static void GenerateScenesEntities()
        {
            tempAllEntityPaths.Clear();
            AllSceneObjects(
                GhostType,
                (_go, _scene) =>
                {
                    var _entityPath = buildSceneObjectEntitySavePath(_go);
                    generateSingleEntity(_go, _entityPath);
                }
            );
        }

        //构造场景对象的保存路径
        private static string buildSceneObjectEntitySavePath(GameObject sceneGO)
        {
            return Path.Combine(
                    "Assets/GhostEntities/",
                    sceneGO.scene.name,
                    sceneGO.transform.GetFullPath(".") + ".prefab"
                )
                .ToForwardSlash();
        }

        private static void AllSceneObjects<T>(
            T goType,
            Action<UnityEngine.GameObject, UnityEngine.SceneManagement.Scene> handler
        )
            where T : System.Type
        {
            AssetDatabase
                .FindAssets("t:Scene", filterDirectories().ToArray())
                .Select(_guid => AssetDatabase.GUIDToAssetPath(_guid))
                .Select(_path => (_path, AssetDatabase.LoadAssetAtPath<SceneAsset>(_path)))
                .ToList()
                .ForEach(_item =>
                {
                    var _scene = EditorSceneManager.OpenScene(_item._path, OpenSceneMode.Single);
#if UNITY_2023_1_OR_NEWER
                    UnityEngine.Object
                        .FindObjectsByType(goType, FindObjectsSortMode.None)
#else
                    UnityEngine.Object
                        .FindObjectsOfType(goType)
#endif
                        .OfType<Component>()
                        .ToList()
                        .ForEach(_component => handler(_component.gameObject, _scene));
                    EditorSceneManager.SaveScene(_scene);
                });
        }

        /// <summary>
        /// 保存资源中幽灵的实体
        /// </summary>
        private static void GenerateAssetsEntities(string[] searchInFolders = null)
        {
            tempAllEntityPaths.Clear();

            AllPrefabComponents(
                GhostType,
                (_ghostComponent, _prefabPath) =>
                {
                    var _entityPath = prefabAssetPathToGhostEntityPath(_prefabPath);
                    _entityPath = Path.Combine(
                            Path.GetDirectoryName(_entityPath),
                            _ghostComponent.transform.GetFullPath(".") + ".prefab"
                        )
                        .ToForwardSlash();
                    generateSingleEntity(_ghostComponent, _entityPath);
                },
                null,
                searchInFolders
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Make Ghosts", false, 81)]
        private static void GenerateFoldersEntities()
        {
            var _curDir = EditorUtil.GetSelectedDirectory();
            var _filterDirs = new string[] { _curDir };
            safeTransaction(() =>
            {
                addGhostToAssets(_filterDirs);
                GenerateAssetsEntities(_filterDirs);
            });
        }

        [MenuItem("Assets/Make Ghosts", true)]
        private static bool GenerateFoldersEntitiesValidate()
        {
            return isValidGhostFolder();
        }

        private static bool isValidGhostFolder()
        {
            var _selectedFolders = EditorUtil.GetSelectedDirectories();
            var _validFolders = _selectedFolders
                .Where(
                    _ =>
                        !invalidFolders.Exists(
                            _invalidFolder => _.StartsWith("Assets/" + _invalidFolder)
                        )
                )
                .ToList();
            if (_validFolders.Count > 0)
                return true;

            return false;
        }

        [MenuItem("Assets/Restore Entities", false, 82)]
        private static void restoreFoldersEntities()
        {
            var _curDir = EditorUtil.GetSelectedDirectory();
            var _filterDirs = new string[] { _curDir };
            safeTransaction(() =>
            {
                RestoreAssetsEntities(_filterDirs);
                removeGhostFromAssets(_filterDirs);
            });
        }

        [MenuItem("Assets/Restore Entities", true)]
        private static bool restoreFoldersEntitiesValidate()
        {
            return isValidGhostFolder();
        }

        [MenuItem("Assets/Export Package (Ghost Mode) %e", false, 83)]
        private static void exportPackageWithGhosts()
        {
            var _selectPaths = Selection
                .GetFiltered<UnityEngine.Object>(SelectionMode.Assets)
                .Select(_ => AssetDatabase.GetAssetPath(_))
                .Where(_ => _.StartsWith("Assets"))
                .ToList();
            if (_selectPaths.Count == 0)
            {
                Debug.LogWarning("Please select a folder or file to export");
                return;
            }

            string[] _folderNames = Application.dataPath.Split('/');
            var _projectName = _folderNames[_folderNames.Length - 2];

            var _savePath = EditorUtility.SaveFilePanel(
                "Export Package (Ghost Mode))",
                Application.dataPath.Replace("Assets", ""),
                $"{_projectName}_Art_{DateTime.Now:MMddHHmm}.unitypackage",
                "unitypackage"
            );

            if (string.IsNullOrEmpty(_savePath))
                return;

            EditorApplication.ExecuteMenuItem("Assets/Make Ghosts");

            var _uniArtDir = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                @"Packages\com.parful.uniart"
            );
            _selectPaths.Add(AssetDatabase.GetAssetPath(_uniArtDir));

            var _guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_uniArtDir));

            _selectPaths.Add(AssetDatabase.GetAssetPath(_uniArtDir));
            AssetDatabase.ExportPackage(
                _selectPaths.ToArray(),
                _savePath,
                ExportPackageOptions.Default
                    | ExportPackageOptions.Interactive
                    | ExportPackageOptions.Recurse
                    | ExportPackageOptions.IncludeDependencies
            );
        }

        [MenuItem("Assets/Export Package (Ghost Mode) %e", true)]
        private static bool exportPackageWithGhostsValidate()
        {
            return isValidGhostFolder();
        }

        private static void AllPrefabComponents<T>(
            T cType,
            Action<GameObject, string> handler,
            Func<(string _path, GameObject prefab), bool> condition = null,
            string[] searchInFolders = null
        )
            where T : System.Type
        {
            if (searchInFolders == null)
                searchInFolders = filterDirectories().ToArray();
            var _condition = condition ?? (_ => true);
            AssetDatabase
                .FindAssets("t:Prefab", searchInFolders)
                .Select(_guid => AssetDatabase.GUIDToAssetPath(_guid))
                .Select(_path => (_path, AssetDatabase.LoadAssetAtPath<GameObject>(_path)))
                .Where(_condition)
                .ToList()
                .ForEach(_item =>
                {
                    var _path = _item._path;
                    _item.Item2
                        .GetComponentsInChildren(cType, true)
                        .Select(_component => _component.gameObject)
                        .ToList()
                        .ForEach(_go =>
                        {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(_go);
                            handler(_go, _path);
                        });
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(_item.Item2);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_item.Item2);
                    PrefabUtility.SavePrefabAsset(_item.Item2);
                });
        }

        /// <summary>
        /// 根据幽灵组件生成实体保存路径
        /// </summary>
        /// <param name="ghostComponent"></param>
        /// <returns></returns>
        public static string buildGhostEntitySavePath(Component ghostComponent)
        {
            var _prefabStage = PrefabStageUtility.GetPrefabStage(ghostComponent.gameObject);
            var _entityPath = string.Empty;
            // 1. target is in prefab edit window
            if (_prefabStage)
            {
                _entityPath =
                    Path.GetDirectoryName(_prefabStage.assetPath)
                    + @"\"
                    + ghostComponent.transform.GetFullPath(
                        ".",
                        _prefabStage.prefabContentsRoot.transform.parent
                    )
                    + ".prefab";
                _entityPath = _entityPath.Replace(@"\", "/");
                _entityPath = prefabAssetPathToGhostEntityPath(_entityPath);
            }
            // 2. target is scene object
            else if (!string.IsNullOrEmpty(ghostComponent.gameObject.scene.name))
            {
                _entityPath = buildSceneObjectEntitySavePath(ghostComponent.gameObject);
            }
            // 3. target is asset
            else if (AssetDatabase.Contains(ghostComponent.gameObject))
            {
                _entityPath = AssetDatabase.GetAssetPath(ghostComponent.gameObject);
                _entityPath = prefabAssetPathToGhostEntityPath(_entityPath);
            }

            return _entityPath;
        }

        /// <summary>
        /// 获取幽灵组件是否已经恢复实体
        /// </summary>
        /// <param name="ghostComponent"></param>
        /// <returns></returns>
        public static bool IsGhostRestored(Component ghostComponent)
        {
            var _entityPath = buildGhostEntitySavePath(ghostComponent);
            return GhostData.GetGhostMeta(_entityPath).HasEntity;
        }

        /// <summary>
        /// 生成幽灵实体
        /// </summary>
        /// <param name="ghostComponent"></param>
        public static void GenerateGhostEntity(Component ghostComponent)
        {
            var _prefabStage = PrefabStageUtility.GetPrefabStage(ghostComponent.gameObject);
            var _entityPath = buildGhostEntitySavePath(ghostComponent);
            generateSingleEntity(ghostComponent.gameObject, _entityPath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 根据资源预制体路径生成幽灵实体路径
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <returns></returns>
        private static string prefabAssetPathToGhostEntityPath(string prefabPath)
        {
            return Regex
                .Replace(prefabPath, "^Assets/", "Assets/GhostEntities/Assets/")
                .Replace("Resources", "Resources_ghost")
                .ToForwardSlash();
        }

        /// <summary>
        /// 恢复幽灵实体
        /// </summary>
        /// <param name="ghostComponent"></param>
        public static void RestoreGhostEntity(Component ghostComponent)
        {
            var _prefabStage = PrefabStageUtility.GetPrefabStage(ghostComponent.gameObject);
            var _entityPath = buildGhostEntitySavePath(ghostComponent);
            restoreSingleEntity(ghostComponent.gameObject, _entityPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<string> tempAllEntityPaths = new List<string>();

        /// <summary>
        /// 保存单个实体
        /// </summary>
        /// <param name="ghostEntity"></param>
        /// <param name="entityPath"></param>
        private static void generateSingleEntity(
            GameObject ghostObj,
            string entityPath,
            bool forceOverwrite = false
        )
        {
            var ghostComponent = ghostObj.GetComponent(GhostType);
            var _ghostMeta = GhostData.GetGhostMeta(entityPath);
            if (!_ghostMeta.HasEntity)
            {
                LogWarning("组件已是幽灵:" + entityPath);
                return;
            }

            if (!forceOverwrite)
            {
                if (tempAllEntityPaths.Contains(entityPath))
                {
                    entityPath = AssetDatabase.GenerateUniqueAssetPath(entityPath);
                }
                tempAllEntityPaths.Add(entityPath);
            }

            if (!Directory.Exists(Path.GetDirectoryName(entityPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(entityPath));
            }
            Log("保存实体开始" + entityPath);

            // 如果是预制体实例则需要先保存原始预制体
            var _parentRoots = ghostObj.ParentInstanceRoots();
            //_parentRoots.Reverse ();
            _parentRoots.ForEach(_prefabRoot =>
            {
                LogError("保存预制体" + _prefabRoot.prefabPath);
                var _assetPath = _prefabRoot.prefabPath;
                var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
                var _entityPath = prefabAssetPathToGhostEntityPath(_assetPath);
                _prefab
                    .GetComponentsInChildren(GhostType, true)
                    .Select(_component => _component.gameObject)
                    .ToList()
                    .ForEach(_prefabGhostObj =>
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(_prefabGhostObj);
                        generateSingleEntity(_prefabGhostObj, _entityPath, true);
                        PrefabUtility.SavePrefabAsset(_prefab);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    });
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            });

            if (!_ghostMeta.HasEntity)
            {
                LogWarning("组件已是幽灵:" + entityPath);
                return;
            }

            var ghostEntity = ghostObj;
            var _newEntityPrefab = GameObject.Instantiate(
                ghostEntity,
                ghostEntity.transform.position,
                ghostEntity.transform.rotation,
                null
            );
            UnityEngine.Object.DestroyImmediate(_newEntityPrefab.GetComponent(GhostType));

            // 移除非内置组件
            var _nonBuiltinComponents = ghostEntity.NonBuiltinComponents().ToList();
            LogWarning("移除非内置组件目标 " + ghostEntity.transform.GetFullPath("/"));
            while (_nonBuiltinComponents.Count > 0)
            {
                var _component = _nonBuiltinComponents
                    .Where(_component => _component.gameObject.CanDestroy(_component.GetType()))
                    .First();
                LogWarning("移除组件: " + _component.GetType().Name);
                _nonBuiltinComponents.Remove(_component);
                UnityEngine.Object.DestroyImmediate(_component, true);
            }
            ;

            // 移除实体子组件
            Enumerable
                .Range(0, _newEntityPrefab.transform.childCount)
                .Select(_idx => _newEntityPrefab.transform.GetChild(_idx).gameObject)
                .ToList()
                .ForEach(_child =>
                {
                    UnityEngine.Object.DestroyImmediate(_child);
                });

            Log(
                "保存实体结束: "
                    + entityPath
                    + " 组件数量"
                    + _newEntityPrefab.GetComponents<Component>().Length
            );
            PrefabUtility.SaveAsPrefabAsset(_newEntityPrefab, entityPath, out bool success);
            var _assetGUID = AssetDatabase.AssetPathToGUID(entityPath);
            _ghostMeta.HasEntity = false;
            _ghostMeta.GUID = _assetGUID;
            if (PrefabUtility.IsPartOfAnyPrefab(ghostEntity))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ghostEntity);
            }
            AssetDatabase.SaveAssetIfDirty(ghostEntity);
            AssetDatabase.SaveAssets();
            UnityEngine.Object.DestroyImmediate(_newEntityPrefab);
        }

        private static void RestoreSceneEntities()
        {
            EditorSceneManager.SaveOpenScenes();
            AllSceneObjects(
                GhostType,
                (_go, _scene) =>
                {
                    var _entityPath = buildSceneObjectEntitySavePath(_go);
                    restoreSingleEntity(_go, _entityPath);
                }
            );
        }

        /// <summary>
        /// 恢复资源目录中幽灵的实体
        /// </summary>
        private static void RestoreAssetsEntities(string[] searchInFolders = null)
        {
            AllPrefabComponents(
                GhostType,
                (_component, _prefabPath) =>
                {
                    var _entityPath = prefabAssetPathToGhostEntityPath(_prefabPath);
                    _entityPath = Path.Combine(
                            Path.GetDirectoryName(_entityPath),
                            _component.transform.GetFullPath(".") + ".prefab"
                        )
                        .ToForwardSlash();

                    AssetDatabase.Refresh();
                    restoreSingleEntity(_component, _entityPath);
                },
                null,
                searchInFolders
            );
        }

        /// <summary>
        /// 恢复单个实体
        /// </summary>
        /// <param name=" ghostComponent "></param>
        public static void restoreSingleEntity(GameObject gameObj, string entityPath)
        {
            Log("开始恢复实体: " + entityPath);
            gameObj
                .ParentInstanceRoots()
                .ForEach(_root =>
                {
                    var _assetPath = _root.prefabPath;
                    var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
                    _prefab
                        .GetComponentsInChildren(GhostType, true)
                        .Select(_component => _component.gameObject)
                        .ToList()
                        .ForEach(_go =>
                        {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(_go.gameObject);
                            var _entityPath = prefabAssetPathToGhostEntityPath(_assetPath);
                            restoreSingleEntity(_go, _entityPath);
                            PrefabUtility.SavePrefabAsset(_prefab);
                        });
                });

            var ghostComponent = gameObj.GetComponent(GhostType);

            var _ghostMeta = GhostData.GetGhostMeta(entityPath);
            if (_ghostMeta.HasEntity)
            {
                LogWarning("实体已经存在: " + ghostComponent.transform.GetFullPath("/"));
                return;
            }
            _ghostMeta.HasEntity = true;

            if (PrefabUtility.IsPartOfAnyPrefab(ghostComponent))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ghostComponent);
            }

            var _assetPath = AssetDatabase.GUIDToAssetPath(_ghostMeta.GUID);
            var _entityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
            if (_entityPrefab is null)
                return;

            var _nonBuiltinComponents = _entityPrefab.NonBuiltinComponents().ToList();
            UnityEditor.Selection.activeGameObject = gameObj;
            LogWarning("组件目标: " + gameObj.transform.GetFullPath("/"));
            _nonBuiltinComponents.ForEach(_component =>
            {
                if (gameObj.GetComponent(_component.GetType()) == null)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(_component);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(gameObj);
                    LogWarning("添加组件 " + _component.GetType());
                }
                else
                {
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(_component);
                    LogWarning("粘贴组件 " + _component.GetType());
                }
                if (PrefabUtility.IsPartOfAnyPrefab(ghostComponent))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ghostComponent);
                }
                AssetDatabase.SaveAssetIfDirty(gameObj);
                AssetDatabase.Refresh();
            });
            Log("结束恢复: " + entityPath);
            if (PrefabUtility.IsPartOfAnyPrefab(ghostComponent))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ghostComponent);
            }
            AssetDatabase.SaveAssetIfDirty(gameObj);
            AssetDatabase.Refresh();
        }

        public static void CheckRemoveNonBuiltinComponents(Component component)
        {
            if (IsBusyNow)
                return;
            if (IsGhostRestored(component))
                return;

            var _nonBuiltinComponents = component.gameObject.NonBuiltinComponents().ToList();
            if (_nonBuiltinComponents.Count <= 0)
                return;

            Debug.LogWarning(
                "You can't add non built-in component because current component is in ghost mode."
            );

            while (_nonBuiltinComponents.Count > 0)
            {
                var _component = _nonBuiltinComponents
                    .Where(_component => _component.gameObject.CanDestroy(_component.GetType()))
                    .First();
                _nonBuiltinComponents.Remove(_component);
                UnityEngine.Object.DestroyImmediate(_component, true);
            }
            ;
        }
    }
}
