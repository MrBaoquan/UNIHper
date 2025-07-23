Shader "UNIHper/Unlit/ThresholdToColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // 主纹理
        _Threshold ("Brightness Threshold", Range(0, 1)) = 0.5 // 亮度阈值
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1) // 高亮颜色
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 引用 Unity 的 Shader 库
            #include "UnityCG.cginc"

            // 属性声明
            sampler2D _MainTex;
            float _Threshold;
            fixed4 _HighlightColor;

            // 顶点着色器
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 片段着色器
            fixed4 frag (v2f i) : SV_Target
            {
                // 读取主纹理颜色
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 计算亮度 (基于人眼感知的加权值)
                float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // 如果亮度超过阈值，将颜色设为高亮颜色
                if (brightness > _Threshold)
                {
                    return fixed4(_HighlightColor.rgb, texColor.a); // 使用高亮颜色
                }

                // 否则保持原颜色
                return texColor;
            }
            ENDCG
        }
    }
}
