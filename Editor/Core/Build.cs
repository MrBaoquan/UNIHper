using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }
    }
}
