using System;
using System.Linq;
using UnityEditor;

namespace UNIHper.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public static class WorkflowUtility
    {
        public class UNIHperCache
        {
            // 之前是否打开过项目
            public bool hasOpenedBefore = true;
        }

        static string unihperCacheFilePath = Path.Combine(ProjectPath, "Library/UNIHperCache.json");
        public static string ProjectPath => Application.dataPath.Replace("/Assets", "");

        public static string ProjectName
        {
            get
            {
                string[] _folderNames = Application.dataPath.Split('/');
                return _folderNames[_folderNames.Length - 2];
            }
        }

        [InitializeOnLoadMethod]
        static void AutoWorkEnv()
        {
            if (SessionState.GetBool(bWorkflowComplete, false))
            {
                return;
            }
            EditorApplication.update += update;
        }

        private static bool setProductName()
        {
            if (SessionState.GetBool(bSetProductName, false))
            {
                return true;
            }

            Func<bool> isInvalidProductName = () =>
            {
                return UNIHperSettings.InvalidAppNamePrefixes.Any(
                    _prefix => PlayerSettings.productName.ToLower().StartsWith(_prefix)
                );
            };

            if (isInvalidProductName())
            {
                PlayerSettings.productName = ProjectName;
                SessionState.SetBool(bSetProductName, true);
            }

            return isInvalidProductName();
        }

        // 延迟调用
        private static void delayedCall()
        {
            moveOdinConfig();
        }

        const string bFirstOpenProject = "FirstOpenProject";
        const string bWorkflowComplete = "WorkflowComplete";
        const string bSetProductName = "SetProductName";
        const string bOdinConfigMoved = "OdinConfigMoved";

        private static bool checkAllWorkflowComplete()
        {
            return new List<string> { bSetProductName, bOdinConfigMoved }
                .Select(_field => SessionState.GetBool(_field, false))
                .All(b => b);
        }

        private static void firstOpenWorkflow()
        {
            if (File.Exists(unihperCacheFilePath) == false)
            {
                File.WriteAllText(
                    unihperCacheFilePath,
                    JsonConvert.SerializeObject(new UNIHperCache(), Formatting.Indented)
                );

                if (Application.HasProLicense())
                {
                    PlayerSettings.SplashScreen.show = false;
                }
            }
        }

        private static void update()
        {
            if (SessionState.GetBool(bFirstOpenProject, true))
            {
                firstOpenWorkflow();
                SessionState.SetBool(bFirstOpenProject, false);
            }
            if (SessionState.GetBool(bWorkflowComplete, false))
            {
                EditorApplication.update -= update;
                return;
            }

            setProductName();
            moveOdinConfig();

            if (checkAllWorkflowComplete())
            {
                SessionState.SetBool(bWorkflowComplete, true);
            }
        }

        private static bool moveOdinConfig()
        {
            if (SessionState.GetBool(bOdinConfigMoved, false))
            {
                return true;
            }

            var _configPath =
                "Assets/Packages/com.parful.unihper/Runtime/Plugins/Sirenix/Odin Inspector/Config/Editor/InspectorConfig.asset";
            var _newConfigPath = "Assets/Resources/Odin InspectorConfig.asset";
            Func<bool> _isOdinConfigExists = () =>
            {
                bool _completed =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_newConfigPath) != null
                    && AssetDatabase.IsValidFolder("Assets/Packages/com.parful.unihper") == false;

                if (_completed)
                {
                    SessionState.SetBool(bOdinConfigMoved, true);
                }

                return _completed;
            };

            var _odinConfig = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_configPath);
            if (_odinConfig == null)
            {
                if (AssetDatabase.IsValidFolder("Assets/Packages/com.parful.unihper") == true)
                {
                    AssetDatabase.DeleteAsset("Assets/Packages");
                }

                return _isOdinConfigExists();
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_newConfigPath) != null)
            {
                AssetDatabase.DeleteAsset("Assets/Packages");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return _isOdinConfigExists();
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var _opLog = AssetDatabase.MoveAsset(_configPath, _newConfigPath);
            if (!string.IsNullOrEmpty(_opLog))
            {
                Debug.LogWarning(_opLog);
                return _isOdinConfigExists();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return _isOdinConfigExists();
        }

        [MenuItem("UNIHper/Workflow/Restart Editor &r", priority = 41)]
        private static void RestartEditor()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AssetDatabase.SaveAssets();
                EditorApplication.OpenProject(ProjectPath);
            }
        }

        [MenuItem("UNIHper/Workflow/Clean Excluded Files", priority = 95)]
        public static void CleanExcludedPaths()
        {
            var _excludedPaths = UNIHperSettings.Instance.SVNExcludedPaths;
            foreach (var path in _excludedPaths)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            Debug.Log("<color=#00ff00>All excluded paths have been cleaned.</color>");
            AssetDatabase.Refresh();
        }

        [MenuItem("UNIHper/Workflow/SVN Update Slim Repo", priority = 71)]
        public static void CleanAssets()
        {
            UpdateSVNDepth("exclude");
        }

        [MenuItem("UNIHper/Workflow/SVN Update Full Repo", priority = 72)]
        public static void SetFullSVNDepth()
        {
            UpdateSVNDepth("infinity");
        }

        private static void UpdateSVNDepth(string depth)
        {
            EditorUtility.DisplayProgressBar(
                "Switch SVN Repo Mode",
                "Start setting SVN depth...",
                0
            );

            var _excludedPaths = UNIHperSettings.Instance.SVNExcludedPaths;
            foreach (var path in _excludedPaths)
            {
                var _progress = (float)(_excludedPaths.IndexOf(path) + 1) / _excludedPaths.Count;
                try
                {
                    var _output = ShellUtils.ExecuteCommand(
                        "svn",
                        $"update --set-depth {depth} \"{path}\"",
                        true
                    );
                    if (_output.HasErrors) { }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Exception while setting SVN depth: {ex.Message}");
                }
                finally
                {
                    EditorUtility.DisplayProgressBar(
                        "Switch SVN Repo Mode",
                        "Finished setting SVN depth.",
                        _progress
                    );
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        [MenuItem("UNIHper/Workflow/SVN Update Slim Repo", true)]
        [MenuItem("UNIHper/Workflow/SVN Update Full Repo", true)]
        private static bool checkIfSVNRepo()
        {
            var _repoPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".svn");
            return Directory.Exists(_repoPath);
        }

        public static void DeleteMatchingFilesAndDirectories(
            string path,
            string pattern,
            SearchOption option = SearchOption.TopDirectoryOnly
        )
        {
            if (Directory.Exists(path))
            {
                Regex regex = new Regex(pattern);

                // Delete matching files
                foreach (string file in Directory.GetFiles(path, "*", option))
                {
                    if (regex.IsMatch(Path.GetFileName(file)))
                    {
                        File.Delete(file);
                        Debug.Log($"Deleted file: {file}");
                    }
                }

                // Delete matching directories
                foreach (string dir in Directory.GetDirectories(path, "*", option))
                {
                    if (regex.IsMatch(new DirectoryInfo(dir).Name))
                    {
                        if (Directory.Exists(dir) == false)
                            continue;
                        Directory.Delete(dir, true);
                        Debug.Log($"Deleted directory: {dir}");
                    }
                }
            }
        }
    }
}
