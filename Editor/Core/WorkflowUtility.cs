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
            bool isInvalidProductName = UNIHperSettings.InvalidAppNamePrefixes.Any(
                _prefix => PlayerSettings.productName.ToLower().StartsWith(_prefix)
            );

            if (isInvalidProductName)
                PlayerSettings.productName = ProjectName;
        }
    }
}
