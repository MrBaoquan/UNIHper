using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UNIHper.Editor
{
    public class AVProEditor
    {
        const string avproPrefabPath =
            "Packages/com.parful.unihper/Assets/Resources/__Prefabs/Components/AVPro Player.prefab";
        const string multipleAVProPrefabPath =
            "Packages/com.parful.unihper/Assets/Resources/__Prefabs/Components/Multiple AVPro Player.prefab";

        [MenuItem("GameObject/UI/AVPro Player", false, 20)]
        static void InstallAVPro()
        {
            UNIEditorUtil.InstantiatePrefab(avproPrefabPath, "AVPro Player");
        }

        [MenuItem("GameObject/UI/AVPro Player", true)]
        static bool ValidateInstallAVProUI()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/UI/Multiple AVPro Player", false, 21)]
        static void InstallMultipleAVPro()
        {
            UNIEditorUtil.InstantiatePrefab(multipleAVProPrefabPath, "Multiple AVPro Player");
        }

        [MenuItem("GameObject/UI/Multiple AVPro Player", true)]
        static bool ValidateInstallMultipleAVProUI()
        {
            return Selection.activeGameObject != null;
        }
    }
}
