using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UNIHper {

    public class BuildUNIHper {

        [PostProcessBuildAttribute (1)]
        public static void OnPostprocessBuild (BuildTarget target, string pathToBuildProject) {

        }

    }

}