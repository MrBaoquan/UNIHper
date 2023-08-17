Shader "UNIHper/Unlit/FadeAB"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(Off,On)] _Enable("Enable Fade",Integer) = 1
		_ATexture ("Src Texture", 2D) = "white" {}
		_BTexture ("Dst Texture", 2D) = "white" {}
		_Weight ("Weight", Range(0,1)) = 0
        
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200
        Pass
        {
            CGPROGRAM
            #pragma fragment frag
            #pragma vertex vert

            #include "UnityCG.cginc"

            fixed4 _Color;
            int _Enable;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _ATexture;
            sampler2D _BTexture;
            
            half _Weight;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
                float4 color:COLOR;
            };

            struct v2f
            {
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
                float4 color:COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {                
                if(_Enable==0){
                    return tex2D(_MainTex,i.uv) * i.color;
                }

                fixed4 _colorA = tex2D(_ATexture,i.uv);
                fixed4 _colorB = tex2D(_BTexture,i.uv);
                fixed4 _final = _colorA * (1-_Weight) + _colorB * _Weight;
                return _final * i.color;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
