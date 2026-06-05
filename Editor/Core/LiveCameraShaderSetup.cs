using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UNIHper.Editor
{
    /// <summary>
    /// 自动将 AVProLiveCamera 所需的 Hidden Shader 加入 Always Included Shaders，
    /// 防止 Unity 打包时剥离 Hidden/ 前缀的 Shader 导致运行时 Shader.Find 返回 null。
    /// <para>时机：编辑器首次加载 / 脚本重编译后自动执行</para>
    /// </summary>
    [InitializeOnLoad]
    internal static class LiveCameraShaderSetup
    {
        /// <summary>
        /// AVProLiveCamera 运行时必须的 Shader 名称列表
        /// </summary>
        private static readonly string[] RequiredShaderNames = new[]
        {
            "Hidden/AVProLiveCamera/CompositeBGRA_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeMono8_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeYUY2_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeUYVY_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeYVYU_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeHDYC_2_RGBA",
            "Hidden/AVProLiveCamera/CompositeYUV_I420",
            "Hidden/AVProLiveCamera/CompositeYUV_YV12",
            "Hidden/AVProLiveCamera/Deinterlace",
        };

        static LiveCameraShaderSetup()
        {
            EnsureShadersIncluded();
        }

        [MenuItem("UNIHper/LiveCamera/Include Shaders in Build", false, 200)]
        private static void MenuIncludeShaders()
        {
            EnsureShadersIncluded();
            Debug.Log("[LiveCameraShaderSetup] 手动执行完成");
        }

        private static void EnsureShadersIncluded()
        {
            var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null)
            {
                Debug.LogWarning("[LiveCameraShaderSetup] 无法加载 GraphicsSettings");
                return;
            }

            var serializedObject = new SerializedObject(graphicsSettings);
            var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");

            // 收集已有的 Shader 名称（避免重复添加）
            var existingNames = new HashSet<string>();
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var shader = arrayProp.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null)
                    existingNames.Add(shader.name);
            }

            int addedCount = 0;
            foreach (var shaderName in RequiredShaderNames)
            {
                if (existingNames.Contains(shaderName))
                    continue;

                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[LiveCameraShaderSetup] 找不到 Shader: {shaderName}，请检查 AVProLiveCamera 插件是否完整");
                    continue;
                }

                int idx = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(idx);
                arrayProp.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
                addedCount++;
            }

            if (addedCount > 0)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[LiveCameraShaderSetup] 已将 {addedCount} 个 AVProLiveCamera Shader 加入 Always Included Shaders");
            }
        }
    }
}
