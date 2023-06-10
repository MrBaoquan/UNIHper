Shader "UNIHper/ARShadow"
{
   Properties
	{
		_ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 0.53)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="AlphaTest"}
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Tags { "LightMode"="ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc" 

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				SHADOW_COORDS(2)
			};


			fixed4 _ShadowColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed atten = SHADOW_ATTENUATION(i);
				return fixed4(_ShadowColor.rgb,saturate(1 - atten)*_ShadowColor.a);
			}
			ENDCG
		}


         Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

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
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
	}

}
