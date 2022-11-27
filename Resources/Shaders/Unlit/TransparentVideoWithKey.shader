Shader "UNIHper/Unlit/TransparentVideoWithKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Alpha ("Video Alpha", Range(0,1)) = 1

        _thresh ("Threshold", Range (0, 16)) = 0.8
        _slope ("Slope", Range (0, 1)) = 0.2
        _keyingColor ("Key Colour", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Alpha;
            
            float3 _keyingColor;
            float _thresh; // 0.8
            float _slope; // 0.2

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float3 input_color = tex2D(_MainTex,i.uv).rgb;
                float d = abs(length(abs(_keyingColor.rgb - input_color.rgb)));
                float edge0 = _thresh*(1 - _slope);
                float _alpha = smoothstep(edge0,_thresh,d);

                _alpha *= _Alpha;
                // apply fog
                return fixed4(input_color, _alpha);
            }
            ENDCG
        }
    }
}
