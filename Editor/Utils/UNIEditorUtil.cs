using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UNIEditorUtil
{
    public static string GetCurrentAssetDirectory()
    {
        foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (System.IO.Directory.Exists(path))
                return path;
            else if (System.IO.File.Exists(path))
                return System.IO.Path.GetDirectoryName(path);
        }
        return "Assets";
    }

    public static string GetSelectedDirectory()
    {
        foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (System.IO.Directory.Exists(path))
                return path;
        }
        return string.Empty;
    }

    public static GameObject InstantiatePrefab(string prefabPath, string prefabName = "")
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found at " + prefabPath);
            return null;
        }
        var _newPrefab = GameObject.Instantiate(prefab, Selection.activeGameObject.transform);
        _newPrefab.name = string.IsNullOrEmpty(prefabName) ? prefab.name : prefabName;
        return _newPrefab;
    }
}
