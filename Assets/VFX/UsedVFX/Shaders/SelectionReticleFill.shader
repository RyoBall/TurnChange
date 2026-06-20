Shader "TurnChange/SelectionReticleFill"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (1, 0.15, 0.05, 0.35)
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.12
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.42
        _FillPower ("Fill Power", Range(0.25, 4)) = 1.4
        _CenterBoost ("Center Boost", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float _InnerRadius;
                float _OuterRadius;
                float _FillPower;
                float _CenterBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centeredUv = input.uv - 0.5;
                float distanceToCenter = length(centeredUv) * 2.0;
                float ringMask = smoothstep(_InnerRadius, _InnerRadius + 0.03, distanceToCenter);
                ringMask *= 1.0 - smoothstep(_OuterRadius - 0.04, _OuterRadius, distanceToCenter);
                float centerGlow = (1.0 - saturate(distanceToCenter / max(_InnerRadius, 0.001))) * _CenterBoost;
                float alpha = saturate(ringMask + centerGlow);
                alpha = pow(alpha, _FillPower);
                half4 color = _FillColor * input.color;
                color.a *= alpha;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
