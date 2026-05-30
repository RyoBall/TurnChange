Shader "Hidden/TurnChange/FieldDomainEffect"
{
    Properties
    {
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "white" {}
        _Origin ("Origin", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0
        _MaxRadius ("Max Radius", Float) = 1.5
        _WaveWidth ("Wave Width", Float) = 0.035
        _Phase ("Phase", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _VisualStyle ("Visual Style", Float) = 0
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Saturation ("Saturation", Float) = 1
        _Contrast ("Contrast", Float) = 1
        _Exposure ("Exposure", Float) = 1
        _DistortionStrength ("Distortion Strength", Float) = 0
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _VignetteIntensity ("Vignette Intensity", Float) = 0
        _GridColor ("Grid Color", Color) = (1, 1, 1, 1)
        _GridLineWidth ("Grid Line Width", Float) = 3
        _GridScale ("Grid Scale", Float) = 1
        _EdgeGridWidth ("Edge Grid Width", Float) = 0.025
        _EdgeGridSoftness ("Edge Grid Softness", Float) = 0.02
        _BreathSpeed ("Breath Speed", Float) = 1
        _BreathAmplitude ("Breath Amplitude", Float) = 0.25
        _HeartbeatPhase ("Heartbeat Phase", Float) = 0
        _HeartbeatStrength ("Heartbeat Strength", Float) = 0
        _BloomStrength ("Bloom Strength", Float) = 0
        _EffectTime ("Effect Time", Float) = 0
        _GrainStrength ("Grain Strength", Float) = 0
        _ChromaticStrength ("Chromatic Strength", Float) = 0
        _RadialGlowStrength ("Radial Glow Strength", Float) = 0
        _HeatShimmerStrength ("Heat Shimmer Strength", Float) = 0
        _SecondaryAccentColor ("Secondary Accent Color", Color) = (1, 0.75, 0.2, 1)
        _BorderVfxStrength ("Border VFX Strength", Float) = 0.6
        _BorderVfxDepth ("Border VFX Depth", Float) = 0.12
        _BorderVfxEdgeSoftness ("Border VFX Edge Softness", Float) = 0.72
        _RingBurnStrength ("Ring Burn Strength", Float) = 0.5
        _BorderVfxSpeed ("Border VFX Speed", Float) = 1.2
        _BorderVfxHotColor ("Border VFX Hot Color", Color) = (1, 0.95, 0.5, 1)
        _BorderVfxCoreColor ("Border VFX Core Color", Color) = (1, 0.45, 0.08, 1)
        _FlameNoiseTex ("Flame Noise", 2D) = "white" {}
        _FlameNoiseTiling ("Flame Noise Tiling", Vector) = (5.5, 14, 0, 0)
        _FlameNoiseInwardStretch ("Flame Noise Inward Stretch", Float) = 1.8
        _FlameNoiseInwardScroll ("Flame Noise Inward Scroll", Float) = 0.4
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

            #define STYLE_VERDICT 0
            #define STYLE_DESPERATION 1
            #define STYLE_MIRACLE 2

            CBUFFER_START(UnityPerMaterial)
                float4 _Origin;
                float _Radius;
                float _MaxRadius;
                float _WaveWidth;
                float _Phase;
                float _Intensity;
                float _VisualStyle;
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
                float _EdgeGridSoftness;
                float _BreathSpeed;
                float _BreathAmplitude;
                float _HeartbeatPhase;
                float _HeartbeatStrength;
                float _BloomStrength;
                float _EffectTime;
                float _GrainStrength;
                float _ChromaticStrength;
                float _RadialGlowStrength;
                float _HeatShimmerStrength;
                float4 _SecondaryAccentColor;
                float _BorderVfxStrength;
                float _BorderVfxDepth;
                float _BorderVfxEdgeSoftness;
                float _RingBurnStrength;
                float _BorderVfxSpeed;
                float4 _BorderVfxHotColor;
                float4 _BorderVfxCoreColor;
                float4 _FlameNoiseTiling;
                float _FlameNoiseInwardStretch;
                float _FlameNoiseInwardScroll;
            CBUFFER_END

            TEXTURE2D(_FlameNoiseTex);
            SAMPLER(sampler_FlameNoiseTex);

            #define EDGE_LEFT 0
            #define EDGE_RIGHT 1
            #define EDGE_BOTTOM 2
            #define EDGE_TOP 3

            float GridLine(float2 uv, float lineWidth)
            {
                float2 grid = abs(frac(uv - 0.5) - 0.5) / fwidth(uv);
                float gridDist = min(grid.x, grid.y);
                return 1.0 - saturate(gridDist - (10.0 - lineWidth * 2.0));
            }

            float RadialGridLine(float2 uv, float lineWidth)
            {
                float angle = atan2(uv.y, uv.x);
                float radial = frac(length(uv) * 3.5 - _EffectTime * 0.15);
                float spokes = abs(frac(angle * 0.15915494 + 0.5) - 0.5) / fwidth(angle);
                float spokeLine = 1.0 - saturate(spokes - (8.0 - lineWidth));
                float ring = abs(radial - 0.5) / fwidth(radial);
                float ringLine = 1.0 - saturate(ring - (6.0 - lineWidth * 0.5));
                return saturate(max(spokeLine, ringLine * 0.65));
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

            float FilmGrain(float2 uv, float time)
            {
                return frac(sin(dot(uv * 900.0 + time * 47.0, float2(12.9898, 78.233))) * 43758.5453) * 2.0 - 1.0;
            }

            float GetScreenAspect()
            {
                return _ScreenParams.x / max(_ScreenParams.y, 1.0);
            }

            // 向内距离按「高度归一化」：左右边乘 aspect，与上下边在像素尺度上一致
            float EdgeDistanceUv(float2 uv, float aspect)
            {
                float distX = min(uv.x, 1.0 - uv.x) * aspect;
                float distY = min(uv.y, 1.0 - uv.y);
                return min(distX, distY);
            }

            float BorderMaskFromEdge(float edgeDist, float depth)
            {
                return 1.0 - smoothstep(0.0, depth, edgeDist);
            }

            // 沿最近边的向内距离（min），整条边都有遮罩；勿用 length(edgeVec)，那只会亮四角
            float GetSoftEdgeBorderMask(float2 uv, float depth, float aspect)
            {
                float edgeDist = EdgeDistanceUv(uv, aspect);
                float t = saturate(edgeDist / max(depth, 0.001));
                float mask = 1.0 - smoothstep(0.0, 1.0, t);
                float softness = max(_BorderVfxEdgeSoftness, 0.2);
                return pow(saturate(mask), softness);
            }

            float Fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * Noise(uv);
                    uv = uv * 2.02 + shift;
                    amplitude *= 0.5;
                }
                return value;
            }

            float SampleFlameNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_FlameNoiseTex, sampler_FlameNoiseTex, uv).r;
            }

            float GetFlameInwardScroll()
            {
                return _EffectTime * _BorderVfxSpeed * _FlameNoiseInwardScroll;
            }

            float2 BuildInwardFlameUV(float alongCoord, float inwardNorm)
            {
                float along = alongCoord * _FlameNoiseTiling.x;
                float inward = inwardNorm * _FlameNoiseTiling.y * _FlameNoiseInwardStretch * 2.4;
                float scroll = GetFlameInwardScroll();
                return float2(along, inward - scroll);
            }

            float SampleFlameDensityInward(float2 flameUV, float inwardNorm)
            {
                float warpIn = SampleFlameNoise(float2(flameUV.x * 0.35 + 6.0, flameUV.y * 0.55 + 3.0)) - 0.5;
                float2 uv = float2(flameUV.x + warpIn * 0.022, flameUV.y + warpIn * 0.065);

                float primary = SampleFlameNoise(uv);
                float detail = SampleFlameNoise(float2(uv.x * 1.6, uv.y * 2.75));

                float ridged = 1.0 - abs(primary * 2.0 - 1.0);
                float ridgedDetail = 1.0 - abs(detail * 2.0 - 1.0);
                float tongues = saturate(ridged * 0.72 + ridgedDetail * 0.48);
                tongues = pow(tongues, 1.4);

                float softFill = smoothstep(0.22, 0.7, primary * 0.55 + detail * 0.45);
                float density = saturate(tongues * 0.92 + softFill * 0.26);

                float edgeWeight = 1.0 - inwardNorm * 0.55;
                density *= 0.5 + 0.5 * edgeWeight;
                density = max(density, edgeWeight * 0.2);

                float flicker = 0.88 + 0.12 * sin(_EffectTime * _BorderVfxSpeed * 2.4 + inwardNorm * 9.5);
                return saturate(density * flicker);
            }

            int GetDominantEdge(float2 uv, float aspect)
            {
                float left = uv.x * aspect;
                float right = (1.0 - uv.x) * aspect;
                float bottom = uv.y;
                float top = 1.0 - uv.y;
                float minDist = left;
                int edge = EDGE_LEFT;
                if (right < minDist)
                {
                    minDist = right;
                    edge = EDGE_RIGHT;
                }
                if (bottom < minDist)
                {
                    minDist = bottom;
                    edge = EDGE_BOTTOM;
                }
                if (top < minDist)
                {
                    edge = EDGE_TOP;
                }
                return edge;
            }

            float GetEdgeDistForEdge(float2 uv, int edge, float aspect)
            {
                if (edge == EDGE_LEFT)
                {
                    return uv.x * aspect;
                }
                if (edge == EDGE_RIGHT)
                {
                    return (1.0 - uv.x) * aspect;
                }
                if (edge == EDGE_BOTTOM)
                {
                    return uv.y;
                }
                return (1.0 - uv.y);
            }

            float GetAlongEdgeCoord(float2 uv, int edge, float aspect)
            {
                if (edge == EDGE_LEFT || edge == EDGE_RIGHT)
                {
                    return uv.y;
                }
                return uv.x * aspect;
            }

            // 自底边从左下角起，沿屏幕顺时针一周的连续弧长坐标（与 aspect 校正一致）
            float GetAlongEdgeCoordClockwise(float2 uv, int edge, float aspect)
            {
                if (edge == EDGE_BOTTOM)
                {
                    return uv.x * aspect;
                }
                if (edge == EDGE_RIGHT)
                {
                    return aspect + uv.y;
                }
                if (edge == EDGE_TOP)
                {
                    return aspect + 1.0 + (1.0 - uv.x) * aspect;
                }
                return aspect + 1.0 + aspect + (1.0 - uv.y);
            }

            float2 GetEdgeNormalUv(float2 uv, float aspect)
            {
                int edge = GetDominantEdge(uv, aspect);
                if (edge == EDGE_LEFT)
                {
                    return float2(1.0, 0.0);
                }
                if (edge == EDGE_RIGHT)
                {
                    return float2(-1.0, 0.0);
                }
                if (edge == EDGE_BOTTOM)
                {
                    return float2(0.0, 1.0);
                }
                return float2(0.0, -1.0);
            }

            void GetMiracleGlassFields(float2 uv, float aspect, float insideMask, out float glassMask, out float rimFactor, out float tNorm)
            {
                float edgeDist = EdgeDistanceUv(uv, aspect);
                float band = max(_BorderVfxDepth, 0.001);
                tNorm = saturate(edgeDist / band);
                float falloff = 1.0 - smoothstep(0.0, 1.0, tNorm);
                falloff = pow(saturate(falloff), max(_BorderVfxEdgeSoftness, 1.5));
                falloff *= exp(-tNorm * tNorm * 6.0);
                glassMask = falloff * insideMask;
                rimFactor = pow(saturate(1.0 - tNorm), 1.8);
            }

            float GetMiracleGlassMask(float2 uv, float aspect, float insideMask)
            {
                float glassMask;
                float rimFactor;
                float tNorm;
                GetMiracleGlassFields(uv, aspect, insideMask, glassMask, rimFactor, tNorm);
                return glassMask;
            }

            float EvaluateEdgeFlame(float alongCoord, float inwardNorm)
            {
                if (inwardNorm <= 0.001)
                {
                    return 0.0;
                }

                float2 flameUV = BuildInwardFlameUV(alongCoord, inwardNorm);
                return SampleFlameDensityInward(flameUV, inwardNorm);
            }

            void ApplyVerdictBorderFlame(inout float3 result, float2 uv, float insideMask, float breath, float aspect)
            {
                if (_BorderVfxStrength <= 0.001 || insideMask <= 0.001)
                {
                    return;
                }

                float depth = max(_BorderVfxDepth, 0.001);
                float softBorderMask = GetSoftEdgeBorderMask(uv, depth, aspect);
                if (softBorderMask <= 0.001)
                {
                    return;
                }

                int edge = GetDominantEdge(uv, aspect);
                float edgeDist = GetEdgeDistForEdge(uv, edge, aspect);
                float inwardNorm = saturate(edgeDist / depth);
                float along = GetAlongEdgeCoord(uv, edge, aspect);
                float density = EvaluateEdgeFlame(along, inwardNorm);

                float borderMask = insideMask * softBorderMask * _BorderVfxStrength;
                float flame = borderMask * smoothstep(0.1, 0.92, density);
                float3 flameColor = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, pow(saturate(density), 0.72));
                result += flameColor * flame * (1.1 + breath * 0.4);
            }

            void ApplyDesperationBorderMist(inout float3 result, float2 uv, float borderMaskBase, float pulse)
            {
                if (_BorderVfxStrength <= 0.001 || borderMaskBase <= 0.001)
                {
                    return;
                }

                float borderMask = borderMaskBase * _BorderVfxStrength * (1.0 + pulse * 0.2);
                float2 nuv = float2(uv.x * 2.0 + _EffectTime * _BorderVfxSpeed * 0.3, uv.y * 4.0 - _EffectTime * _BorderVfxSpeed * 0.15);
                float fbm = Fbm(nuv * 3.0);
                float mist = borderMask * smoothstep(0.15, 0.75, fbm);
                float3 mistColor = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, fbm * 0.4);
                result = lerp(result, mistColor, mist * 0.5);
                result += mistColor * mist * 0.15;
            }

            float3 SampleSourceRgb(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            void ApplyMiraclePrismGlass(inout float3 result, float2 uv, float insideMask, float breath, float aspect)
            {
                if (_BorderVfxStrength <= 0.001 || insideMask <= 0.001)
                {
                    return;
                }

                float glassMask;
                float rimFactor;
                float tNorm;
                GetMiracleGlassFields(uv, aspect, insideMask, glassMask, rimFactor, tNorm);
                if (glassMask <= 0.0001)
                {
                    return;
                }

                int edge = GetDominantEdge(uv, aspect);
                float along = GetAlongEdgeCoordClockwise(uv, edge, aspect);
                float2 edgeNormal = GetEdgeNormalUv(uv, aspect);

                float chromaScale = _BorderVfxStrength * (0.0055 + _ChromaticStrength * 0.008);
                float split = chromaScale * glassMask;
                float3 prismSample;
                prismSample.r = SampleSourceRgb(uv + edgeNormal * split * 1.35).r;
                prismSample.g = SampleSourceRgb(uv).g;
                prismSample.b = SampleSourceRgb(uv - edgeNormal * split * 1.1).b;

                float timeFlow = _EffectTime * _BorderVfxSpeed;
                float edgeScroll = along * 7.5 - timeFlow * 2.4;
                float edgeScrollSlow = along * 3.2 - timeFlow * 0.85;

                float fbmFlow = Fbm(float2(edgeScroll * 0.22, tNorm * 2.5 - timeFlow * 0.35));
                float fbmFlow2 = Fbm(float2(edgeScrollSlow * 0.35 + 4.7, tNorm * 1.8 + timeFlow * 0.2));

                float hue = edgeScroll * 0.55 + fbmFlow * 0.65 + fbmFlow2 * 0.35 + tNorm * 0.18;
                float3 rainbow = 0.5 + 0.5 * cos(6.28318 * (hue + float3(0.0, 0.33, 0.67)));
                float3 spectral = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, rainbow);

                float tintMix = saturate(sin(edgeScroll * 1.35 + fbmFlow * 0.8) * 0.5 + 0.5);
                float3 iridescentTint = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, tintMix);

                prismSample = lerp(prismSample, spectral, glassMask * rimFactor * 0.48);
                prismSample = lerp(prismSample, iridescentTint, glassMask * 0.28);

                float dispStrength = glassMask * 0.82;
                result = lerp(result, prismSample, dispStrength);

                float fresnel = rimFactor * (0.5 + 0.5 * saturate(1.0 - tNorm * 1.2));
                float3 rimColor = lerp(spectral, iridescentTint, 0.55);
                rimColor = lerp(rimColor, float3(0.95, 0.98, 1.0), 0.15);
                result += rimColor * fresnel * _BorderVfxStrength * 0.32 * (0.88 + breath * 0.18);

                float bandCoord = frac(edgeScroll * 0.42);
                float travelBand = smoothstep(0.62, 0.0, abs(bandCoord - 0.5));
                travelBand += smoothstep(0.22, 0.0, bandCoord) * 0.45;
                travelBand = saturate(travelBand);

                float streakFast = pow(saturate(sin(edgeScroll * 6.28318) * 0.5 + 0.5), 2.2);
                float streakSlow = pow(saturate(sin(edgeScrollSlow * 4.188 + fbmFlow * 2.0) * 0.5 + 0.5), 1.5);

                float hueFlow = edgeScroll * 0.9 + 1.2;
                float3 streamRgb = 0.5 + 0.5 * cos(6.28318 * (hueFlow + float3(0.0, 0.33, 0.67)));
                float3 flowColor = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, streamRgb);
                flowColor = lerp(flowColor, spectral, 0.55);

                float flowIntensity = travelBand * 0.7 + streakFast * 0.55 + streakSlow * 0.4;
                flowIntensity *= glassMask * (rimFactor * 0.65 + (1.0 - tNorm) * 0.35);
                flowIntensity *= _BorderVfxStrength * (0.55 + breath * 0.12);

                result += flowColor * flowIntensity;

                float3 flowHighlight = lerp(spectral, float3(1.0, 0.98, 1.0), 0.35);
                float highlightBand = pow(streakFast * travelBand, 1.4);
                result += flowHighlight * highlightBand * glassMask * rimFactor * _BorderVfxStrength * 0.28;
            }

            void ApplyRingFlameBurn(inout float3 result, float2 aspectCorrected, float dist, float waveRing, float breath)
            {
                if (_RingBurnStrength <= 0.001 || waveRing <= 0.001)
                {
                    return;
                }

                float angle = atan2(aspectCorrected.y, aspectCorrected.x);
                float ringBandNorm = saturate((dist - _Radius) / max(_WaveWidth, 0.001));
                float alongRing = angle * 0.286; // ~1.8 / (2*pi) for tiling along circumference
                float2 ringUV = BuildInwardFlameUV(alongRing, ringBandNorm);
                float density = SampleFlameDensityInward(ringUV, ringBandNorm);
                float burn = waveRing * _RingBurnStrength * smoothstep(0.1, 0.92, density);
                float3 burnColor = lerp(_BorderVfxCoreColor.rgb, _BorderVfxHotColor.rgb, pow(saturate(density), 0.72));
                result += burnColor * burn * (1.2 + breath);
            }

            float3 ApplyDistortion(float2 uv, float2 aspectCorrected, float dist, float insideMask, int style)
            {
                float2 sampleUV = uv;

                if (style == STYLE_VERDICT && _HeatShimmerStrength > 0.001 && insideMask > 0.001)
                {
                    float heat = _HeatShimmerStrength * insideMask;
                    float n = Noise(uv * 6.0 + _EffectTime * 1.2);
                    sampleUV.y += sin(uv.x * 24.0 - _EffectTime * 3.5) * heat * 0.004;
                    sampleUV.x += (n - 0.5) * heat * 0.0025;
                    float2 dir = normalize(aspectCorrected + float2(0.0001, 0.0));
                    sampleUV += dir * sin(dist * 14.0 - _EffectTime * 3.0) * heat * 0.005;
                }
                else if (style != STYLE_DESPERATION && _DistortionStrength > 0.001 && insideMask > 0.001)
                {
                    float n = Noise(uv * 8.0 + _EffectTime * 1.5);
                    float2 dir = normalize(aspectCorrected + float2(0.0001, 0.0));
                    sampleUV += dir * (n - 0.5) * _DistortionStrength * insideMask * 0.03;
                    sampleUV += dir * sin(dist * 18.0 - _EffectTime * 4.0) * _DistortionStrength * insideMask * 0.01;
                }

                if (style == STYLE_DESPERATION && _ChromaticStrength > 0.001 && _HeartbeatStrength > 0.001)
                {
                    float pulse = saturate(sin(_HeartbeatPhase) * 0.5 + 0.5);
                    if (pulse > 0.7)
                    {
                        float chroma = _ChromaticStrength * (pulse - 0.7) / 0.3 * insideMask * 0.004;
                        float r = SampleSourceRgb(sampleUV + float2(chroma, 0.0)).r;
                        float g = SampleSourceRgb(sampleUV).g;
                        float b = SampleSourceRgb(sampleUV - float2(chroma, 0.0)).b;
                        return float3(r, g, b);
                    }
                }

                return SampleSourceRgb(sampleUV);
            }

            float3 ApplyColorGrade(float3 color, float2 uv, float gradeMask, float pulse, int style, float centerClearMask)
            {
                color *= _Exposure * (1.0 + pulse * _HeartbeatStrength * 0.18);

                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luma.xxx, color, _Saturation);

                if (style == STYLE_DESPERATION)
                {
                    color.r = lerp(color.r, max(color.r, luma * 0.85), gradeMask * 0.12);
                    color.r *= 1.0 + gradeMask * 0.08;
                }

                color = (color - 0.5) * (_Contrast + pulse * _HeartbeatStrength * 0.28) + 0.5;
                if (style == STYLE_MIRACLE)
                {
                    color += _TintColor.rgb * _TintColor.a * gradeMask * centerClearMask * 0.35;
                }
                else
                {
                    color = lerp(color, color * _TintColor.rgb, _TintColor.a * gradeMask);
                }

                float2 centered = (uv - _Origin.xy);
                centered.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float vignetteDist = length(centered);
                float vignette = smoothstep(0.2, 1.2, vignetteDist);
                float vignetteBoost = 1.0 + pulse * _HeartbeatStrength * (style == STYLE_DESPERATION ? 1.2 : 0.0);
                color = lerp(color, color * _VignetteColor.rgb, vignette * _VignetteIntensity * gradeMask * vignetteBoost);

                return color;
            }

            float3 ApplyBloomApprox(float3 color, float bloomMask, int style, float centerClearMask)
            {
                if (_BloomStrength <= 0.001 || bloomMask <= 0.001)
                {
                    return color;
                }

                if (style == STYLE_MIRACLE)
                {
                    bloomMask *= centerClearMask;
                }

                if (bloomMask <= 0.001)
                {
                    return color;
                }

                float brightness = max(color.r, max(color.g, color.b));
                float bloom = saturate((brightness - 0.65) * 2.5) * _BloomStrength * bloomMask;
                float3 bloomTint = style == STYLE_MIRACLE
                    ? float3(0.55, 0.88, 0.95)
                    : float3(0.85, 0.95, 1.0);
                return color + bloom * bloomTint;
            }

            float3 ApplyCenterGlow(float2 uv, float2 aspectCorrected, float dist, float mask, int style, float centerClearMask)
            {
                if (style != STYLE_MIRACLE || _RadialGlowStrength <= 0.001 || mask <= 0.001)
                {
                    return float3(0.0, 0.0, 0.0);
                }

                float glow = exp(-dist * 2.2) * _RadialGlowStrength * mask * centerClearMask;
                float breath = 0.85 + 0.15 * sin(_EffectTime * _BreathSpeed);
                return _SecondaryAccentColor.rgb * glow * breath * 0.75;
            }

            void ApplyWaveRingOverlay(inout float3 result, float2 uv, float2 aspectCorrected, float dist, float waveRing, float breath, int style, float centerClearMask)
            {
                if (waveRing <= 0.001)
                {
                    return;
                }

                if (style == STYLE_VERDICT)
                {
                    float2 radialUV = aspectCorrected * _GridScale * 28.0;
                    float radial = RadialGridLine(radialUV, _GridLineWidth + breath * 1.5);
                    float flicker = 0.75 + 0.25 * sin(_EffectTime * 9.0 + dist * 12.0);
                    result = lerp(result, _GridColor.rgb, radial * waveRing * _GridColor.a * flicker);
                    result += _SecondaryAccentColor.rgb * waveRing * radial * flicker * 0.4;
                }
                else if (style == STYLE_DESPERATION)
                {
                    float2 gridUV = uv * _GridScale * 48.0;
                    float grid = GridLine(gridUV, _GridLineWidth * 0.85);
                    result = lerp(result, _GridColor.rgb, grid * waveRing * _GridColor.a);
                    result += _GridColor.rgb * waveRing * 0.2;
                }
                else
                {
                    float softBand = waveRing * (0.65 + 0.35 * sin(_EffectTime * _BreathSpeed * 0.8)) * centerClearMask;
                    float3 waveColor = lerp(_GridColor.rgb, _SecondaryAccentColor.rgb, 0.55);
                    result = lerp(result, waveColor, softBand * _GridColor.a * 0.18);
                    result += waveColor * softBand * 0.06;
                }
            }

            void ApplyActiveEdgeOverlay(inout float3 result, float2 uv, float2 aspectCorrected, float dist, float safeMaxRadius, float insideMask, float breath, float pulse, int style, float centerClearMask)
            {
                float ringDist = abs(dist - safeMaxRadius);
                float edgeInner = max(_EdgeGridWidth, 0.001);
                float edgeOuter = edgeInner + max(_EdgeGridSoftness, 0.001);
                float edgeMask = 1.0 - smoothstep(edgeInner, edgeOuter, ringDist);
                edgeMask *= _Intensity;

                if (edgeMask <= 0.001)
                {
                    return;
                }

                float edgeBreath = 0.5 + 0.5 * sin(_EffectTime * _BreathSpeed);

                if (style == STYLE_VERDICT)
                {
                    float2 radialUV = aspectCorrected * _GridScale * 42.0;
                    float radial = RadialGridLine(radialUV, _GridLineWidth * (0.9 + edgeBreath * 0.5 + breath));
                    float edgeGlow = radial * edgeMask * (_GridColor.a + breath * 0.4) * edgeBreath;
                    result = lerp(result, _GridColor.rgb, edgeGlow);
                    result += _SecondaryAccentColor.rgb * edgeMask * edgeBreath * 0.18;
                }
                else if (style == STYLE_DESPERATION)
                {
                    float2 edgeGridUV = uv * _GridScale * 65.0;
                    float edgeGrid = GridLine(edgeGridUV, _GridLineWidth * (0.7 + edgeBreath * 0.35));
                    float edgeGlow = edgeGrid * edgeMask * (_GridColor.a * 0.9) * (0.6 + pulse * 0.5);
                    result = lerp(result, _GridColor.rgb, edgeGlow);
                    result += _SecondaryAccentColor.rgb * edgeMask * pulse * 0.08;

                    if (_GrainStrength > 0.001)
                    {
                        float grain = FilmGrain(uv, _EffectTime) * _GrainStrength * insideMask * 0.04;
                        result += grain;
                    }
                }
                else
                {
                    float softEdge = edgeMask * (0.55 + 0.45 * edgeBreath) * centerClearMask;
                    float3 edgeColor = lerp(_GridColor.rgb, _SecondaryAccentColor.rgb, 0.6);
                    result = lerp(result, edgeColor, softEdge * _GridColor.a * 0.14);
                    result += edgeColor * softEdge * 0.03;
                }
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                int style = (int)round(_VisualStyle);

                float aspect = GetScreenAspect();
                float2 aspectCorrected = uv - _Origin.xy;
                aspectCorrected.x *= aspect;
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

                float centerClearMask = 1.0;
                if (style == STYLE_MIRACLE)
                {
                    centerClearMask = 1.0 - GetMiracleGlassMask(uv, aspect, insideMask);
                }

                float3 sourceRgb = ApplyDistortion(uv, aspectCorrected, dist, insideMask, style);
                float3 graded = ApplyColorGrade(sourceRgb, uv, insideMask, pulse, style, centerClearMask);
                graded = ApplyBloomApprox(graded, insideMask, style, centerClearMask);
                graded += ApplyCenterGlow(uv, aspectCorrected, dist, insideMask, style, centerClearMask);

                float3 result = lerp(sourceRgb, graded, insideMask * _Intensity);

                if (style == STYLE_DESPERATION && _GrainStrength > 0.001 && insideMask > 0.001)
                {
                    float grain = FilmGrain(uv, _EffectTime) * _GrainStrength * insideMask * 0.035;
                    result += grain;
                }

                float waveRing = 0.0;
                if (_Phase < 0.5 || _Phase >= 1.5)
                {
                    float ringDist = abs(dist - _Radius);
                    waveRing = 1.0 - smoothstep(0.0, max(_WaveWidth, 0.001), ringDist);
                    waveRing *= _Intensity;
                }

                ApplyWaveRingOverlay(result, uv, aspectCorrected, dist, waveRing, breath, style, centerClearMask);

                if (style == STYLE_VERDICT && waveRing > 0.001)
                {
                    ApplyRingFlameBurn(result, aspectCorrected, dist, waveRing, breath);
                }

                if (_Phase >= 0.5 && _Phase < 1.5)
                {
                    ApplyActiveEdgeOverlay(result, uv, aspectCorrected, dist, safeMaxRadius, insideMask, breath, pulse, style, centerClearMask);
                }

                if (style == STYLE_VERDICT)
                {
                    ApplyVerdictBorderFlame(result, uv, insideMask, breath, aspect);
                }
                else if (style == STYLE_DESPERATION)
                {
                    float borderEdge = EdgeDistanceUv(uv, aspect);
                    float borderMaskBase = BorderMaskFromEdge(borderEdge, _BorderVfxDepth) * insideMask;
                    ApplyDesperationBorderMist(result, uv, borderMaskBase, pulse);
                }
                else if (style == STYLE_MIRACLE)
                {
                    ApplyMiraclePrismGlass(result, uv, insideMask, breath, aspect);
                }

                float4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                return float4(result, source.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
