using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper.Editor
{
    /// <summary>
    /// 预制体结构导出器
    /// 将预制体层级结构导出为 AI 可读的 Markdown 文档
    /// </summary>
    public static class PrefabStructureExporter
    {
        #region Menu Items

        [MenuItem("UNIHper/AI Copilot/Export Selected Prefabs", priority = 14)]
        public static void ExportSelectedPrefabs()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("[AI Copilot] Please select one or more prefabs in Project window");
                return;
            }

            int exportedCount = 0;
            foreach (var obj in selectedObjects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (path.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        ExportPrefab(prefab, path);
                        exportedCount++;
                    }
                }
            }

            if (exportedCount > 0)
            {
                GenerateIndexFileIfEnabled();
                Debug.Log($"[AI Copilot] Exported {exportedCount} prefab(s)");
            }
            else
            {
                Debug.LogWarning("[AI Copilot] No prefab files in selection");
            }
        }

        [MenuItem("UNIHper/AI Copilot/Export Selected Prefabs", true)]
        public static bool ValidateExportSelectedPrefabs()
        {
            return Selection.objects.Any(obj => AssetDatabase.GetAssetPath(obj).EndsWith(".prefab"));
        }

        [MenuItem("UNIHper/AI Copilot/Export All UI Prefabs", priority = 15)]
        public static void ExportAllPrefabs()
        {
            var settings = PrefabExporterSettings.Instance;
            var sourcePath = settings.SourceFolderPath;

            if (!AssetDatabase.IsValidFolder(sourcePath))
            {
                Debug.LogError($"[AI Copilot] Source folder not found: {sourcePath}");
                return;
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { sourcePath });
            if (prefabGuids.Length == 0)
            {
                Debug.LogWarning($"[AI Copilot] No prefabs found in: {sourcePath}");
                return;
            }

            int exportedCount = 0;
            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    EditorUtility.DisplayProgressBar("Export Prefabs", $"Exporting: {prefab.name}", (float)i / prefabGuids.Length);

                    if (prefab != null)
                    {
                        ExportPrefab(prefab, path);
                        exportedCount++;
                    }
                }

                GenerateIndexFileIfEnabled();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[AI Copilot] Batch export completed: {exportedCount} prefab(s)");
        }

        [MenuItem("GameObject/📦 UNIHper/Export Hierarchy", priority = 0)]
        public static void ExportHierarchySelection()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[AI Copilot] Please select a GameObject in Hierarchy window");
                return;
            }

            var markdown = GenerateMarkdown(selected, "Hierarchy Selection", null);

            // 复制到剪贴板
            GUIUtility.systemCopyBuffer = markdown;

            Debug.Log($"[AI Copilot] Hierarchy structure copied to clipboard");
        }

        [MenuItem("GameObject/📦 UNIHper/Export Hierarchy", true)]
        public static bool ValidateExportHierarchySelection()
        {
            return Selection.activeGameObject != null;
        }

        #endregion

        #region 核心导出逻辑

        /// <summary>
        /// 导出单个预制体
        /// </summary>
        public static void ExportPrefab(GameObject prefab, string assetPath)
        {
            var settings = PrefabExporterSettings.Instance;
            var outputDir = settings.FullOutputPath;

            // 确保输出目录存在
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保持目录结构
            var relativePath = assetPath.Replace(settings.SourceFolderPath, "").TrimStart('/');
            var outputFileName = Path.ChangeExtension(relativePath, ".md").Replace("/", "_");
            var outputPath = Path.Combine(outputDir, outputFileName);

            // 生成 Markdown 内容
            var markdown = GenerateMarkdown(prefab, prefab.name, assetPath);

            // 写入文件
            File.WriteAllText(outputPath, markdown, Encoding.UTF8);
            Debug.Log($"[PrefabExporter] 导出: {outputPath}");
        }

        /// <summary>
        /// 生成 Markdown 文档
        /// </summary>
        private static string GenerateMarkdown(GameObject root, string title, string assetPath)
        {
            var settings = PrefabExporterSettings.Instance;
            var sb = new StringBuilder();

            // 文档头
            sb.AppendLine($"# {title}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(assetPath))
            {
                sb.AppendLine($"> 预制体路径: `{assetPath}`");
                sb.AppendLine();
            }

            sb.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 统计信息
            var stats = CollectStatistics(root);
            sb.AppendLine("## 📊 统计信息");
            sb.AppendLine();
            sb.AppendLine($"- 节点总数: {stats.NodeCount}");
            sb.AppendLine($"- 最大深度: {stats.MaxDepth}");
            if (stats.ComponentCounts.Count > 0)
            {
                sb.AppendLine($"- 主要组件:");
                foreach (var kvp in stats.ComponentCounts.OrderByDescending(x => x.Value).Take(10))
                {
                    sb.AppendLine($"  - {kvp.Key}: {kvp.Value}");
                }
            }
            sb.AppendLine();

            // 层级结构
            sb.AppendLine("## 🌲 层级结构");
            sb.AppendLine();
            sb.AppendLine("```");
            AppendHierarchyTree(sb, root, "", true);
            sb.AppendLine("```");
            sb.AppendLine();

            // 关键节点路径（用于代码生成）
            sb.AppendLine("## 🔗 关键节点路径");
            sb.AppendLine();
            sb.AppendLine("以下路径可用于 `transform.Find()` 或 UNIHper 的 `Get<T>()` 方法：");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            AppendKeyNodePaths(sb, root, "");
            sb.AppendLine("```");
            sb.AppendLine();

            // 详细节点信息
            sb.AppendLine("## 📋 节点详情");
            sb.AppendLine();
            AppendNodeDetails(sb, root, "", settings);

            return sb.ToString();
        }

        /// <summary>
        /// 追加关键节点路径（可交互组件）
        /// </summary>
        private static void AppendKeyNodePaths(StringBuilder sb, GameObject node, string path)
        {
            var currentPath = string.IsNullOrEmpty(path) ? "" : path;
            var relativePath = string.IsNullOrEmpty(path) ? node.name : $"{path}/{node.name}";

            // 检查是否为关键节点（可交互或有文本）
            var button = node.GetComponent<Button>();
            var toggle = node.GetComponent<Toggle>();
            var slider = node.GetComponent<Slider>();
            var inputField = node.GetComponent<TMP_InputField>() ?? (Component)node.GetComponent<InputField>();
            var tmpText = node.GetComponent<TMP_Text>();
            var text = node.GetComponent<Text>();
            var scrollRect = node.GetComponent<ScrollRect>();

            if (button != null)
                sb.AppendLine($"// Button: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<Button>();");
            if (toggle != null)
                sb.AppendLine($"// Toggle: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<Toggle>();");
            if (slider != null)
                sb.AppendLine($"// Slider: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<Slider>();");
            if (inputField != null)
                sb.AppendLine($"// InputField: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<TMP_InputField>();");
            if (tmpText != null && button == null && toggle == null)
                sb.AppendLine($"// Text: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<TMP_Text>();");
            else if (text != null && button == null && toggle == null)
                sb.AppendLine($"// Text: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<Text>();");
            if (scrollRect != null)
                sb.AppendLine($"// ScrollRect: {relativePath}\ntransform.Find(\"{relativePath}\").GetComponent<ScrollRect>();");

            // 递归子节点
            for (int i = 0; i < node.transform.childCount; i++)
            {
                var child = node.transform.GetChild(i).gameObject;
                var childPath = string.IsNullOrEmpty(path) ? node.name : relativePath;
                AppendKeyNodePaths(sb, child, childPath);
            }
        }

        /// <summary>
        /// 追加层级树（ASCII 树形结构）
        /// </summary>
        private static void AppendHierarchyTree(StringBuilder sb, GameObject node, string prefix, bool isLast)
        {
            var connector = isLast ? "└── " : "├── ";
            var components = GetComponentSummary(node);
            var activeMarker = node.activeSelf ? "" : " ❌隐藏";

            sb.AppendLine($"{prefix}{connector}{node.name}{activeMarker} {components}");

            var childPrefix = prefix + (isLast ? "    " : "│   ");
            var childCount = node.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                var child = node.transform.GetChild(i).gameObject;
                AppendHierarchyTree(sb, child, childPrefix, i == childCount - 1);
            }
        }

        /// <summary>
        /// 获取节点的组件摘要（简短形式）
        /// </summary>
        private static string GetComponentSummary(GameObject node)
        {
            var components = new List<string>();

            // 检查 UI 组件
            if (node.GetComponent<Button>())
                components.Add("🔘Button");
            if (node.GetComponent<Toggle>())
                components.Add("☑Toggle");
            if (node.GetComponent<Slider>())
                components.Add("🎚Slider");
            if (node.GetComponent<ScrollRect>())
                components.Add("📜ScrollRect");
            if (node.GetComponent<InputField>() || node.GetComponent<TMP_InputField>())
                components.Add("✏️Input");
            if (node.GetComponent<Dropdown>() || node.GetComponent<TMP_Dropdown>())
                components.Add("📋Dropdown");

            // 文本组件
            var tmpText = node.GetComponent<TMP_Text>();
            var legacyText = node.GetComponent<Text>();
            if (tmpText != null)
            {
                var preview = TruncateText(tmpText.text, 20);
                components.Add($"📝\"{preview}\"");
            }
            else if (legacyText != null)
            {
                var preview = TruncateText(legacyText.text, 20);
                components.Add($"📝\"{preview}\"");
            }

            // 图像组件
            var image = node.GetComponent<Image>();
            var rawImage = node.GetComponent<RawImage>();
            if (image != null && image.sprite != null)
            {
                components.Add($"🖼{TruncateText(image.sprite.name, 15)}");
            }
            else if (rawImage != null && rawImage.texture != null)
            {
                components.Add($"🖼{TruncateText(rawImage.texture.name, 15)}");
            }
            else if (image != null || rawImage != null)
            {
                components.Add("🖼Image");
            }

            // Canvas
            if (node.GetComponent<Canvas>())
                components.Add("📐Canvas");

            // 布局组件
            if (node.GetComponent<LayoutGroup>())
                components.Add("📏Layout");
            if (node.GetComponent<ContentSizeFitter>())
                components.Add("📏Fitter");

            if (components.Count == 0)
                return "";
            return $"[{string.Join(", ", components)}]";
        }

        /// <summary>
        /// 追加详细节点信息
        /// </summary>
        private static void AppendNodeDetails(StringBuilder sb, GameObject node, string path, PrefabExporterSettings settings)
        {
            var currentPath = string.IsNullOrEmpty(path) ? node.name : $"{path}/{node.name}";
            var components = node.GetComponents<Component>();

            // 过滤掉不需要详细展示的节点（只有 Transform/RectTransform）
            var significantComponents = components
                .Where(
                    c =>
                        c != null
                        && c.GetType() != typeof(Transform)
                        && c.GetType() != typeof(RectTransform)
                        && c.GetType() != typeof(CanvasRenderer)
                )
                .ToList();

            if (significantComponents.Count > 0)
            {
                sb.AppendLine($"### `{currentPath}`");
                sb.AppendLine();

                // RectTransform 信息
                if (settings.ExportRectTransformDetails)
                {
                    var rectTransform = node.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        sb.AppendLine("**RectTransform:**");
                        sb.AppendLine($"- anchoredPosition: {rectTransform.anchoredPosition}");
                        sb.AppendLine($"- sizeDelta: {rectTransform.sizeDelta}");
                        sb.AppendLine($"- anchorMin: {rectTransform.anchorMin}");
                        sb.AppendLine($"- anchorMax: {rectTransform.anchorMax}");
                        sb.AppendLine($"- pivot: {rectTransform.pivot}");
                        sb.AppendLine();
                    }
                }

                // 组件列表
                sb.AppendLine("**组件:**");
                foreach (var component in significantComponents)
                {
                    var componentName = component.GetType().Name;
                    sb.AppendLine($"- `{componentName}`");

                    if (settings.ExportSerializedProperties)
                    {
                        AppendComponentProperties(sb, component);
                    }
                }
                sb.AppendLine();
            }

            // 递归处理子节点
            for (int i = 0; i < node.transform.childCount; i++)
            {
                var child = node.transform.GetChild(i).gameObject;
                AppendNodeDetails(sb, child, currentPath, settings);
            }
        }

        /// <summary>
        /// 追加组件的关键属性
        /// </summary>
        private static void AppendComponentProperties(StringBuilder sb, Component component)
        {
            switch (component)
            {
                case Button button:
                    sb.AppendLine($"  - interactable: {button.interactable}");
                    sb.AppendLine($"  - transition: {button.transition}");
                    break;

                case Toggle toggle:
                    sb.AppendLine($"  - isOn: {toggle.isOn}");
                    sb.AppendLine($"  - interactable: {toggle.interactable}");
                    break;

                case Slider slider:
                    sb.AppendLine($"  - value: {slider.value}");
                    sb.AppendLine($"  - minValue: {slider.minValue}");
                    sb.AppendLine($"  - maxValue: {slider.maxValue}");
                    break;

                case TMP_Text tmpText:
                    sb.AppendLine($"  - text: \"{TruncateText(tmpText.text, 50)}\"");
                    sb.AppendLine($"  - fontSize: {tmpText.fontSize}");
                    sb.AppendLine($"  - alignment: {tmpText.alignment}");
                    break;

                case Text text:
                    sb.AppendLine($"  - text: \"{TruncateText(text.text, 50)}\"");
                    sb.AppendLine($"  - fontSize: {text.fontSize}");
                    sb.AppendLine($"  - alignment: {text.alignment}");
                    break;

                case Image image:
                    // 只输出有意义的信息，精简默认值
                    sb.AppendLine($"  - sprite: {(image.sprite != null ? TruncateText(image.sprite.name, 30) : "null")}");
                    if (image.color != Color.white)
                        sb.AppendLine($"  - color: {image.color}");
                    if (!image.raycastTarget)
                        sb.AppendLine($"  - raycastTarget: False");
                    if (image.type != Image.Type.Simple)
                        sb.AppendLine($"  - type: {image.type}");
                    break;

                case RawImage rawImage:
                    sb.AppendLine($"  - texture: {(rawImage.texture != null ? TruncateText(rawImage.texture.name, 30) : "null")}");
                    if (rawImage.color != Color.white)
                        sb.AppendLine($"  - color: {rawImage.color}");
                    break;

                case ScrollRect scrollRect:
                    sb.AppendLine($"  - horizontal: {scrollRect.horizontal}");
                    sb.AppendLine($"  - vertical: {scrollRect.vertical}");
                    sb.AppendLine($"  - movementType: {scrollRect.movementType}");
                    break;

                case TMP_InputField tmpInput:
                    sb.AppendLine($"  - text: \"{TruncateText(tmpInput.text, 30)}\"");
                    sb.AppendLine($"  - placeholder: \"{GetPlaceholderText(tmpInput)}\"");
                    sb.AppendLine($"  - contentType: {tmpInput.contentType}");
                    break;

                case InputField input:
                    sb.AppendLine($"  - text: \"{TruncateText(input.text, 30)}\"");
                    sb.AppendLine($"  - contentType: {input.contentType}");
                    break;

                case Canvas canvas:
                    sb.AppendLine($"  - renderMode: {canvas.renderMode}");
                    sb.AppendLine($"  - sortingOrder: {canvas.sortingOrder}");
                    break;

                case CanvasGroup canvasGroup:
                    sb.AppendLine($"  - alpha: {canvasGroup.alpha}");
                    sb.AppendLine($"  - interactable: {canvasGroup.interactable}");
                    sb.AppendLine($"  - blocksRaycasts: {canvasGroup.blocksRaycasts}");
                    break;

                case LayoutGroup layout:
                    sb.AppendLine($"  - padding: {layout.padding}");
                    sb.AppendLine($"  - childAlignment: {layout.childAlignment}");
                    if (layout is HorizontalOrVerticalLayoutGroup hvLayout)
                    {
                        sb.AppendLine($"  - spacing: {hvLayout.spacing}");
                    }
                    else if (layout is GridLayoutGroup gridLayout)
                    {
                        sb.AppendLine($"  - cellSize: {gridLayout.cellSize}");
                        sb.AppendLine($"  - spacing: {gridLayout.spacing}");
                    }
                    break;

                default:
                    // 对于自定义脚本组件，导出公开的序列化字段
                    AppendCustomComponentProperties(sb, component);
                    break;
            }
        }

        /// <summary>
        /// 导出自定义组件的序列化属性
        /// </summary>
        private static void AppendCustomComponentProperties(StringBuilder sb, Component component)
        {
            // 跳过 Unity 内置组件
            var typeName = component.GetType().FullName;
            if (typeName.StartsWith("UnityEngine.") || typeName.StartsWith("TMPro."))
                return;

            var serializedObj = new SerializedObject(component);
            var iterator = serializedObj.GetIterator();
            var hasProperties = false;

            // 跳过 m_Script 等内置属性
            iterator.NextVisible(true);

            while (iterator.NextVisible(false))
            {
                // 跳过内部属性
                if (iterator.name.StartsWith("m_"))
                    continue;

                var displayValue = GetSerializedPropertyDisplayValue(iterator);
                if (!string.IsNullOrEmpty(displayValue))
                {
                    sb.AppendLine($"  - {iterator.name}: {displayValue}");
                    hasProperties = true;
                }
            }

            if (!hasProperties)
            {
                sb.AppendLine($"  - (自定义脚本)");
            }
        }

        /// <summary>
        /// 获取 SerializedProperty 的显示值
        /// </summary>
        private static string GetSerializedPropertyDisplayValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("F2");
                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(property.stringValue) ? null : $"\"{TruncateText(property.stringValue, 30)}\"";
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames.Length > property.enumValueIndex && property.enumValueIndex >= 0
                        ? property.enumDisplayNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "null";
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                default:
                    return null; // 复杂类型不展示
            }
        }

        #endregion

        #region 索引文件生成

        /// <summary>
        /// 生成索引文件（如果启用）
        /// </summary>
        public static void GenerateIndexFileIfEnabled()
        {
            var settings = PrefabExporterSettings.Instance;
            if (!settings.GenerateIndexFile)
                return;

            var outputDir = settings.FullOutputPath;
            if (!Directory.Exists(outputDir))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("# UI 预制体结构索引");
            sb.AppendLine();
            sb.AppendLine($"> 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"> 源目录: `{settings.SourceFolderPath}`");
            sb.AppendLine();

            var mdFiles = Directory.GetFiles(outputDir, "*.md").Where(f => !Path.GetFileName(f).StartsWith("_")).OrderBy(f => f).ToList();

            sb.AppendLine($"## 📁 预制体列表 ({mdFiles.Count} 个)");
            sb.AppendLine();

            foreach (var file in mdFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                sb.AppendLine($"- [{fileName}]({Path.GetFileName(file)})");
            }

            var indexPath = Path.Combine(outputDir, "_index.md");
            File.WriteAllText(indexPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[PrefabExporter] 生成索引: {indexPath}");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 收集统计信息
        /// </summary>
        private static PrefabStatistics CollectStatistics(GameObject root)
        {
            var stats = new PrefabStatistics();
            CollectStatisticsRecursive(root, 0, stats);
            return stats;
        }

        private static void CollectStatisticsRecursive(GameObject node, int depth, PrefabStatistics stats)
        {
            stats.NodeCount++;
            stats.MaxDepth = Math.Max(stats.MaxDepth, depth);

            var components = node.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null)
                    continue;
                var typeName = component.GetType().Name;

                // 跳过基础组件统计
                if (typeName == "Transform" || typeName == "RectTransform" || typeName == "CanvasRenderer")
                    continue;

                if (!stats.ComponentCounts.ContainsKey(typeName))
                    stats.ComponentCounts[typeName] = 0;
                stats.ComponentCounts[typeName]++;
            }

            for (int i = 0; i < node.transform.childCount; i++)
            {
                CollectStatisticsRecursive(node.transform.GetChild(i).gameObject, depth + 1, stats);
            }
        }

        /// <summary>
        /// 截断文本
        /// </summary>
        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            text = text.Replace("\n", "\\n").Replace("\r", "");
            if (text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 获取 TMP_InputField 的 placeholder 文本
        /// </summary>
        private static string GetPlaceholderText(TMP_InputField inputField)
        {
            if (inputField.placeholder is TMP_Text placeholder)
            {
                return TruncateText(placeholder.text, 30);
            }
            return "";
        }

        #endregion

        #region 数据结构

        private class PrefabStatistics
        {
            public int NodeCount { get; set; }
            public int MaxDepth { get; set; }
            public Dictionary<string, int> ComponentCounts { get; } = new Dictionary<string, int>();
        }

        #endregion
    }
}
