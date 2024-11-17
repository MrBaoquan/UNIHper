using UnityEngine;

namespace System.IO
{
    public static class PathExtension
    {
        public static bool IsAbsolutedPath(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (Path.IsPathRooted(path))
                return true;

            if (path.StartsWith("jar:file://"))
                return true;

            return false;
        }
    }
}
