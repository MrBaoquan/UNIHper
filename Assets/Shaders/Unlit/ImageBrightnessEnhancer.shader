Shader "UNIHper/Unlit/ImageBrightnessEnhancer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}             // 主纹理
        _BrightnessMultiplier ("Brightness Multiplier", Range(1.0, 100.0)) = 1.0 // 亮度比例提升参数
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float _BrightnessMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 获取纹理颜色
                fixed4 color = tex2D(_MainTex, i.uv);

                // 计算颜色的亮度
                float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));

                // 对亮度进行等比提升，同时确保亮度为 0 的区域保持为 0
                float newLuminance = luminance * _BrightnessMultiplier;

                // 使用比例提升后的亮度重新调整颜色
                if (luminance > 0.0)
                {
                    color.rgb *= newLuminance / luminance;
                }

                // 确保颜色值在合法范围
                color.rgb = saturate(color.rgb);

                return color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
