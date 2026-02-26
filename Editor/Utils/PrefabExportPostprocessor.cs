using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UNIHper.Editor
{
    /// <summary>
    /// 预制体导出自动触发器
    /// 监听预制体变更事件，根据配置自动导出
    /// </summary>
    public class PrefabExportPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// 编辑器启动时检查是否需要全量导出
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            // 延迟执行，确保编辑器完全加载
            EditorApplication.delayCall += () =>
            {
                // 使用 SessionState 确保每次编辑器启动只执行一次
                const string sessionKey = "PrefabExporter_StartupExported";
                if (SessionState.GetBool(sessionKey, false))
                    return;

                SessionState.SetBool(sessionKey, true);

                var settings = PrefabExporterSettings.Instance;
                if (settings.ExportOnEditorStart)
                {
                    Debug.Log("[PrefabExporter] 编辑器启动，执行全量导出...");
                    ExportAllPrefabsSilently();
                }
            };
        }

        /// <summary>
        /// 资源变更后处理
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            var settings = PrefabExporterSettings.Instance;
            if (!settings.EnableAutoExport)
                return;

            var sourcePath = settings.SourceFolderPath;
            bool needsIndexUpdate = false;

            // 处理导入/修改的预制体
            foreach (var path in importedAssets)
            {
                if (IsTargetPrefab(path, sourcePath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Debug.Log($"[PrefabExporter] 自动导出: {path}");
                        PrefabStructureExporter.ExportPrefab(prefab, path);
                        needsIndexUpdate = true;
                    }
                }
            }

            // 处理删除的预制体（删除对应的 md 文件）
            foreach (var path in deletedAssets)
            {
                if (IsTargetPrefab(path, sourcePath))
                {
                    DeleteExportedFile(path, settings);
                    needsIndexUpdate = true;
                }
            }

            // 处理移动的预制体
            for (int i = 0; i < movedAssets.Length; i++)
            {
                var fromPath = movedFromAssetPaths[i];
                var toPath = movedAssets[i];

                // 从目标目录移出
                if (IsTargetPrefab(fromPath, sourcePath) && !IsTargetPrefab(toPath, sourcePath))
                {
                    DeleteExportedFile(fromPath, settings);
                    needsIndexUpdate = true;
                }
                // 移入目标目录
                else if (!IsTargetPrefab(fromPath, sourcePath) && IsTargetPrefab(toPath, sourcePath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(toPath);
                    if (prefab != null)
                    {
                        Debug.Log($"[PrefabExporter] 自动导出(移入): {toPath}");
                        PrefabStructureExporter.ExportPrefab(prefab, toPath);
                        needsIndexUpdate = true;
                    }
                }
                // 在目标目录内移动/重命名
                else if (IsTargetPrefab(fromPath, sourcePath) && IsTargetPrefab(toPath, sourcePath))
                {
                    DeleteExportedFile(fromPath, settings);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(toPath);
                    if (prefab != null)
                    {
                        Debug.Log($"[PrefabExporter] 自动导出(重命名): {toPath}");
                        PrefabStructureExporter.ExportPrefab(prefab, toPath);
                        needsIndexUpdate = true;
                    }
                }
            }

            // 更新索引
            if (needsIndexUpdate)
            {
                PrefabStructureExporter.GenerateIndexFileIfEnabled();
            }
        }

        /// <summary>
        /// 判断是否为目标目录下的预制体
        /// </summary>
        private static bool IsTargetPrefab(string assetPath, string sourcePath)
        {
            return assetPath.StartsWith(sourcePath) && assetPath.EndsWith(".prefab");
        }

        /// <summary>
        /// 删除已导出的 Markdown 文件
        /// </summary>
        private static void DeleteExportedFile(string prefabPath, PrefabExporterSettings settings)
        {
            var relativePath = prefabPath.Replace(settings.SourceFolderPath, "").TrimStart('/');
            var outputFileName = System.IO.Path.ChangeExtension(relativePath, ".md").Replace("/", "_");
            var outputPath = System.IO.Path.Combine(settings.FullOutputPath, outputFileName);

            if (System.IO.File.Exists(outputPath))
            {
                System.IO.File.Delete(outputPath);
                Debug.Log($"[PrefabExporter] 删除导出文件: {outputPath}");
            }
        }

        /// <summary>
        /// 静默批量导出（无对话框）
        /// </summary>
        private static void ExportAllPrefabsSilently()
        {
            var settings = PrefabExporterSettings.Instance;
            var sourcePath = settings.SourceFolderPath;

            if (!AssetDatabase.IsValidFolder(sourcePath))
            {
                Debug.LogWarning($"[PrefabExporter] 源目录不存在: {sourcePath}");
                return;
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { sourcePath });
            if (prefabGuids.Length == 0)
            {
                Debug.Log($"[PrefabExporter] 目录下没有预制体: {sourcePath}");
                return;
            }

            int exportedCount = 0;
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    PrefabStructureExporter.ExportPrefab(prefab, path);
                    exportedCount++;
                }
            }

            PrefabStructureExporter.GenerateIndexFileIfEnabled();
            Debug.Log($"[PrefabExporter] 启动全量导出完成: {exportedCount} 个预制体");
        }
    }
}
