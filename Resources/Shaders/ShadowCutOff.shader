/*
 *
 *  针对对AlphaMask 进行像素裁剪
 *
 */

Shader "UNIHper/ShadowCutOff"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMask ("Alpha Mask", 2D) = "white" {}

        [Enum(LessThan,0,GreaterThan,1)] _Compare("Compare", Float) = 1.0
        _Threshold("Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _AlphaMask;
            fixed4 _Color;

            float _Threshold;
            float _Compare;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // put shadows data into TEXCOORD1
                float2 r_uv:TEXCOORD2;
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);// v.texcoord;
                o.r_uv = v.texcoord;

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal,1));
                // compute shadows data
                TRANSFER_SHADOW(o)
                return o;
            }



            fixed4 frag (v2f i) : SV_Target
            {

                fixed4 _alpha = tex2D(_AlphaMask,i.r_uv);
                float _clip = (_Threshold - _alpha.a)*(_Compare-0.5)*2;
                clip(_clip);

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = i.diff * shadow + i.ambient;
                //fixed3 lighting = fixed3(1,1,1);

                col.rgb *= lighting;
                return col;
            }
            ENDCG
        }

                // shadow caster rendering pass, implemented manually
        // using macros from UnityCG.cginc
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            sampler2D _AlphaMask;
            float _Threshold;
            float _Compare;

            struct v2f {
                float2 uv : TEXCOORD0;
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                fixed4 _alpha = tex2D(_AlphaMask,i.uv);
                float _clip = (_Threshold - _alpha.a)*(_Compare-0.5)*2;
                clip(_clip);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }

        // shadow casting support
        //UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}