using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UNIHper.Editor
{
    /// <summary>
    /// AI 上下文生成器
    /// 收集预制体结构和框架指南，同步到 .github/copilot-instructions.md
    /// </summary>
    public static class AIContextGenerator
    {
        private const string AI_CONTEXT_DIR = ".ai-context";
        private const string COPILOT_INSTRUCTIONS_PATH = ".github/copilot-instructions.md";
        private const string TEMPLATE_GUIDE_PATH = "Packages/com.parful.unihper/UNIHPER_GUIDE.md";

        #region Menu Items

        /// <summary>
        /// Generate AI context (Shortcut: Alt+Q)
        /// </summary>
        [MenuItem("UNIHper/AI Copilot/Generate Context &q", priority = 11)]
        public static void GenerateAIContext()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Generate AI Context", "Exporting prefabs...", 0.3f);

                // 1. 导出所有预制体
                PrefabStructureExporter.ExportAllPrefabs();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Collecting context...", 0.6f);

                // 2. 收集所有上下文
                var contextData = CollectAllContext();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Writing instructions...", 0.9f);

                // 3. 生成并写入 copilot-instructions.md
                GenerateCopilotInstructions(contextData);

                Debug.Log($"[AI Copilot] Context generated: {contextData.PrefabCount} prefabs collected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI Copilot] Generation failed: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("UNIHper/AI Copilot/Open Context Folder", priority = 12)]
        public static void OpenAIContextDirectory()
        {
            var path = GetProjectPath(AI_CONTEXT_DIR);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("UNIHper/AI Copilot/Open Copilot Instructions", priority = 13)]
        public static void OpenCopilotInstructions()
        {
            var path = GetProjectPath(COPILOT_INSTRUCTIONS_PATH);
            if (File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else
            {
                Debug.LogWarning("[AI Copilot] copilot-instructions.md not found. Generate context first (Alt+Q)");
            }
        }

        #endregion

        #region Context Collection

        /// <summary>
        /// 收集所有 AI 上下文数据
        /// </summary>
        private static AIContextData CollectAllContext()
        {
            var data = new AIContextData();

            // 直接读取框架内的指南模板
            var templatePath = Path.GetFullPath(TEMPLATE_GUIDE_PATH);
            if (File.Exists(templatePath))
            {
                data.UnihperGuide = File.ReadAllText(templatePath, Encoding.UTF8);
            }

            // 收集预制体结构
            var prefabsPath = Path.Combine(GetProjectPath(AI_CONTEXT_DIR), "prefabs");
            if (Directory.Exists(prefabsPath))
            {
                var mdFiles = Directory.GetFiles(prefabsPath, "*.md").Where(f => !Path.GetFileName(f).StartsWith("_")).ToList();

                data.PrefabCount = mdFiles.Count;

                // 只收集预制体索引和摘要，完整内容太长
                var indexPath = Path.Combine(prefabsPath, "_index.md");
                if (File.Exists(indexPath))
                {
                    data.PrefabIndex = File.ReadAllText(indexPath, Encoding.UTF8);
                }

                // 收集每个预制体的层级树摘要（不含详情）
                foreach (var file in mdFiles)
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    var summary = ExtractPrefabSummary(content);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        data.PrefabSummaries.Add(Path.GetFileNameWithoutExtension(file), summary);
                    }
                }
            }

            // 收集项目脚本结构
            data.ScriptStructure = CollectScriptStructure();

            return data;
        }

        /// <summary>
        /// 提取预制体的摘要（统计信息 + 层级树 + 关键路径）
        /// </summary>
        private static string ExtractPrefabSummary(string fullContent)
        {
            var sb = new StringBuilder();
            var lines = fullContent.Split('\n');
            var inSection = false;
            var currentSection = "";
            var sectionsToInclude = new HashSet<string> { "## 📊 统计信息", "## 🌲 层级结构", "## 🔗 关键节点路径" };

            foreach (var line in lines)
            {
                // 检测标题
                if (line.StartsWith("# "))
                {
                    sb.AppendLine(line);
                    continue;
                }

                // 检测预制体路径
                if (line.StartsWith("> 预制体路径:"))
                {
                    sb.AppendLine(line);
                    sb.AppendLine();
                    continue;
                }

                // 检测章节
                if (line.StartsWith("## "))
                {
                    if (sectionsToInclude.Contains(line.Trim()))
                    {
                        inSection = true;
                        currentSection = line.Trim();
                        sb.AppendLine(line);
                    }
                    else
                    {
                        inSection = false;
                    }
                    continue;
                }

                // 在需要的章节中，收集内容
                if (inSection)
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// 收集项目脚本结构
        /// </summary>
        private static string CollectScriptStructure()
        {
            var sb = new StringBuilder();
            var scriptsPath = "Assets/Develop/Scripts";

            if (!AssetDatabase.IsValidFolder(scriptsPath))
            {
                return "";
            }

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { scriptsPath });

            // 分类收集
            var uiScripts = new List<string>();
            var sceneScripts = new List<string>();
            var configScripts = new List<string>();
            var otherScripts = new List<string>();

            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName.EndsWith("UI"))
                    uiScripts.Add(fileName);
                else if (fileName.StartsWith("Scene") && fileName.EndsWith("Script"))
                    sceneScripts.Add(fileName);
                else if (fileName.EndsWith("Config"))
                    configScripts.Add(fileName);
                else
                    otherScripts.Add(fileName);
            }

            if (uiScripts.Count > 0)
            {
                sb.AppendLine("### UI 脚本");
                foreach (var s in uiScripts.OrderBy(x => x))
                    sb.AppendLine($"- `{s}`");
                sb.AppendLine();
            }

            if (sceneScripts.Count > 0)
            {
                sb.AppendLine("### 场景脚本");
                foreach (var s in sceneScripts.OrderBy(x => x))
                    sb.AppendLine($"- `{s}`");
                sb.AppendLine();
            }

            if (configScripts.Count > 0)
            {
                sb.AppendLine("### 配置类");
                foreach (var s in configScripts.OrderBy(x => x))
                    sb.AppendLine($"- `{s}`");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region Copilot Instructions 生成

        /// <summary>
        /// 生成 copilot-instructions.md
        /// </summary>
        private static void GenerateCopilotInstructions(AIContextData data)
        {
            var sb = new StringBuilder();

            // 头部
            sb.AppendLine("# Copilot Instructions for " + Application.productName);
            sb.AppendLine();
            sb.AppendLine("> ⚠️ 此文件由 UNIHper AI 上下文生成器自动生成，请勿手动编辑");
            sb.AppendLine($"> 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 项目概述
            sb.AppendLine("## Project Overview");
            sb.AppendLine();
            sb.AppendLine($"Unity {Application.unityVersion} 项目，使用 UNIHper 框架开发。");
            sb.AppendLine();

            // UNIHper 指南（精简版）
            if (!string.IsNullOrEmpty(data.UnihperGuide))
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine(data.UnihperGuide);
                sb.AppendLine();
            }

            // 项目脚本结构
            if (!string.IsNullOrEmpty(data.ScriptStructure))
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 项目脚本");
                sb.AppendLine();
                sb.AppendLine(data.ScriptStructure);
            }

            // 预制体索引
            if (!string.IsNullOrEmpty(data.PrefabIndex))
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## UI 预制体");
                sb.AppendLine();
                sb.AppendLine($"项目包含 {data.PrefabCount} 个 UI 预制体，完整结构见 `.ai-context/prefabs/` 目录。");
                sb.AppendLine();

                // 添加预制体摘要
                if (data.PrefabSummaries.Count > 0)
                {
                    sb.AppendLine("### 预制体结构概览");
                    sb.AppendLine();

                    foreach (var kvp in data.PrefabSummaries.OrderBy(x => x.Key))
                    {
                        sb.AppendLine($"<details>");
                        sb.AppendLine($"<summary>{kvp.Key}</summary>");
                        sb.AppendLine();
                        sb.AppendLine(kvp.Value);
                        sb.AppendLine();
                        sb.AppendLine("</details>");
                        sb.AppendLine();
                    }
                }
            }

            // 写入文件
            var outputPath = GetProjectPath(COPILOT_INSTRUCTIONS_PATH);
            var outputDir = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[AIContextGenerator] 已更新: {outputPath}");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取项目根目录下的路径
        /// </summary>
        private static string GetProjectPath(string relativePath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, relativePath);
        }

        #endregion

        #region 数据结构

        private class AIContextData
        {
            public string UnihperGuide { get; set; } = "";
            public string PrefabIndex { get; set; } = "";
            public int PrefabCount { get; set; }
            public Dictionary<string, string> PrefabSummaries { get; } = new Dictionary<string, string>();
            public string ScriptStructure { get; set; } = "";
        }

        #endregion
    }
}
