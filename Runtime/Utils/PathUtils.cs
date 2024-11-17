using UnityEngine;
using System.IO;
using DNHper;

namespace UNIHper
{
    public static class PathUtils
    {
        // public static bool IsAbsolutedPath(string path)
        // {
        //     if (string.IsNullOrEmpty(path))
        //         return false;

        //     if (Path.IsPathRooted(path))
        //         return true;

        //     if (path.StartsWith("jar:file://"))
        //         return true;

        //     return false;
        // }

        // 获取StreamingAssets下的资源绝对路径
        public static string GetStreamingAssetsPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, relativePath).ToForwardSlash();
        }

        public static string GetPersistentDataPath(string relativePath)
        {
            return Path.Combine(Application.persistentDataPath, relativePath).ToForwardSlash();
        }

        public static string GetExternalAbsolutePath(string relativePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, relativePath).ToForwardSlash();

#else
            return Path.Combine(Application.streamingAssetsPath, relativePath).ToForwardSlash();
#endif
        }

        public static string BuildWebRequestJARUri(string filePath)
        {
            if (filePath.StartsWith("jar:file://"))
                return filePath;
            return "jar:file://" + filePath;
        }
    }
}
