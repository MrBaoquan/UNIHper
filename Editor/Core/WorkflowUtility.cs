using System.Linq;

using UnityEditor;
using System.Diagnostics;

namespace UNIHper.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Sirenix.OdinInspector;

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

        [MenuItem("UNIHper/Workflow/SVN Update Slim Repo", priority = 51)]
        public static void CleanAssets()
        {
            updateSVNDepth("exclude");
        }

        [MenuItem("UNIHper/Workflow/SVN Update Full Repo", priority = 52)]
        public static void SetFullSVNDepth()
        {
            updateSVNDepth("infinity");
        }

        private static void updateSVNDepth(string depth)
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
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "svn",
                    Arguments = $"update --set-depth {depth} \"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath)
                };

                try
                {
                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit(3000);
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (process.ExitCode == 0)
                        {
                            EditorUtility.DisplayProgressBar(
                                "Switch SVN Repo Mode",
                                $"Set depth to {depth} for {path}.",
                                _progress
                            );
                        }
                        else
                        {
                            Debug.LogError(
                                $"Failed to set depth to {depth} for {path}. Error: {error}"
                            );
                        }
                    }
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
