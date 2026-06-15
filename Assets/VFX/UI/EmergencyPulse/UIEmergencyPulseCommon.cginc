#ifndef UI_EMERGENCY_PULSE_COMMON_INCLUDED
#define UI_EMERGENCY_PULSE_COMMON_INCLUDED

struct appdata_t
{
    float4 vertex : POSITION;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex : SV_POSITION;
    fixed4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _MainTex_TexelSize;
fixed4 _Color;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float _Phase;
float _ExpandPixels;
float _WhiteRimPixels;
float _RedHaloPixels;
float _OuterGlowPixels;
float _OuterGlowStrength;
float _OuterGlowSoftness;
float _GlowSteps;
float _WhiteBoost;
float _RedBoost;
float _Intensity;
float _BreathBrightness;
float _AlphaBlur;
fixed4 _WhiteColor;
fixed4 _RedColor;

v2f vert(appdata_t input)
{
    v2f output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.vertex = UnityObjectToClipPos(input.vertex);
    output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
    output.color = input.color * _Color;
    return output;
}

float SampleAlpha(float2 uv)
{
    if (any(uv < 0.0) || any(uv > 1.0))
    {
        return 0.0;
    }

    return (tex2D(_MainTex, uv) + _TextureSampleAdd).a;
}

float BlurredAlpha(float2 uv, float blurPixels)
{
    float2 step = _MainTex_TexelSize.xy * max(blurPixels, 1.0);
    float sum = SampleAlpha(uv) * 2.0;
    sum += SampleAlpha(uv + float2(step.x, 0.0));
    sum += SampleAlpha(uv + float2(-step.x, 0.0));
    sum += SampleAlpha(uv + float2(0.0, step.y));
    sum += SampleAlpha(uv + float2(0.0, -step.y));
    sum += SampleAlpha(uv + float2(step.x, step.y));
    sum += SampleAlpha(uv + float2(-step.x, step.y));
    return sum / 8.0;
}

float DilateAlpha(float2 uv, float pixelRadius)
{
    float2 step = _MainTex_TexelSize.xy * max(pixelRadius, 0.5);
    float result = SampleAlpha(uv);
    result = max(result, SampleAlpha(uv + float2(step.x, 0.0)));
    result = max(result, SampleAlpha(uv + float2(-step.x, 0.0)));
    result = max(result, SampleAlpha(uv + float2(0.0, step.y)));
    result = max(result, SampleAlpha(uv + float2(0.0, -step.y)));
    result = max(result, SampleAlpha(uv + float2(step.x, step.y)));
    result = max(result, SampleAlpha(uv + float2(-step.x, step.y)));
    result = max(result, SampleAlpha(uv + float2(step.x, -step.y)));
    result = max(result, SampleAlpha(uv + float2(-step.x, -step.y)));
    return result;
}

float DilateBlurredAlpha(float2 uv, float pixelRadius, float blurPixels)
{
    float2 step = _MainTex_TexelSize.xy * max(pixelRadius, 0.5);
    float result = BlurredAlpha(uv, blurPixels);
    result = max(result, BlurredAlpha(uv + float2(step.x, 0.0), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(-step.x, 0.0), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(0.0, step.y), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(0.0, -step.y), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(step.x, step.y), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(-step.x, step.y), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(step.x, -step.y), blurPixels));
    result = max(result, BlurredAlpha(uv + float2(-step.x, -step.y), blurPixels));
    return result;
}

fixed4 ApplyBreath(fixed4 color)
{
    float breath = _BreathBrightness;
    color.rgb *= breath;
    color.a *= lerp(0.94, breath, 0.7);
    return color;
}

float SoftOuterHalo(float2 uv, float flowPx)
{
    float inner = DilateAlpha(uv, flowPx);
    float totalWidth = max(_OuterGlowPixels, 1.0);
    float accum = 0.0;

    [unroll]
    for (int i = 1; i <= 5; i++)
    {
        float t = i / 5.0;
        float radius = flowPx + totalWidth * t;
        float outer = DilateAlpha(uv, radius);
        float band = saturate(outer - inner);
        float feather = pow(band, _OuterGlowSoftness + t * 1.5);
        float weight = (1.0 - t) * (1.0 - t);
        accum += feather * weight;
    }

    return accum * 0.28;
}

fixed4 frag_soft(v2f input) : SV_Target
{
    float2 uv = input.texcoord;
    float flowPx = _Phase * _ExpandPixels;
    float blurPx = max(_AlphaBlur, 1.0);
    float coreWidth = max(_WhiteRimPixels + _RedHaloPixels, 4.0);
    float haloWidth = max(_OuterGlowPixels * _OuterGlowStrength * 0.22, 0.0);
    float totalWidth = coreWidth + haloWidth;

    float edgeInner = DilateBlurredAlpha(uv, flowPx, blurPx);
    float edgeCore = DilateBlurredAlpha(uv, flowPx + coreWidth, blurPx);
    float edgeOuter = DilateBlurredAlpha(uv, flowPx + totalWidth, blurPx);
    float glow = saturate(edgeCore - edgeInner);
    glow = pow(glow, max(1.4, _OuterGlowSoftness * 0.36));

    if (glow <= 0.0005 && edgeOuter - edgeCore <= 0.0005)
    {
        return fixed4(0.0, 0.0, 0.0, 0.0);
    }

    float edgeWhite = DilateBlurredAlpha(uv, flowPx + _WhiteRimPixels, blurPx);

    float whiteField = pow(saturate(edgeWhite - edgeInner), 1.2);
    float redField = pow(saturate(edgeCore - edgeWhite), 1.55);

    float outerHalo = saturate(edgeOuter - edgeCore);
    outerHalo = pow(outerHalo, 2.6) * _OuterGlowStrength * 0.28;

    fixed3 rgb =
        _WhiteColor.rgb * whiteField * _WhiteBoost +
        _RedColor.rgb * (redField * _RedBoost + outerHalo);

    float alpha = saturate((whiteField * 0.62 + redField * 0.82 + outerHalo * 0.25) * _Intensity);
    fixed4 color = fixed4(rgb * _Intensity, alpha) * input.color;
    color.a *= UnityGet2DClipping(input.vertex.xy, _ClipRect);
    return ApplyBreath(color);
}

fixed4 frag_outer(v2f input) : SV_Target
{
#ifdef _PULSE_SOFT
    return frag_soft(input);
#else
    float2 uv = input.texcoord;
    float flowPx = _Phase * _ExpandPixels;
    float halo = SoftOuterHalo(uv, flowPx);

    if (halo <= 0.0003)
    {
        return fixed4(0.0, 0.0, 0.0, 0.0);
    }

    float edgeBase = DilateAlpha(uv, flowPx);
    float edgeTip = DilateAlpha(uv, flowPx + _WhiteRimPixels + _RedHaloPixels * 0.35);
    float edgeT = saturate((edgeTip - edgeBase) / max(edgeTip, 0.001));
    fixed3 auraColor = lerp(_WhiteColor.rgb, _RedColor.rgb, smoothstep(0.12, 0.75, edgeT));

    fixed3 rgb = auraColor * halo * _OuterGlowStrength * 1.6;
    float alpha = halo * _OuterGlowStrength * 0.22;
    fixed4 color = fixed4(rgb, alpha) * input.color;
    color.a *= UnityGet2DClipping(input.vertex.xy, _ClipRect);
    return ApplyBreath(color);
#endif
}

fixed4 frag_core(v2f input) : SV_Target
{
#ifdef _PULSE_SOFT
    return fixed4(0.0, 0.0, 0.0, 0.0);
#else
    float2 uv = input.texcoord;
    float flowPx = _Phase * _ExpandPixels;
    float coreWidth = max(_WhiteRimPixels + _RedHaloPixels, 1.0);
    int steps = (int)clamp(round(_GlowSteps), 3.0, 8.0);

    float3 accumColor = 0.0;
    float accumWeight = 0.0;

    [loop]
    for (int s = 0; s < 8; s++)
    {
        if (s >= steps)
        {
            break;
        }

        float t = steps <= 1 ? 0.0 : s / (float)(steps - 1);
        float slice = max(coreWidth / (float)steps, 1.5);
        float rInner = flowPx + coreWidth * t;
        float rOuter = rInner + slice;
        float ring = saturate(DilateAlpha(uv, rOuter) - DilateAlpha(uv, rInner));
        ring = pow(ring, lerp(0.55, 2.4, t));

        float weight = exp(-t * 2.6) * ring;
        float3 ringColor = lerp(
            _WhiteColor.rgb * _WhiteBoost,
            _RedColor.rgb * _RedBoost,
            smoothstep(0.04, 0.62, t));

        accumColor += ringColor * weight;
        accumWeight += weight;
    }

    if (accumWeight <= 0.0003)
    {
        return fixed4(0.0, 0.0, 0.0, 0.0);
    }

    float3 pulseColor = accumColor / max(accumWeight, 0.0001);
    float alpha = saturate(accumWeight * 0.38 * _Intensity);
    fixed4 color = fixed4(pulseColor * _Intensity, alpha) * input.color;
    color.a *= UnityGet2DClipping(input.vertex.xy, _ClipRect);
    return ApplyBreath(color);
#endif
}

#endif
