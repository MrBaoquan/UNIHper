using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public class AddressableUtil
{
    static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings addressableSettings()
    {
        if (!AddressableAssetSettingsDefaultObject.SettingsExists)
        {
            var _settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var _group = _settings.CreateGroup(
                "Persistence Assets",
                true,
                false,
                true,
                _settings.DefaultGroup.Schemas
            );

            _settings.RemoveGroup(_settings.FindGroup("Default Local Group"));
            EditorUtility.SetDirty(_group);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        return AddressableAssetSettingsDefaultObject.Settings;
    }

    static bool isEntryExist()
    {
        var settings = addressableSettings();
        var _curDir = UNIEditorUtil.GetSelectedDirectory();
        string guid = AssetDatabase.AssetPathToGUID(_curDir);
        var entry = settings.FindAssetEntry(guid);
        return entry != null;
    }

    [MenuItem("Assets/Add to Addressable System")]
    static void Add2Addressable()
    {
        var settings = addressableSettings();
        var _curDir = UNIEditorUtil.GetSelectedDirectory();
        string guid = AssetDatabase.AssetPathToGUID(_curDir);

        var _entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, true);
        _entry.SetLabel("default", true, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static List<string> builtinDirectories = new List<string>
    {
        "Assets/Resources",
        "Assets/StreamingAssets",
        "Packages"
    };

    [MenuItem("Assets/Add to Addressable System", true)]
    static bool Add2AddressableValidate()
    {
        var _curDir = UNIEditorUtil.GetSelectedDirectory();
        if (string.IsNullOrEmpty(_curDir))
            return false;
        foreach (var _buildinDir in builtinDirectories)
        {
            if (_curDir.StartsWith(_buildinDir))
                return false;
        }

        return !isEntryExist();
    }

    [MenuItem("Assets/Remove From Addressable System")]
    static void RemoveFromAddressable()
    {
        var settings = addressableSettings();
        var _curDir = UNIEditorUtil.GetSelectedDirectory();
        string guid = AssetDatabase.AssetPathToGUID(_curDir);
        settings.RemoveAssetEntry(guid, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Remove From Addressable System", true)]
    static bool RemoveFromAddressableValidate()
    {
        return isEntryExist();
    }
}
