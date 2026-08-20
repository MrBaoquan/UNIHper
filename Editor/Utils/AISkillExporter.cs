using System.IO;
using UnityEditor;
using UnityEngine;

namespace UNIHper.Editor
{
    // 极简技能导出器：手动一次性把包内框架技能复制到项目 .github/skills/，导出后归项目所有
    public static class AISkillExporter
    {
        private const string FrameworkSkillsPath = "Packages/com.parful.unihper/Editor/Skills";
        private const string ProjectSkillsDir = ".github/skills";
        private const string UserMaintainedSkill = "project-memory";

        [MenuItem("UNIHper/AI Copilot/Export Skills to Project", priority = 11)]
        public static void ExportSkills()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var sourceDir = Path.Combine(projectRoot, FrameworkSkillsPath);
            var targetDir = Path.Combine(projectRoot, ProjectSkillsDir);

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning($"[AI Copilot] Skills source not found: {FrameworkSkillsPath}");
                return;
            }
            Directory.CreateDirectory(targetDir);

            var exported = 0;
            foreach (var skillDir in Directory.GetDirectories(sourceDir))
            {
                var skillFile = Path.Combine(skillDir, "SKILL.md");
                if (!File.Exists(skillFile))
                    continue;

                var skillName = Path.GetFileName(skillDir);
                var destFile = Path.Combine(targetDir, skillName, "SKILL.md");
                if (skillName == UserMaintainedSkill && File.Exists(destFile))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(skillFile, destFile, overwrite: true);
                exported++;
            }

            Debug.Log($"[AI Copilot] Exported {exported} skills to {ProjectSkillsDir}. Skills are project-owned after export.");
        }
    }
}
