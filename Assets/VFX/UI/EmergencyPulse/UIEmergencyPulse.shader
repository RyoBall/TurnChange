Shader "UI/EmergencyPulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Phase ("Phase", Range(0, 1)) = 0
        _ExpandPixels ("Expand Pixels", Float) = 10
        _WhiteRimPixels ("White Rim Pixels", Float) = 3
        _RedHaloPixels ("Red Halo Pixels", Float) = 14
        _OuterGlowPixels ("Outer Glow Pixels", Float) = 38
        _OuterGlowStrength ("Outer Glow Strength", Range(0, 3)) = 1.15
        _OuterGlowSoftness ("Outer Glow Softness", Range(1, 8)) = 4.2
        _GlowSteps ("Glow Steps", Range(3, 8)) = 6
        _WhiteBoost ("White Boost", Range(0, 8)) = 4
        _RedBoost ("Red Boost", Range(0, 4)) = 1.2
        _Intensity ("Intensity", Range(0, 3)) = 1.35
        _BreathBrightness ("Breath Brightness", Range(0, 2)) = 1
        _AlphaBlur ("Alpha Blur", Range(0, 12)) = 5
        _WhiteColor ("White Color", Color) = (1, 1, 1, 1)
        _RedColor ("Red Color", Color) = (1, 0.18, 0.08, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        // Pass 0: wide soft aura — simulates bloom for Overlay UI (no post-process)
        Pass
        {
            Name "OuterAura"
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_outer
            #pragma shader_feature_local _PULSE_SOFT
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "UIEmergencyPulseCommon.cginc"
            ENDCG
        }

        // Pass 1: bright core rim with smooth white→red gradient
        Pass
        {
            Name "CoreRim"
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_core
            #pragma shader_feature_local _PULSE_SOFT
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "UIEmergencyPulseCommon.cginc"
            ENDCG
        }
    }

    Fallback Off
}
