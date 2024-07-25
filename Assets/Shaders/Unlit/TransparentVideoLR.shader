Shader "UNIHper/Unlit/TransparentTextureLR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 colorUV = i.uv;
                float2 alphaUV = i.uv;
                colorUV.x *= 0.5; // 左半部分
                alphaUV.x = 0.5 + alphaUV.x * 0.5; // 右半部分

                fixed4 color = tex2D(_MainTex, colorUV);
                fixed4 alpha = tex2D(_MainTex, alphaUV);

                color.a = alpha.r; // 使用右半部分的红色通道作为alpha值
                return color;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}
