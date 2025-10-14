using UnityEngine;
using System.IO;
using DNHper;

namespace UNIHper
{
    public static class PathUtils
    {
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

        /// <summary>
        /// 获取Plugins目录路径
        /// </summary>
        /// <param name="relativePath">相对路径（可选）</param>
        /// <returns>Plugins目录的绝对路径</returns>
        public static string GetPluginsPath(string relativePath = "")
        {
            string pluginsPath = Path.Combine(Application.dataPath, "Plugins");

            if (!string.IsNullOrEmpty(relativePath))
            {
                pluginsPath = Path.Combine(pluginsPath, relativePath);
            }

            return pluginsPath.ToForwardSlash();
        }

        /// <summary>
        /// 获取项目根目录路径
        /// 编辑器下：工程根目录（Assets的父目录）
        /// 运行时：可执行文件所在目录
        /// </summary>
        /// <param name="relativePath">相对路径（可选）</param>
        /// <returns>项目根目录的绝对路径</returns>
        public static string GetProjectPath(string relativePath = "")
        {
            string projectPath;

#if UNITY_EDITOR
            // 编辑器模式：返回工程根目录（Assets的父目录）
            projectPath = Directory.GetParent(Application.dataPath).FullName;
#else
            // 运行时：返回可执行文件所在目录
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            // 获取可执行文件路径
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            projectPath = Path.GetDirectoryName(exePath);
#elif UNITY_ANDROID
            // Android 使用 persistentDataPath
            projectPath = Application.persistentDataPath;
#elif UNITY_IOS
            // iOS 使用 persistentDataPath
            projectPath = Application.persistentDataPath;
#else
            // 其他平台回退到 dataPath 的父目录
            projectPath = Directory.GetParent(Application.dataPath).FullName;
#endif
#endif

            if (!string.IsNullOrEmpty(relativePath))
            {
                projectPath = Path.Combine(projectPath, relativePath);
            }

            return projectPath.ToForwardSlash();
        }

        /// <summary>
        /// 获取项目Data目录路径（运行时的Data文件夹）
        /// 仅在Standalone构建后有效
        /// </summary>
        /// <param name="relativePath">相对路径（可选）</param>
        /// <returns>Data目录的绝对路径</returns>
        public static string GetDataPath(string relativePath = "")
        {
            string dataPath;

#if UNITY_EDITOR
            // 编辑器模式：返回Assets目录
            dataPath = Application.dataPath;
#else
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            // Standalone平台：exe所在目录下的 *_Data 文件夹
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exePath);
            string exeName = Path.GetFileNameWithoutExtension(exePath);

#if UNITY_STANDALONE_OSX
            // macOS: MyApp.app/Contents
            dataPath = Path.Combine(exeDir, "Data");
#else
                    // Windows/Linux: MyApp_Data
                    dataPath = Path.Combine(exeDir, exeName + "_Data");
#endif
#else
            dataPath = Application.dataPath;
#endif
#endif

            if (!string.IsNullOrEmpty(relativePath))
            {
                dataPath = Path.Combine(dataPath, relativePath);
            }

            return dataPath.ToForwardSlash();
        }

        /// <summary>
        /// 检查路径是否存在
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <returns>路径是否存在</returns>
        public static bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        /// <summary>
        /// 确保目录存在，不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录是否存在或创建成功</returns>
        public static bool EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建目录失败: {directoryPath}\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取相对于项目根目录的相对路径
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>相对路径</returns>
        public static string GetRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            string projectPath = GetProjectPath();

            if (absolutePath.StartsWith(projectPath))
            {
                return absolutePath.Substring(projectPath.Length).TrimStart('/', '\\').ToForwardSlash();
            }

            return absolutePath;
        }
    }
}
