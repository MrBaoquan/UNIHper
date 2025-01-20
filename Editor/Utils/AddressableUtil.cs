using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace UNIHper.Editor
{
    public class AddressableUtil
    {
        public static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings LoadOrCreateAddressableSettings()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
            {
                var _settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                var _persistenceGroup = _settings.groups.FirstOrDefault(
                    g => g.name == "Persistence Assets"
                );
                if (_persistenceGroup != null)
                {
                    return AddressableAssetSettingsDefaultObject.Settings;
                }

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

        static bool checkEntryExist(string path)
        {
            var settings = LoadOrCreateAddressableSettings();
            var guid = AssetDatabase.AssetPathToGUID(path);
            var _entry = settings.FindAssetEntry(guid);
            return _entry != null;
        }

        static bool checkParentEntryExist(string path)
        {
            var settings = LoadOrCreateAddressableSettings();
            return path.GetParentPaths()
                .Select(_path => _path.ToForwardSlash())
                .Any(_path => checkEntryExist(_path));
        }

        [MenuItem("Assets/Add To Addressable System", priority = 1500)]
        static void Add2Addressable()
        {
            var settings = LoadOrCreateAddressableSettings();
            var _curDir = UNIEditorUtil.GetSelectedDirectory();
            string guid = AssetDatabase.AssetPathToGUID(_curDir);

            var _entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, true);
            _entry.SetLabel("default", true, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void AddToLabel(string label, string path)
        {
            var settings = LoadOrCreateAddressableSettings();
            var _guid = AssetDatabase.AssetPathToGUID(path);
            var _entry = settings.CreateOrMoveEntry(_guid, settings.DefaultGroup, false, true);
            _entry.SetLabel(label, true, true);
        }

        public static bool IsEntryExist(string path)
        {
            var settings = LoadOrCreateAddressableSettings();
            var _guid = AssetDatabase.AssetPathToGUID(path);
            var _entry = settings.FindAssetEntry(_guid);
            return _entry != null;
        }

        static List<string> builtinDirectories = new List<string>
        {
            "Assets/Resources",
            "Assets/StreamingAssets",
        };

        [MenuItem("Assets/Add To Addressable System", true)]
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

            return !checkEntryExist(_curDir) && !checkParentEntryExist(_curDir);
        }

        [MenuItem("Assets/Remove From Addressable System", priority = 1501)]
        static void RemoveFromAddressable()
        {
            var settings = LoadOrCreateAddressableSettings();
            var _curDir = UNIEditorUtil.GetSelectedDirectory();
            string guid = AssetDatabase.AssetPathToGUID(_curDir);
            settings.RemoveAssetEntry(guid, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Remove From Addressable System", true)]
        static bool RemoveFromAddressableValidate()
        {
            var _curDir = UNIEditorUtil.GetSelectedDirectory();
            return checkEntryExist(_curDir);
        }
    }
}
