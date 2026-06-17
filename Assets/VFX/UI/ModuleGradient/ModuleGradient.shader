Shader "UI/ModuleGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorA ("Color A", Color) = (0.28, 0.78, 1.0, 0.9)
        _ColorB ("Color B", Color) = (0.1, 0.3, 0.85, 0.9)
        _GradientAngle ("Gradient Angle (deg)", Range(0, 360)) = 45

        // Per-cell, set from C# on each material instance
        _CellOffset ("Cell Offset (shapeRoot px)", Vector) = (0, 0, 0, 0)
        _CellSize   ("Cell Size (px)",              Vector) = (32, 32, 0, 0)
        _BoundsMin  ("Shape Bounds Min (px)",        Vector) = (-32, -32, 0, 0)
        _BoundsMax  ("Shape Bounds Max (px)",        Vector) = (32, 32, 0, 0)

        _StencilComp      ("Stencil Comparison",  Float) = 8
        _Stencil          ("Stencil ID",          Float) = 0
        _StencilOp        ("Stencil Operation",   Float) = 0
        _StencilWriteMask ("Stencil Write Mask",  Float) = 255
        _StencilReadMask  ("Stencil Read Mask",   Float) = 255
        _ColorMask        ("Color Mask",          Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _ColorA;
            fixed4 _ColorB;
            float  _GradientAngle;
            float4 _CellOffset;
            float4 _CellSize;
            float4 _BoundsMin;
            float4 _BoundsMax;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                OUT.vertex        = UnityObjectToClipPos(v.vertex);
                OUT.texcoord      = v.texcoord;
                OUT.color         = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Pixel position in shapeRoot local space (pixels)
                // texcoord runs 0→1 across the cell; (texcoord - 0.5) * cellSize gives
                // the offset from the cell's anchor center.
                float2 posInShape = _CellOffset.xy + (IN.texcoord - 0.5) * _CellSize.xy;

                // Normalize to [0,1] across the whole shape's bounding box
                float2 boundsSize = max(_BoundsMax.xy - _BoundsMin.xy, float2(1, 1));
                float2 normPos    = (posInShape - _BoundsMin.xy) / boundsSize;

                // Project onto gradient direction
                float rad = _GradientAngle * (3.14159265 / 180.0);
                float2 dir = float2(cos(rad), sin(rad));
                float t = saturate(dot(normPos - 0.5, dir) + 0.5);

                fixed4 col = lerp(_ColorA, _ColorB, t);

                // Preserve vertex-color alpha (CanvasGroup, loaded-state fade, etc.)
                col.a *= IN.color.a;

                // Sprite alpha mask (rounded corners if the sprite has them)
                col.a *= tex2D(_MainTex, IN.texcoord).a;

                // UI scroll-rect / mask clipping
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
