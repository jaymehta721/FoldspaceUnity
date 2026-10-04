Shader "Hidden/Foldspace/Post"
{
    Properties
    {
        _MainTex ("", 2D) = "black" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;

    float4 _Threshold;      // x: threshold, y: threshold - knee, z: 2 * knee, w: 0.25 / knee
    half4 _BloomTint;       // rgb: tint * intensity
    float4 _Waves[4];       // xy: center (viewport), z: radius (screen heights), w: strength
    float _Aspect;
    float _Aberration;
    half4 _Flash;           // rgb: color, a: amount
    half4 _Vignette;        // rgb: color, a: intensity
    float2 _VignetteRange;  // inner and outer radius in screen heights
    half4 _Damage;          // rgb: color, a: amount
    float4 _Grade;          // x: exposure, y: contrast, z: saturation, w: grain
    float _GrainSeed;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2f vert (appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    half3 SampleBox (float2 uv, float delta)
    {
        float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb
                + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half3 Prefilter (half3 c)
    {
        half brightness = max(c.r, max(c.g, c.b));
        half soft = clamp(brightness - _Threshold.y, 0, _Threshold.z);
        soft = soft * soft * _Threshold.w;
        half contribution = max(soft, brightness - _Threshold.x) / max(brightness, 0.00001);
        return c * contribution;
    }

    float Hash (float2 p)
    {
        p = frac(p * float2(443.897, 441.423));
        p += dot(p, p.yx + 19.19);
        return frac((p.x + p.y) * p.x);
    }
    ENDCG

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        // 0: bloom prefilter
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Prefilter(SampleBox(i.uv, 1)), 1); }
            ENDCG
        }

        // 1: downsample
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(SampleBox(i.uv, 1), 1); }
            ENDCG
        }

        // 2: upsample, added onto the larger level
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(SampleBox(i.uv, 0.5), 1); }
            ENDCG
        }

        // 3: composite
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 offset = 0;
                half waveLight = 0;
                for (int k = 0; k < 4; k++)
                {
                    float4 w = _Waves[k];
                    if (w.w == 0) continue;
                    float2 d = uv - w.xy;
                    d.x *= _Aspect;
                    float dist = length(d);
                    float x = (dist - w.z) / 0.04;
                    float ring = exp(-x * x);
                    float2 dir = d / max(dist, 0.0001);
                    dir.x /= _Aspect;
                    offset -= dir * ring * w.w * 0.03;
                    waveLight += ring * abs(w.w);
                }
                uv += offset;

                float2 fromCenter = uv - 0.5;
                float2 ca = fromCenter * _Aberration;
                half3 col;
                col.r = tex2D(_MainTex, uv - ca).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv + ca).b;

                float2 bloomUV = uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) bloomUV.y = 1 - bloomUV.y;
                #endif
                col += tex2D(_BloomTex, bloomUV).rgb * _BloomTint.rgb;
                col += waveLight * 0.05;

                col *= _Grade.x;
                col = (col - 0.5) * _Grade.y + 0.5;
                half luma = dot(col, half3(0.2126, 0.7152, 0.0722));
                col = lerp(luma.xxx, col, _Grade.z);

                col = lerp(col, _Flash.rgb, saturate(_Flash.a));

                float2 vd = i.uv - 0.5;
                vd.x *= _Aspect;
                float r = length(vd);
                col = lerp(col, _Vignette.rgb, smoothstep(_VignetteRange.x, _VignetteRange.y, r) * _Vignette.a);
                col = lerp(col, _Damage.rgb, smoothstep(0.22, 0.85, r) * _Damage.a);

                col += (Hash(i.uv * _ScreenParams.xy + _GrainSeed) - 0.5) * _Grade.w;
                return half4(max(col, 0), 1);
            }
            ENDCG
        }
    }
}
