Shader "Custom/NeonBall2D"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite",2D)="white"{}
        _Color("Sprite Tint", Color) = (1,1,1,1)

        // Core colors
        [HDR]_GlowColor("Inner Glow Color", Color) = (0.2, 0.8, 1.4, 1)
        [HDR]_RimColor ("Rim Color", Color)       = (0.6, 1.0, 1.0, 1)
        [HDR]_SweepColor("Sweep Color", Color)    = (1.2, 1.2, 1.2, 1)

        // Glow & rim shaping
        _GlowStrength("Glow Strength", Range(0,3)) = 1.2
        _GlowRadius  ("Glow Radius",   Range(0,1)) = 0.55
        _RimStrength ("Rim Strength",  Range(0,3)) = 1.0
        _RimWidth    ("Rim Width",     Range(0.001,0.5)) = 0.12
        _RimSoftness ("Rim Softness",  Range(0,1)) = 0.5

        // Animated sweep highlight
        _SweepStrength("Sweep Strength", Range(0,3)) = 0.8
        _SweepWidth   ("Sweep Width",    Range(0.05,1)) = 0.28
        _SweepSpeed   ("Sweep Speed",    Range(-4,4)) = 1.2
        _SweepAngleDeg("Sweep Angle (deg)", Range(0,180)) = 40

        // Optional outline
        [HDR]_EdgeColor("Edge Color", Color) = (1,1,1,0.15)
        _EdgeWidth("Edge Width", Range(0,0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _GlowColor, _RimColor, _SweepColor, _EdgeColor;
            float _GlowStrength, _GlowRadius;
            float _RimStrength, _RimWidth, _RimSoftness;
            float _SweepStrength, _SweepWidth, _SweepSpeed, _SweepAngleDeg;
            float _EdgeWidth;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;   // atlas uv
                float2 uv01  : TEXCOORD1;   // 0..1 rect uv
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv01  = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // Smooth band helper: returns 0..1 inside [edge - width .. edge]
            float rimBand(float x, float edge, float width, float softness)
            {
                float inner = edge - width;
                float t = saturate((x - inner) / max(width, 1e-5));
                // softness curves the edge
                return pow(t, lerp(1.0, 2.5, softness));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseTex = tex2D(_MainTex, i.uv) * i.color;
                float  alpha   = baseTex.a;
                fixed3 col     = baseTex.rgb;

                // Map to centered quad space: (-1..1)
                float2 p = i.uv01 * 2.0 - 1.0;
                // Radial distance from center (treat like a ball even if sprite is square)
                float r = length(p);

                // -------- Inner glow (strongest in the center, fades toward radius) --------
                // Build a soft falloff from center: 1 at r=0 down to 0 at r>=_GlowRadius
                float glowMask = 1.0 - smoothstep(0.0, max(_GlowRadius, 1e-5), r);
                col += _GlowColor.rgb * (glowMask * _GlowStrength);

                // -------- Rim light (bright near the edge of the disc) --------
                // Edge at r≈1 for a full quad; tighten slightly to avoid square corners
                float edge = 1.0;
                float rim = rimBand(r, edge, _RimWidth, _RimSoftness);
                col += _RimColor.rgb * (rim * _RimStrength);

                // -------- Animated sweep highlight --------
                if (_SweepStrength > 0.0001)
                {
                    // Build a rotating axis using time and angle
                    float a = radians(_SweepAngleDeg);
                    float2 dir = normalize(float2(cos(a), sin(a)));

                    // Rotate sweep along dir and animate offset with time
                    float phase = _Time.y * _SweepSpeed; // _Time.y ≈ t*2
                    // Signed coordinate along the sweep direction
                    // -------- Animated sweep highlight (wrapped) --------
                    if (_SweepStrength > 0.0001)
                    {
                        float a = radians(_SweepAngleDeg);
                        float2 dir = normalize(float2(cos(a), sin(a)));

                        // range along the quad in dir is ~[-1..1] (length ~= 2.0)
                        // Wrap the sweep center into [-1..1] so it keeps looping.
                        float t = _Time.y * _SweepSpeed; // seconds * speed
                        float sweepCenter = frac(t * 0.5) * 2.0 - 1.0;   // loops -1 -> +1 (one-way)
                        // If you prefer ping-pong (back and forth), use:
                        // float sweepCenter = 1.0 - 2.0 * abs(frac(t * 0.5) - 0.5);

                        // Signed coord along sweep axis, centered on wrapped position
                        float s = dot(p, dir) - sweepCenter;

                        float w2 = max(_SweepWidth*_SweepWidth, 1e-6);
                        float sweepMask = exp(-(s*s) / w2);

                        col += _SweepColor.rgb * (sweepMask * _SweepStrength);
                    }
                }

                // -------- Optional thin edge outline (nice with bloom) --------
                if (_EdgeWidth > 0.0001 && _EdgeColor.a > 0.0001)
                {
                    float edgeMask = smoothstep(1.0 - _EdgeWidth*2.0, 1.0, r);
                    col = lerp(col, _EdgeColor.rgb, _EdgeColor.a * edgeMask);
                }

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
