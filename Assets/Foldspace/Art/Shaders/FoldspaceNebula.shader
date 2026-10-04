Shader "Foldspace/Nebula"
{
    Properties
    {
        _Deep ("Deep space", Color) = (0.02, 0.024, 0.06, 1)
        _ColorA ("Cloud A", Color) = (0.18, 0.07, 0.38, 1)
        _ColorB ("Cloud B", Color) = (0.03, 0.18, 0.38, 1)
        _ColorC ("Highlight", Color) = (0.5, 0.36, 0.95, 1)
        _Scale ("Scale", Float) = 0.11
        _Parallax ("Parallax", Float) = 0.15
        _Drift ("Drift", Vector) = (0.012, 0.005, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _Deep, _ColorA, _ColorB, _ColorC;
            float _Scale, _Parallax;
            float4 _Drift;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 p : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                float2 world = mul(unity_ObjectToWorld, vertex).xy;
                float2 cam = _WorldSpaceCameraPos.xy;
                o.p = (world - cam + cam * _Parallax) * _Scale + _Drift.xy * _Time.y;
                return o;
            }

            float hash (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm (float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                const float2x2 m = float2x2(1.6, 1.2, -1.2, 1.6);
                for (int k = 0; k < 5; k++)
                {
                    v += a * noise(p);
                    p = mul(m, p);
                    a *= 0.5;
                }
                return v;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 p = i.p;
                float2 q = float2(fbm(p), fbm(p + float2(5.2, 1.3)));
                float n = fbm(p + 1.8 * q + float2(_Time.y * 0.008, 0));
                float m = fbm(p * 1.7 + 4.0 * q.yx + 9.1);

                half3 c = _Deep.rgb;
                c = lerp(c, _ColorA.rgb, smoothstep(0.32, 0.82, n) * 0.9);
                c = lerp(c, _ColorB.rgb, smoothstep(0.4, 0.9, m) * 0.8);
                c += _ColorC.rgb * pow(saturate(n * m * 1.8 - 0.25), 3.0) * 0.9;
                return half4(c, 1);
            }
            ENDCG
        }
    }
}
