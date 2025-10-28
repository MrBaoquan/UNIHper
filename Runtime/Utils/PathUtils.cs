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
            // 使用 Application.dataPath 推导可执行文件所在目录
            // Unity Standalone 构建后，dataPath 指向 *_Data 文件夹
            // 其父目录就是可执行文件所在目录
            projectPath = Directory.GetParent(Application.dataPath).FullName;
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
        /// 编辑器下：Assets目录
        /// 运行时：*_Data 文件夹
        /// </summary>
        /// <param name="relativePath">相对路径（可选）</param>
        /// <returns>Data目录的绝对路径</returns>
        public static string GetDataPath(string relativePath = "")
        {
            string dataPath = Application.dataPath;

            if (!string.IsNullOrEmpty(relativePath))
            {
                dataPath = Path.Combine(dataPath, relativePath);
            }

            return dataPath.ToForwardSlash();
        }

        /// <summary>
        /// 获取可执行文件路径
        /// 编辑器下：返回空字符串
        /// 运行时：返回 .exe 文件的完整路径
        /// </summary>
        /// <returns>可执行文件的绝对路径</returns>
        public static string GetExecutablePath()
        {
#if UNITY_EDITOR
            // 编辑器模式：返回空字符串
            return string.Empty;
#else
#if UNITY_STANDALONE_WIN
            // Windows Standalone: 通过 dataPath 推导
            // 例如: MyGame_Data -> MyGame.exe
            string dataPath = Application.dataPath;
            string exeDir = Directory.GetParent(dataPath).FullName;
            string exeName = new DirectoryInfo(dataPath).Name.Replace("_Data", "");
            return Path.Combine(exeDir, exeName + ".exe").ToForwardSlash();
#elif UNITY_STANDALONE_OSX
            // macOS: 从 .app 包中获取
            string dataPath = Application.dataPath;
            // dataPath 通常是: MyApp.app/Contents/Data
            // 可执行文件在: MyApp.app/Contents/MacOS/MyApp
            string contentsDir = Directory.GetParent(dataPath).FullName;
            string appDir = Directory.GetParent(contentsDir).FullName;
            string appName = new DirectoryInfo(appDir).Name.Replace(".app", "");
            return Path.Combine(contentsDir, "MacOS", appName).ToForwardSlash();
#elif UNITY_STANDALONE_LINUX
            // Linux: 类似 Windows
            string dataPath = Application.dataPath;
            string exeDir = Directory.GetParent(dataPath).FullName;
            string exeName = new DirectoryInfo(dataPath).Name.Replace("_Data", "");
            // Linux 可执行文件通常是 .x86_64 或无扩展名
            string exePath = Path.Combine(exeDir, exeName + ".x86_64");
            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(exeDir, exeName); // 无扩展名
            }
            return exePath.ToForwardSlash();
#else
            return string.Empty;
#endif
#endif
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
