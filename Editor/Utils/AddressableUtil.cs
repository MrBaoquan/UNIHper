using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public class AddressableUtil {

    static bool isEntryExist () {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var _curDir = UNIEditorUtil.GetSelectedDirectory ();
        string guid = AssetDatabase.AssetPathToGUID (_curDir);
        var entry = settings.FindAssetEntry (guid);
        return entry != null;
    }

    [MenuItem ("Assets/Add to Addressable System")]
    static void Add2Addressable () {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var _curDir = UNIEditorUtil.GetSelectedDirectory ();
        string guid = AssetDatabase.AssetPathToGUID (_curDir);

        var _entry = settings.CreateOrMoveEntry (guid, settings.DefaultGroup, false, true);
        _entry.SetLabel ("default", true, true);

    }

    static List<string> builtinDirectories = new List<string> { "Resources", "StreamingAssets", "Packages" };
    [MenuItem ("Assets/Add to Addressable System", true)]
    static bool Add2AddressableValidate () {
        return !isEntryExist () && !builtinDirectories.Contains (Path.GetFileName (UNIEditorUtil.GetSelectedDirectory ()));
    }

    [MenuItem ("Assets/Remove From Addressable System")]
    static void RemoveFromAddressable () {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var _curDir = UNIEditorUtil.GetSelectedDirectory ();
        string guid = AssetDatabase.AssetPathToGUID (_curDir);
        settings.RemoveAssetEntry (guid, true);
    }

    [MenuItem ("Assets/Remove From Addressable System", true)]
    static bool RemoveFromAddressableValidate () {
        return isEntryExist ();
    }

}