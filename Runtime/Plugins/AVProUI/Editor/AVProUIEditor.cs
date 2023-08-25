using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using AVProUI;

namespace AVProUI.Editor
{
    public class AVProUIEditor
    {
        const string avproPrefabPath =
            "Packages/com.parful.unihper/Runtime/Plugins/AVProUI/Assets/Prefabs/AVPro Player UI.prefab";

        [MenuItem("GameObject/UI/AVPro Player UI", false, 21)]
        static void InstallAVProUI()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(avproPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("AVProUI prefab not found at " + avproPrefabPath);
                return;
            }
            var _newAVProUI = GameObject.Instantiate(prefab, Selection.activeGameObject.transform);
            _newAVProUI.name = "AVPro Player UI";
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(
                _newAVProUI.GetComponent<AVProPlayerUI>(),
                true
            );
        }

        [MenuItem("GameObject/UI/AVPro Player UI", true)]
        static bool ValidateInstallAVProUI()
        {
            return Selection.activeGameObject != null;
        }
    }
}
