Shader "Hidden/TurnChange/FieldDomainEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "FieldDomainEffect"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 SampleSourceColor(float2 uv)
            {
                // Blitter(RenderFeature) 使用 Blit.hlsl 中的 _BlitTexture；CommandBuffer.Blit(Hook) 使用 _MainTex。
                float4 blitColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return dot(blitColor, blitColor) >= dot(mainColor, mainColor) ? blitColor : mainColor;
            }

            float4 _Origin;
            float _Radius;
            float _MaxRadius;
            float _WaveWidth;
            float _Phase;
            float _Intensity;

            float4 _TintColor;
            float _Saturation;
            float _Contrast;
            float _Exposure;
            float _DistortionStrength;

            float4 _VignetteColor;
            float _VignetteIntensity;

            float4 _GridColor;
            float _GridLineWidth;
            float _GridScale;
            float _EdgeGridWidth;

            float _BreathSpeed;
            float _BreathAmplitude;
            float _HeartbeatPhase;
            float _HeartbeatStrength;
            float _BloomStrength;
            float _EffectTime;

            float GridLine(float2 uv, float lineWidth)
            {
                float2 grid = abs(frac(uv - 0.5) - 0.5) / fwidth(uv);
                float gridDist = min(grid.x, grid.y);
                return 1.0 - saturate(gridDist - (10.0 - lineWidth * 2.0));
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float Noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = dot(Hash22(i + float2(0.0, 0.0)) * 2.0 - 1.0, f - float2(0.0, 0.0));
                float b = dot(Hash22(i + float2(1.0, 0.0)) * 2.0 - 1.0, f - float2(1.0, 0.0));
                float c = dot(Hash22(i + float2(0.0, 1.0)) * 2.0 - 1.0, f - float2(0.0, 1.0));
                float d = dot(Hash22(i + float2(1.0, 1.0)) * 2.0 - 1.0, f - float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
            }

            float3 ApplyColorGrade(float3 color, float2 uv, float gradeMask, float pulse)
            {
                color *= _Exposure * (1.0 + pulse * _HeartbeatStrength * 0.15);

                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luma.xxx, color, _Saturation);

                color = (color - 0.5) * (_Contrast + pulse * _HeartbeatStrength * 0.25) + 0.5;
                color = lerp(color, color * _TintColor.rgb, _TintColor.a * gradeMask);

                float2 centered = (uv - _Origin.xy);
                centered.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float vignetteDist = length(centered);
                float vignette = smoothstep(0.2, 1.2, vignetteDist);
                color = lerp(color, color * _VignetteColor.rgb, vignette * _VignetteIntensity * gradeMask * (1.0 + pulse * _HeartbeatStrength));

                return color;
            }

            float3 ApplyBloomApprox(float3 color, float bloomMask)
            {
                if (_BloomStrength <= 0.001 || bloomMask <= 0.001)
                {
                    return color;
                }

                float brightness = max(color.r, max(color.g, color.b));
                float bloom = saturate((brightness - 0.65) * 2.5) * _BloomStrength * bloomMask;
                return color + bloom * float3(0.85, 0.95, 1.0);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 aspectCorrected = uv - _Origin.xy;
                aspectCorrected.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float dist = length(aspectCorrected);

                float safeMaxRadius = max(_MaxRadius, 0.0001);
                float activeRadius = _Phase >= 0.5 ? safeMaxRadius : _Radius;
                float insideMask = 1.0 - smoothstep(activeRadius - 0.002, activeRadius + 0.004, dist);

                if (_Phase >= 1.5)
                {
                    insideMask = 1.0 - smoothstep(_Radius - 0.002, _Radius + 0.004, dist);
                }
                else if (_Phase >= 0.5)
                {
                    insideMask = 1.0;
                }

                float pulse = _HeartbeatStrength > 0.001
                    ? saturate(sin(_HeartbeatPhase) * 0.5 + 0.5)
                    : 0.0;

                float breath = sin(_EffectTime * _BreathSpeed) * _BreathAmplitude;

                float2 sampleUV = uv;
                if (_DistortionStrength > 0.001 && insideMask > 0.001)
                {
                    float n = Noise(uv * 8.0 + _EffectTime * 1.5);
                    float2 dir = normalize(aspectCorrected + float2(0.0001, 0.0));
                    sampleUV += dir * (n - 0.5) * _DistortionStrength * insideMask * 0.03;
                    sampleUV += dir * sin(dist * 18.0 - _EffectTime * 4.0) * _DistortionStrength * insideMask * 0.01;
                }

                float4 source = SampleSourceColor(sampleUV);
                float3 graded = ApplyColorGrade(source.rgb, uv, insideMask, pulse);
                graded = ApplyBloomApprox(graded, insideMask);

                float3 result = lerp(source.rgb, graded, insideMask * _Intensity);

                float waveRing = 0.0;
                if (_Phase < 0.5 || _Phase >= 1.5)
                {
                    float ringDist = abs(dist - _Radius);
                    waveRing = 1.0 - smoothstep(0.0, max(_WaveWidth, 0.001), ringDist);
                    waveRing *= _Intensity;
                }

                if (waveRing > 0.001)
                {
                    float2 gridUV = uv * _GridScale * 40.0;
                    float grid = GridLine(gridUV, _GridLineWidth + breath * 2.0);
                    result = lerp(result, _GridColor.rgb, grid * waveRing * _GridColor.a);
                    result += _GridColor.rgb * waveRing * 0.35;
                }

                if (_Phase >= 0.5 && _Phase < 1.5)
                {
                    float edgeDist = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float edgeMask = 1.0 - smoothstep(0.0, max(_EdgeGridWidth, 0.001), edgeDist);
                    edgeMask *= _Intensity;

                    float edgeBreath = 0.5 + 0.5 * sin(_EffectTime * _BreathSpeed);
                    float2 edgeGridUV = uv * _GridScale * 60.0;
                    float edgeGrid = GridLine(edgeGridUV, _GridLineWidth * (0.8 + edgeBreath * 0.6 + breath));
                    float edgeGlow = edgeGrid * edgeMask * (_GridColor.a + breath * 0.5) * edgeBreath;
                    result = lerp(result, _GridColor.rgb, edgeGlow);
                    result += _GridColor.rgb * edgeMask * edgeBreath * 0.12;
                }

                return float4(result, source.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
