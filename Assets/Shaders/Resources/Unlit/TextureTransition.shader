Shader "UNIHper/Unlit/TextureTransition"
{
    Properties
    {
        _FromTex("From Texture", 2D) = "white" {}
        _ToTex("To Texture", 2D) = "white" {}
        _Fade("Transition Progress", Range(0,1)) = 0
        _TransitionType("Transition Type", Int) = 1
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // 透明混合

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			
            sampler2D _FromTex;
            sampler2D _ToTex;
            float _Fade;
            int _TransitionType;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
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
                fixed4 colFrom = tex2D(_FromTex, i.uv);
                fixed4 colTo = tex2D(_ToTex, i.uv);
                float t = _Fade;

                switch(_TransitionType)
                {
                    case 0: // 无过渡(直接显示目标)
                        return colTo;

                    case 1: // 淡入淡出
                        return lerp(colFrom, colTo, t);

                    case 2: // 黑色过渡
                        if(t < 0.5)
                        {
                            return lerp(colFrom, fixed4(0,0,0,1), t * 2.0);
                        }
                        else
                        {
                            return lerp(fixed4(0,0,0,1), colTo, 2.0 * (t - 0.5));
                        }

                    case 3: // 白色过渡
                        if(t < 0.5)
                        {
                            return lerp(colFrom, fixed4(1,1,1,1), t * 2.0);
                        }
                        else
                        {
                            return lerp(fixed4(1,1,1,1), colTo, 2.0 * (t - 0.5));
                        }

                    case 4: // 水平切换
                        {
                            float threshold = t;
                            float blendT = step(i.uv.x, threshold);
                            return lerp(colFrom, colTo, blendT);
                        }

                    case 5: // 垂直切换
                        {
                            float threshold = t;
                            float blendT = step(i.uv.y, threshold);
                            return lerp(colFrom, colTo, blendT);
                        }

                    case 6: // 圆形扩展
                        {
                            float2 center = float2(0.5, 0.5);
                            float dist = distance(i.uv, center);
                            float radius = t * 0.7; // 调节最大半径
                            float blendT = step(dist, radius);
                            return lerp(colFrom, colTo, blendT);
                        }

                    case 7: // 缩放渐变
                        {
                            float scaleFrom = lerp(1.0, 0.2, t);
                            float scaleTo = lerp(0.2, 1.0, t);
                            float2 uvFrom = (i.uv - 0.5)/scaleFrom + 0.5;
                            float2 uvTo = (i.uv - 0.5)/scaleTo + 0.5;

                            fixed4 cFrom = (uvFrom.x < 0 || uvFrom.x >1 || uvFrom.y <0 || uvFrom.y >1) ? fixed4(0,0,0,0) : tex2D(_FromTex, uvFrom);
                            fixed4 cTo = (uvTo.x <0 || uvTo.x >1 || uvTo.y <0 || uvTo.y >1) ? fixed4(0,0,0,0) : tex2D(_ToTex, uvTo);
                            
                            float blendT = smoothstep(0.4, 1.0, t);
                            return lerp(cFrom, cTo, blendT);
                        }

                    default: // 默认淡入淡出
                        return lerp(colFrom, colTo, t);
                }
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
