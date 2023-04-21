using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UNIHper.Editor
{
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
            if (
                !ProjectName.StartsWith("UNIHper_template")
                && PlayerSettings.productName.StartsWith("UNIHper_template")
            )
                PlayerSettings.productName = ProjectName;
        }
    }
}
