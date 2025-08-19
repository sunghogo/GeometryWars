Shader "Custom/GlowSprite"
{
    Properties
    {
        // SpriteRenderer-friendly properties
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        // Style controls
        _BaseColor     ("Base Color", Color) = (0.2, 0.9, 1.0, 1)
        _OutlineColor  ("Outline Color", Color) = (0.0, 1.0, 1.0, 1)
        _OutlineThickness ("Outline Thickness (px)", Range(0.5, 4)) = 1.0
        _SoftEdge      ("Edge Softness", Range(0, 1)) = 0.25

        // Glow / Pulse
        _GlowColor     ("Glow Color", Color) = (0.0, 1.0, 1.0, 1)
        _GlowStrength  ("Glow Strength", Range(0, 5)) = 1.5
        _PulseAmount   ("Pulse Amount", Range(0, 2)) = 0.75
        _PulseSpeed    ("Pulse Speed", Range(0, 10)) = 3.0

        // Hit flash (drive from script)
        _HitColor      ("Hit Flash Color", Color) = (1,1,1,1)
        _HitAmount     ("Hit Amount", Range(0,1)) = 0

        // Optional UV wiggle
        _DistortAmount ("Glow Distort Amount", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // x=1/width, y=1/height

            fixed4 _Color;

            fixed4 _BaseColor;
            fixed4 _OutlineColor;
            float  _OutlineThickness;
            float  _SoftEdge;

            fixed4 _GlowColor;
            float  _GlowStrength;
            float  _PulseAmount;
            float  _PulseSpeed;

            fixed4 _HitColor;
            float  _HitAmount;

            float  _DistortAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR; // from SpriteRenderer
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color * _Color; // SR tint * material tint
                return o;
            }

            // Expand alpha via neighbor taps to make an outline
            float outlineMask(sampler2D tex, float2 uv, float thicknessPx)
            {
                float2 ts = _MainTex_TexelSize.xy;
                float2 step = ts * thicknessPx;

                // 8-neighborhood taps
                float a  = tex2D(tex, uv).a;
                float a1 = tex2D(tex, uv + float2( step.x, 0)).a;
                float a2 = tex2D(tex, uv + float2(-step.x, 0)).a;
                float a3 = tex2D(tex, uv + float2(0,  step.y)).a;
                float a4 = tex2D(tex, uv + float2(0, -step.y)).a;
                float a5 = tex2D(tex, uv + float2( step.x,  step.y)).a;
                float a6 = tex2D(tex, uv + float2(-step.x,  step.y)).a;
                float a7 = tex2D(tex, uv + float2( step.x, -step.y)).a;
                float a8 = tex2D(tex, uv + float2(-step.x, -step.y)).a;

                float expanded = saturate(max(a, max(max(a1,a2), max(a3,a4))));
                expanded = max(expanded, max(max(a5,a6), max(a7,a8)));

                // outline = expanded edge minus original fill
                float edge = saturate(expanded - a);

                // soften edge a bit
                edge = saturate(edge / max(1e-5, _SoftEdge + 1e-5));
                return edge;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Optional subtle UV wobble for glow layer
                float t = _Time.y * _PulseSpeed;
                float2 wobble = (sin(float2(t*1.73, t*2.11)) * 0.5 + 0.5) * _DistortAmount * _MainTex_TexelSize.xy * 4.0;

                // Base sample (white sprite recommended)
                fixed4 texCol = tex2D(_MainTex, i.uv);
                float a = texCol.a * i.color.a;

                // Base body color (uses white sprite so RGB comes from material)
                fixed3 baseRGB = (_BaseColor.rgb * i.color.rgb);

                // Radial-ish pulse (centers in UV space)
                float2 uvCentered = (i.uv - 0.5);
                float radial = 1.0 - saturate(length(uvCentered) * 2.0); // center=1, edge=0
                float pulse = (sin(t) * 0.5 + 0.5) * _PulseAmount;
                float innerGlow = saturate(radial * (0.4 + pulse));

                // Glow layer (distorted sample to keep it lively)
                float aWobble = tex2D(_MainTex, i.uv + wobble).a;
                float glowMask = saturate(max(a, aWobble)) * innerGlow;

                // Outline
                float edge = outlineMask(_MainTex, i.uv, _OutlineThickness);

                // Compose colors
                fixed3 rgb = baseRGB;
                rgb += _GlowColor.rgb * glowMask * _GlowStrength;

                // Add a crisp neon outline (alpha not added to fill)
                rgb = lerp(rgb, _OutlineColor.rgb, edge);

                // Hit flash (overrides tint briefly)
                rgb = lerp(rgb, _HitColor.rgb, _HitAmount);

                // Final alpha = base alpha + outline alpha (clamped)
                float outA = saturate(a + edge * saturate(_OutlineColor.a));

                return fixed4(rgb, outA);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
