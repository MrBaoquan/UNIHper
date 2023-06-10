Shader "UNIHper/Unlit/FadeMB"
{
    Properties
    {
		_MainTex ("Main Texture", 2D) = "white" {}
		_BTexture ("Dst Texture", 2D) = "white" {}
		_Weight ("Weight", Range(0,1)) = 0
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma fragment frag
            #pragma vertex vert

            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _BTexture;
            half _Weight;
            half _Alpha;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 _colorA = tex2D(_MainTex,i.uv);
                fixed4 _colorB = tex2D(_BTexture,i.uv);
                fixed4 _final = _colorA*(1-_Weight)+_colorB*_Weight;
                _final.a *= _Alpha;
                return _final;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
