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
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
        {
            // Copy readme files
            var _projectDir = Directory.GetParent(Application.dataPath);
            var _buildDir = Path.GetDirectoryName(pathToBuildProject);

            new List<string> { "README.txt", "README.pdf", "README.html" }
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
            WorkflowUtility.DeleteMatchingFilesAndDirectories(streamingAssetsPath, "~$");
        }

        public static string GetStreamingAssetsPath(BuildTarget target, string pathToBuiltProject)
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
    }
}
