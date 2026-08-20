using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UNIHper.Editor
{
    /// <summary>
    /// AI 上下文生成器
    /// 管理 Agent Skills 的同步与生成：
    /// 1. 框架 Skills —— 随 UNIHper 版本自动同步
    /// 2. UGUI 预制体 Skill —— 快捷键触发生成
    /// 3. 项目记忆 Skill —— 自更新的项目知识库
    /// 4. 用户自定义 Skills —— 用户自行维护
    /// </summary>
    public static class AIContextGenerator
    {
        #region Constants

        private const string AI_CONTEXT_DIR = ".ai-context";
        private const string COPILOT_INSTRUCTIONS_PATH = ".github/copilot-instructions.md";
        private const string SKILLS_DIR = ".github/skills";
        private const string MANAGED_SKILLS_MANIFEST = ".github/skills/.managed-skills.json";
        private const string FRAMEWORK_SKILLS_PATH = "Packages/com.parful.unihper/Editor/Skills";
        private const string PACKAGE_JSON_PATH = "Packages/com.parful.unihper/package.json";
        private const string PROJECT_MEMORY_SKILL = "project-memory";
        private const string UGUI_PREFABS_SKILL = "ugui-prefabs";

        #endregion

        #region Auto Sync on Domain Reload

        /// <summary>
        /// Unity 域重载时自动检查并同步框架 Skills
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    CheckAndAutoSyncFrameworkSkills();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AI Copilot] Auto-sync check failed: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// 检查 UNIHper 版本是否变化，若变化则自动同步框架 Skills
        /// </summary>
        private static void CheckAndAutoSyncFrameworkSkills()
        {
            var manifest = LoadManifest();
            var currentVersion = GetPackageVersion();

            if (manifest == null || manifest.unihperVersion != currentVersion)
            {
                SyncFrameworkSkills();
                Debug.Log($"[AI Copilot] Framework skills auto-synced to v{currentVersion}");
            }
        }

        #endregion

        #region Menu Items

        /// <summary>
        /// 生成完整 AI 上下文 (Shortcut: Alt+Q)
        /// 同步框架 Skills + 生成预制体 Skill + 生成 copilot-instructions.md
        /// </summary>
        [MenuItem("UNIHper/AI Copilot/Generate Context &q", priority = 11)]
        public static void GenerateAIContext()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Generate AI Context", "Syncing framework skills...", 0.1f);
                SyncFrameworkSkills();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Exporting prefabs...", 0.3f);
                PrefabStructureExporter.ExportAllPrefabs();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Generating prefab skill...", 0.5f);
                GeneratePrefabSkill();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Collecting context...", 0.7f);
                var contextData = CollectAllContext();

                EditorUtility.DisplayProgressBar("Generate AI Context", "Writing instructions...", 0.9f);
                GenerateCopilotInstructions(contextData);

                Debug.Log(
                    $"[AI Copilot] Context generated: {contextData.PrefabCount} prefabs, framework skills synced (v{GetPackageVersion()})"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI Copilot] Generation failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("UNIHper/AI Copilot/Open Skills Folder", priority = 12)]
        public static void OpenSkillsDirectory()
        {
            var path = GetProjectPath(SKILLS_DIR);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        #endregion

        #region Framework Skills Sync

        /// <summary>
        /// 从 UNIHper 包中同步框架 Skills 到项目 .github/skills/ 目录
        /// </summary>
        private static void SyncFrameworkSkills()
        {
            var frameworkSkillsSource = GetProjectPath(FRAMEWORK_SKILLS_PATH);
            var projectSkillsDir = GetProjectPath(SKILLS_DIR);
            var currentVersion = GetPackageVersion();

            if (!Directory.Exists(frameworkSkillsSource))
            {
                Debug.LogWarning($"[AI Copilot] Framework skills source not found: {frameworkSkillsSource}");
                return;
            }

            if (!Directory.Exists(projectSkillsDir))
                Directory.CreateDirectory(projectSkillsDir);

            var managedSkills = new List<string>();

            // 遍历框架 Skills 目录
            foreach (var skillDir in Directory.GetDirectories(frameworkSkillsSource))
            {
                var skillName = Path.GetFileName(skillDir);
                var sourceSkillFile = Path.Combine(skillDir, "SKILL.md");
                if (!File.Exists(sourceSkillFile))
                    continue;

                var targetDir = Path.Combine(projectSkillsDir, skillName);
                var targetSkillFile = Path.Combine(targetDir, "SKILL.md");

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // project-memory 仅首次播种，不覆盖用户已有内容
                if (skillName == PROJECT_MEMORY_SKILL && File.Exists(targetSkillFile))
                {
                    managedSkills.Add(skillName);
                    continue;
                }

                // 复制 SKILL.md
                File.Copy(sourceSkillFile, targetSkillFile, overwrite: true);

                // 复制子目录 (references/, scripts/, templates/)
                CopySubdirectory(skillDir, targetDir, "references");
                CopySubdirectory(skillDir, targetDir, "scripts");
                CopySubdirectory(skillDir, targetDir, "templates");

                managedSkills.Add(skillName);
            }

            // 保存清单
            var manifest = LoadManifest() ?? new ManagedSkillsManifest();
            manifest.unihperVersion = currentVersion;
            manifest.syncedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            manifest.managedSkills = managedSkills;
            SaveManifest(manifest);
        }

        /// <summary>
        /// 复制子目录（如 references/, scripts/, templates/）
        /// </summary>
        private static void CopySubdirectory(string sourceParent, string targetParent, string subDirName)
        {
            var sourceDir = Path.Combine(sourceParent, subDirName);
            if (!Directory.Exists(sourceDir))
                return;

            var targetDir = Path.Combine(targetParent, subDirName);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite: true);
            }
        }

        #endregion

        #region Prefab Skill Generation

        /// <summary>
        /// 从预制体导出数据生成 ugui-prefabs Agent Skill
        /// </summary>
        private static void GeneratePrefabSkill()
        {
            var prefabsPath = Path.Combine(GetProjectPath(AI_CONTEXT_DIR), "prefabs");
            if (!Directory.Exists(prefabsPath))
                return;

            var mdFiles = Directory.GetFiles(prefabsPath, "*.md").Where(f => !Path.GetFileName(f).StartsWith("_")).ToList();

            if (mdFiles.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("name: ugui-prefabs");
            sb.AppendLine(
                $"description: 'UGUI prefab hierarchy and component paths for {mdFiles.Count} UI prefabs in this project. Contains node trees, component types, and transform.Find() paths for Get<T>() usage.'"
            );
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"# UGUI Prefab Structure ({mdFiles.Count} prefabs)");
            sb.AppendLine();
            sb.AppendLine("> Auto-generated from UI prefab assets. Use these paths with UNIHper `Get<T>(\"path\")` method.");
            sb.AppendLine();

            foreach (var file in mdFiles.OrderBy(f => f))
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var summary = ExtractPrefabSummary(content);
                if (!string.IsNullOrEmpty(summary))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    sb.AppendLine($"## {fileName}");
                    sb.AppendLine();
                    sb.AppendLine(summary);
                    sb.AppendLine();
                }
            }

            // 写入 skill 文件
            var skillDir = Path.Combine(GetProjectPath(SKILLS_DIR), UGUI_PREFABS_SKILL);
            if (!Directory.Exists(skillDir))
                Directory.CreateDirectory(skillDir);

            File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), sb.ToString(), Encoding.UTF8);

            // 更新清单
            var manifest = LoadManifest() ?? new ManagedSkillsManifest();
            if (!manifest.generatedSkills.Contains(UGUI_PREFABS_SKILL))
                manifest.generatedSkills.Add(UGUI_PREFABS_SKILL);
            SaveManifest(manifest);
        }

        #endregion

        #region Project Memory Helpers

        /// <summary>
        /// 从 project-memory SKILL.md 中提取 ## Coding Style 区块内容
        /// 用于自动注入 copilot-instructions.md
        /// </summary>
        private static string ExtractCodingStyleFromProjectMemory()
        {
            var skillFile = Path.Combine(GetProjectPath(SKILLS_DIR), PROJECT_MEMORY_SKILL, "SKILL.md");
            if (!File.Exists(skillFile))
                return null;

            var content = File.ReadAllText(skillFile, Encoding.UTF8);

            // 匹配 ## Coding Style 区块（到下一个同级标题或文件末尾）
            var match = Regex.Match(
                content,
                @"^## Coding Style\s*\n(.*?)(?=^## [^#]|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline
            );

            if (!match.Success)
                return null;

            var body = match.Groups[1].Value.Trim();
            return string.IsNullOrWhiteSpace(body) ? null : body;
        }

        #endregion

        #region Context Collection

        /// <summary>
        /// 收集所有 AI 上下文数据
        /// </summary>
        private static AIContextData CollectAllContext()
        {
            var data = new AIContextData();

            // 收集预制体结构
            var prefabsPath = Path.Combine(GetProjectPath(AI_CONTEXT_DIR), "prefabs");
            if (Directory.Exists(prefabsPath))
            {
                var mdFiles = Directory.GetFiles(prefabsPath, "*.md").Where(f => !Path.GetFileName(f).StartsWith("_")).ToList();

                data.PrefabCount = mdFiles.Count;

                foreach (var file in mdFiles)
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    var summary = ExtractPrefabSummary(content);
                    if (!string.IsNullOrEmpty(summary))
                        data.PrefabSummaries.Add(Path.GetFileNameWithoutExtension(file), summary);
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
            var sectionsToInclude = new HashSet<string> { "## 📊 统计信息", "## 🌲 层级结构", "## 🔗 关键节点路径" };

            foreach (var line in lines)
            {
                if (line.StartsWith("# "))
                {
                    sb.AppendLine(line);
                    continue;
                }

                if (line.StartsWith("> 预制体路径:"))
                {
                    sb.AppendLine(line);
                    sb.AppendLine();
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    inSection = sectionsToInclude.Contains(line.Trim());
                    if (inSection)
                        sb.AppendLine(line);
                    continue;
                }

                if (inSection)
                    sb.AppendLine(line);
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
                return "";

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { scriptsPath });

            var uiScripts = new List<string>();
            var sceneScripts = new List<string>();
            var configScripts = new List<string>();

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

        #region Copilot Instructions Generation

        /// <summary>
        /// 生成精简的 copilot-instructions.md
        /// 仅保留项目身份层与全局硬约束
        /// </summary>
        private static void GenerateCopilotInstructions(AIContextData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Copilot Instructions for " + Application.productName);
            sb.AppendLine();
            sb.AppendLine("## Project Identity");
            sb.AppendLine();
            sb.AppendLine($"- Unity {Application.unityVersion} project built on UNIHper.");
            sb.AppendLine("- Default implementation stance: solve with UNIHper first, not with raw Unity patterns first.");
            sb.AppendLine("- Treat UNIHper Editor templates and framework base classes as the default skeleton source for new code.");
            sb.AppendLine();

            sb.AppendLine("## Constraint Order");
            sb.AppendLine();
            sb.AppendLine("1. Project identity and hard defaults from this file.");
            sb.AppendLine("2. UNIHper framework and file-scoped instructions under `.github/instructions/`.");
            sb.AppendLine("3. Domain skills under `.github/skills/`.");
            sb.AppendLine("4. Project-specific deltas from `project-memory`.");
            sb.AppendLine();
            sb.AppendLine(
                "If create-skill guidance conflicts with editor templates or base-class contracts, follow the editor template and the actual framework API, then update the skill to match."
            );
            sb.AppendLine();

            sb.AppendLine("## Global Defaults");
            sb.AppendLine();
            sb.AppendLine("- Prefer `Managements.*` facades when an equivalent framework capability exists.");
            sb.AppendLine("- Prefer UniRx and `IObservable<T>` for async flow, event handling, and UI bindings.");
            sb.AppendLine("- Prefer `Managements.Timer` for delay, interval, countdown, throttle, and debounce.");
            sb.AppendLine("- Prefer `Managements.Event` for cross-component communication.");
            sb.AppendLine("- All `Subscribe` calls must be lifecycle-managed.");
            sb.AppendLine();

            sb.AppendLine("## Avoid By Default");
            sb.AppendLine();
            sb.AppendLine("- Do not introduce `IEnumerator` / `StartCoroutine` unless Unity API usage requires it.");
            sb.AppendLine("- Do not introduce polling-style `Update()` logic unless the behavior is truly frame-driven.");
            sb.AppendLine("- Do not introduce `UnityEvent` or `SendMessage` for project logic.");
            sb.AppendLine();

            sb.AppendLine("## Naming");
            sb.AppendLine();
            sb.AppendLine("- UGUI page: `{Feature}UI.cs`");
            sb.AppendLine("- UI Toolkit page: `{Feature}ToolkitUI.cs`");
            sb.AppendLine("- Scene script: `Scene{Name}Script.cs`");
            sb.AppendLine("- Config: `{Feature}Config.cs`");
            sb.AppendLine("- Event: `{Action}Event.cs`");
            sb.AppendLine();

            sb.AppendLine("## Layer Boundaries");
            sb.AppendLine();
            sb.AppendLine("- Keep always-on rules and decision defaults in `copilot-instructions` and `.github/instructions/`.");
            sb.AppendLine("- Keep task-specific framework usage, examples, and creation workflows in `.github/skills/`.");
            sb.AppendLine("- Keep project-only exceptions and verified corrections in `project-memory`.");
            sb.AppendLine("- Keep Editor templates and framework base-class contracts as the final implementation source of truth.");

            // 写入文件
            var outputPath = GetProjectPath(COPILOT_INSTRUCTIONS_PATH);
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 列出 Skills 及其描述
        /// include 为具体列表时只查找这些 skill；为 null 时扫描全部并排除 exclude 中的
        /// </summary>
        private static Dictionary<string, string> ListSkillsWithDescriptions(List<string> include = null, HashSet<string> exclude = null)
        {
            var result = new Dictionary<string, string>();
            var skillsDir = GetProjectPath(SKILLS_DIR);
            if (!Directory.Exists(skillsDir))
                return result;

            IEnumerable<string> dirs;
            if (include != null)
            {
                dirs = include.Select(name => Path.Combine(skillsDir, name)).Where(Directory.Exists);
            }
            else
            {
                dirs = Directory.GetDirectories(skillsDir);
                if (exclude != null)
                    dirs = dirs.Where(d => !exclude.Contains(Path.GetFileName(d)));
            }

            foreach (var dir in dirs.OrderBy(d => d))
            {
                var skillName = Path.GetFileName(dir);
                var skillFile = Path.Combine(dir, "SKILL.md");
                if (File.Exists(skillFile))
                {
                    var desc = ExtractSkillDescription(skillFile);
                    result[skillName] = desc;
                }
            }

            return result;
        }

        #endregion

        #region Manifest Management

        [Serializable]
        private class ManagedSkillsManifest
        {
            public string unihperVersion = "";
            public string syncedAt = "";
            public List<string> managedSkills = new List<string>();
            public List<string> generatedSkills = new List<string>();
        }

        private static ManagedSkillsManifest LoadManifest()
        {
            var path = GetProjectPath(MANAGED_SKILLS_MANIFEST);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<ManagedSkillsManifest>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveManifest(ManagedSkillsManifest manifest)
        {
            var path = GetProjectPath(MANAGED_SKILLS_MANIFEST);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        #endregion

        #region Utility

        /// <summary>
        /// 从 SKILL.md 的 YAML frontmatter 中提取 description
        /// </summary>
        private static string ExtractSkillDescription(string skillFilePath)
        {
            try
            {
                var lines = File.ReadAllLines(skillFilePath, Encoding.UTF8);
                bool inFrontmatter = false;
                foreach (var line in lines)
                {
                    if (line.Trim() == "---")
                    {
                        if (!inFrontmatter)
                        {
                            inFrontmatter = true;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (inFrontmatter && line.TrimStart().StartsWith("description:"))
                    {
                        var raw = line.Substring(line.IndexOf(':') + 1).Trim();
                        // 去掉单引号包裹
                        if (raw.StartsWith("'") && raw.EndsWith("'"))
                            raw = raw.Substring(1, raw.Length - 2);
                        return raw;
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 从 package.json 读取 UNIHper 版本号
        /// </summary>
        private static string GetPackageVersion()
        {
            var path = GetProjectPath(PACKAGE_JSON_PATH);
            if (!File.Exists(path))
                return "unknown";
            try
            {
                var content = File.ReadAllText(path, Encoding.UTF8);
                var match = Regex.Match(content, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// 获取项目根目录下的路径
        /// </summary>
        private static string GetProjectPath(string relativePath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, relativePath);
        }

        #endregion

        #region Data

        private class AIContextData
        {
            public int PrefabCount { get; set; }
            public Dictionary<string, string> PrefabSummaries { get; } = new Dictionary<string, string>();
            public string ScriptStructure { get; set; } = "";
        }

        #endregion
    }
}
