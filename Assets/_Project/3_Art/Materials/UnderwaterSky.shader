Shader "Skybox/UnderwaterGradient"
{
    Properties
    {
        _TopColor ("Top (surface)", Color) = (0.6, 0.9, 1, 1)
        _HorizonColor ("Horizon", Color) = (0.18, 0.56, 1, 1)
        _BottomColor ("Bottom (abyss)", Color) = (0.02, 0.1, 0.25, 1)
        _TopExponent ("Top Falloff", Range(0.1, 4)) = 0.7
        _BottomExponent ("Bottom Falloff", Range(0.1, 4)) = 0.5
        _SunDir ("Sun Direction", Vector) = (0, 0.6, 0.8, 0)
        _SunColor ("Sun Color", Color) = (1, 1, 1, 1)
        _SunSize ("Sun Tightness", Range(1, 256)) = 20
        _SunStrength ("Sun Strength", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            float _TopExponent, _BottomExponent, _SunSize, _SunStrength;
            float4 _SunDir;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 col = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(d.y), _TopExponent));
                col = lerp(col, _BottomColor.rgb, pow(saturate(-d.y), _BottomExponent));

                float sun = pow(saturate(dot(d, normalize(_SunDir.xyz))), _SunSize);
                col += _SunColor.rgb * sun * _SunStrength;

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}