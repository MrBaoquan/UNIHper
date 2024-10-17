using System.IO;
using UnityEngine;

namespace UNIHper.Editor
{
    public static class UNIPaths
    {
        public static string ToForwardSlash(this string str)
        {
            return str.Replace('\\', '/');
        }

        // get path relative to project root
        public static string ProjectPath(string path)
        {
            return Path.Combine(ProjectRoot, path).ToForwardSlash();
        }

        public static string ProjectAssetPath(string path)
        {
            return Path.Combine(ProjectAssetRoot, path).ToForwardSlash();
        }

        // get path relative to unihper package root
        public static string PackagePath(string path)
        {
            return Path.Combine(PackageRoot, path).ToForwardSlash();
        }

        // public static string PackagePathRelativeToProject(string path)
        // {
        //     return PackagePath(path).Replace(ProjectRoot, "").TrimStart('/').ToForwardSlash();
        // }

        const string bundleName = "com.parful.unihper";

        /// <summary>
        /// 包相关路径
        /// </summary>
        /// <value></value>
        public static string PackageRoot
        {
            get => $@"Packages\{bundleName}".ToForwardSlash();
        }

        /// <summary>
        /// 项目相关路径
        /// </summary>
        /// <value></value>
        public static string ProjectRoot
        {
            get => Path.GetDirectoryName(Application.dataPath).ToForwardSlash();
        }
        private static string ProjectAssetRoot
        {
            get => Application.dataPath.ToForwardSlash();
        }
    }
}
