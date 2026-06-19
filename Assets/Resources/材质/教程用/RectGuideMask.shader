Shader "UI/RectGuideMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 0.7)    // 遮罩颜色（默认半透明黑色）

        // 高亮矩形参数（归一化坐标，范围0~1，相对于屏幕/Canvas宽高）
        _RectMinX ("Rect Min X", Range(0, 1)) = 0.3
        _RectMaxX ("Rect Max X", Range(0, 1)) = 0.7
        _RectMinY ("Rect Min Y", Range(0, 1)) = 0.3
        _RectMaxY ("Rect Max Y", Range(0, 1)) = 0.7

        // 边缘柔化参数
        _EdgeSoftness ("Edge Softness", Range(0, 0.1)) = 0.02

        // 高亮区域是否显示（用于调试）
        _ShowHighlight ("Show Highlight Region", Range(0, 1)) = 0

        // 高亮区域是否可交互（false 时高亮区域 alpha 降为 0.1 而非 0，且射线被阻挡）
        [Toggle] _HighlightInteractable ("Highlight Interactable", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha     // 标准UI混合模式

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 screenUV : TEXCOORD1;  // 屏幕UV坐标（用于高亮判断）
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _RectMinX;
            float _RectMaxX;
            float _RectMinY;
            float _RectMaxY;
            float _EdgeSoftness;
            float _ShowHighlight;
            float _HighlightInteractable;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;

                // 将裁剪空间坐标转换为屏幕UV（0~1范围）
                // Screen Space - Overlay 模式下，SV_POSITION.xy 是屏幕像素坐标
                // 需要先做透视除法，再映射到 0~1
                float2 ndc = (OUT.vertex.xy / OUT.vertex.w) * 0.5 + 0.5;
                OUT.screenUV = ndc;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 mainTex = tex2D(_MainTex, IN.texcoord);
                fixed4 finalColor = IN.color * mainTex * _Color;

                // 获取当前像素的屏幕UV坐标
                float2 uv = IN.screenUV;

                // 计算到矩形四条边的距离
                float leftDist   = uv.x - _RectMinX;
                float rightDist  = _RectMaxX - uv.x;
                float bottomDist = uv.y - _RectMinY;
                float topDist    = _RectMaxY - uv.y;

                // 在矩形内部 = 所有距离都 > 0
                float insideRect = step(0, leftDist) * step(0, rightDist)
                                 * step(0, bottomDist) * step(0, topDist);

                // 计算到最近边的距离（内部为正，外部为负）
                float minDist = min(min(leftDist, rightDist), min(bottomDist, topDist));

                // Alpha值：矩形内部完全透明（alpha=0），外部为遮罩Alpha，边缘渐变过渡
                // 在矩形内部：minDist > 0，smoothstep 输出 0 → alpha = 0
                // 在矩形外部：minDist < 0，smoothstep 输出 1 → alpha = _Color.a
                // 在边缘附近：0~_EdgeSoftness 范围内渐变
                float edgeAlpha = 1 - smoothstep(-_EdgeSoftness, _EdgeSoftness, minDist);
                float alpha = edgeAlpha * _Color.a;

                // 如果高亮区域不可交互，高亮区域 alpha 设为 0.1 而非 0（视觉上半透明可见）
                if (_HighlightInteractable < 0.5 && insideRect > 0.5)
                {
                    alpha = 0.1;
                }

                finalColor.a = alpha;
                finalColor.rgb *= finalColor.a;   // premultiply alpha 防止边缘光晕

                // 调试模式：显示高亮区域的轮廓
                if (_ShowHighlight > 0.5)
                {
                    float outlineWidth = _EdgeSoftness * 2;
                    float outline = 1 - smoothstep(0, outlineWidth, abs(minDist));
                    if (outline > 0.5)
                    {
                        finalColor.rgb = fixed3(1, 0, 0);  // 红色轮廓
                        finalColor.a = 1;
                    }
                }

                return finalColor;
            }
            ENDCG
        }
    }
}