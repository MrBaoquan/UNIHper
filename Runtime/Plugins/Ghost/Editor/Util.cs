using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UNIHper.Ghost.Util
{
    public static class EditorUtil
    {
        public static string GetCurrentAssetDirectory()
        {
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
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
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (System.IO.Directory.Exists(path))
                    return path;
            }
            return string.Empty;
        }

        public static List<string> GetSelectedDirectories()
        {
            return Selection
                .GetFiltered<UnityEngine.Object>(SelectionMode.Assets)
                .Select(obj => AssetDatabase.GetAssetPath(obj))
                .ToList();
        }
    }

    public static class GameObjectExtensions
    {
        public static bool Requires(Type obj, Type requirement)
        {
            return Attribute.IsDefined(obj, typeof(RequireComponent))
                && Attribute
                    .GetCustomAttributes(obj, typeof(RequireComponent))
                    .OfType<RequireComponent>()
                    .Any(
                        rc =>
                            new List<Type> { rc.m_Type0, rc.m_Type1, rc.m_Type2 }
                                .Where(rc => rc != null)
                                .Any(rc => rc.IsAssignableFrom(requirement))
                    );
        }

        public static bool CanDestroy(this GameObject go, Type t)
        {
            return !go.GetComponents<Component>().Any(c => Requires(c.GetType(), t));
        }

        // csharpier-ignore
        public static string GetFullPath(this Transform transform, string separator = "/", Transform stopParent = null)
        {
            string path = transform.name;
            while (transform.parent != null && transform.parent != stopParent)
            {
                transform = transform.parent;
                path = transform.name + separator + path;
            }
            return path;
        }
    }
}
