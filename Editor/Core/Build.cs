using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UNIHper.Editor
{
    public class BuildUNIHper
    {
        [MenuItem("UNIHper/Workflow/Clean Temporary Files", priority = 30)]
        public static void CleanAssets()
        {
            string assetsPath = Application.dataPath;
            string pattern = "~$"; // 正则表达式模式

            // 确认删除操作
            if (
                EditorUtility.DisplayDialog(
                    "Clean Temporary & Backup Files",
                    "Are you sure you want to delete all files and folders matching the pattern '*~' in the Assets directory?",
                    "Yes",
                    "No"
                )
            )
            {
                DeleteMatchingFilesAndDirectories(assetsPath, pattern);
                DeleteMatchingFilesAndDirectories(Application.streamingAssetsPath, pattern);
                AssetDatabase.Refresh();
                Debug.Log("Assets folder cleaned.");
            }
        }

        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
        {
            // Copy readme files
            var _projectDir = Directory.GetParent(Application.dataPath);
            var _buildDir = Path.GetDirectoryName(pathToBuildProject);

            new List<string>
            {
                "README.md",
                "Readme.md",
                "README.txt",
                "Readme.txt",
                "README.pdf",
                "Readme.pdf"
            }
                .Select(_readmeFile => Path.Combine(_projectDir.FullName, _readmeFile))
                .ToList()
                .ForEach(_readme =>
                {
                    if (!File.Exists(_readme))
                        return;
                    File.Copy(_readme, Path.Combine(_buildDir, Path.GetFileName(_readme)), true);
                });

            // 删除发布后目录StreamingAssets下符合*~的文件及文件夹
            string streamingAssetsPath = GetStreamingAssetsPath(target, pathToBuildProject);
            DeleteMatchingFilesAndDirectories(streamingAssetsPath, "~$");
        }

        private static string GetStreamingAssetsPath(BuildTarget target, string pathToBuiltProject)
        {
            string streamingAssetsPath = string.Empty;

            if (
                target == BuildTarget.StandaloneWindows
                || target == BuildTarget.StandaloneWindows64
                || target == BuildTarget.StandaloneOSX
            )
            {
                string dataPath = Path.GetFileNameWithoutExtension(pathToBuiltProject) + "_Data";
                streamingAssetsPath = Path.Combine(
                    Path.GetDirectoryName(pathToBuiltProject),
                    dataPath,
                    "StreamingAssets"
                );
            }
            else if (target == BuildTarget.Android)
            {
                streamingAssetsPath = pathToBuiltProject + "!assets";
            }
            else if (target == BuildTarget.iOS)
            {
                streamingAssetsPath = Path.Combine(pathToBuiltProject, "Data", "Raw");
            }

            return streamingAssetsPath;
        }

        private static void DeleteMatchingFilesAndDirectories(
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
