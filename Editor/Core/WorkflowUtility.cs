using System.Linq;

using UnityEditor;

namespace UNIHper.Editor
{
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEngine;

    public static class WorkflowUtility
    {
        public static string ProjectName
        {
            get
            {
                string[] _folderNames = Application.dataPath.Split('/');
                return _folderNames[_folderNames.Length - 2];
            }
        }

        [InitializeOnLoadMethod]
        static void SetProductName()
        {
            bool isInvalidProductName = UNIHperSettings.InvalidAppNamePrefixes.Any(
                _prefix => PlayerSettings.productName.ToLower().StartsWith(_prefix)
            );

            if (isInvalidProductName)
                PlayerSettings.productName = ProjectName;
        }

        [MenuItem("UNIHper/Workflow/Clean Excluded Files", priority = 41)]
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
        }

        [MenuItem("UNIHper/Workflow/SVN Update Slim Repo", priority = 61)]
        public static void CleanAssets()
        {
            UpdateSVNDepth("exclude");
        }

        [MenuItem("UNIHper/Workflow/SVN Update Full Repo", priority = 62)]
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
