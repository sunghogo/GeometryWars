Shader "Custom/AimCursor2D"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite (white)", 2D) = "white" {}
        _Color("Sprite Tint (from SpriteRenderer)", Color) = (1,1,1,1)

        [HDR]_CursorColor("Cursor Color", Color) = (0.2, 0.95, 1.2, 1)
        _Alpha("Overall Alpha", Range(0,1)) = 1

        // Ring
        _RingRadius("Ring Radius (0..1)", Range(0.05, 0.9)) = 0.45
        _RingThicknessPx("Ring Thickness (px)", Range(0.5, 10)) = 2
        _GlowWidthPx("Glow Width (px)", Range(0, 16)) = 6
        _GlowStrength("Glow Strength", Range(0, 3)) = 1

        // Dashes on ring
        _DashCount("Dash Count (0=off)", Range(0, 64)) = 12
        _DashFill("Dash Fill (0..1)", Range(0,1)) = 0.5
        _RotateSpeed("Dash Rotate Speed", Range(-10, 10)) = 1.5

        // Crosshair arms
        _CrossThicknessPx("Cross Thickness (px)", Range(0.5, 10)) = 2
        _CrossLength("Cross Length (0..1)", Range(0.05, 1)) = 0.85
        _GapRadius("Center Gap Radius (0..1)", Range(0, 0.5)) = 0.12

        // Animated sweep highlight across the ring
        _SweepStrength("Sweep Strength", Range(0, 3)) = 0.8
        _SweepWidth("Sweep Width (0..1)", Range(0.05, 0.6)) = 0.28
        _SweepAngleDeg("Sweep Angle (deg)", Range(0, 180)) = 30
        _SweepSpeed("Sweep Speed", Range(-5, 5)) = 1.2
        _SweepPingPong("Sweep PingPong (0=wrap,1=pingpong)", Range(0,1)) = 1

        // Optional pulse (you can animate this via script or animator)
        _Pulse("Pulse (additive)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _CursorColor; float _Alpha;

            float _RingRadius, _RingThicknessPx, _GlowWidthPx, _GlowStrength;
            float _DashCount, _DashFill, _RotateSpeed;
            float _CrossThicknessPx, _CrossLength, _GapRadius;
            float _SweepStrength, _SweepWidth, _SweepAngleDeg, _SweepSpeed, _SweepPingPong;
            float _Pulse;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float2 uv01  : TEXCOORD1; // 0..1 quad
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

            // Convert pixels to UV width using screen-space derivatives
            float PixelsToUV(float px, float2 uv01)
            {
                float ux = fwidth(uv01.x);
                float uy = fwidth(uv01.y);
                float uvPerPx = max(ux, uy);
                return max(px * uvPerPx, 1e-6);
            }

            // Smooth ring band around |r - R| < W
            float RingBand(float r, float R, float W)
            {
                float aa = fwidth(r) * 1.5;
                return 1.0 - smoothstep(W - aa, W + aa, abs(r - R));
            }

            // Rect band helper (width in UV)
            float RectBand(float dist, float halfWidth)
            {
                float aa = fwidth(dist) * 1.5;
                return 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, abs(dist));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample base sprite tint (lets you use any white sprite/quad)
                fixed4 baseTex = tex2D(_MainTex, i.uv) * i.color;

                // Centered quad space [-1,1]
                float2 p = i.uv01 * 2.0 - 1.0;
                float  r = length(p);

                // ---- Ring (with optional dashes) ----
                float ringWuv = PixelsToUV(_RingThicknessPx, i.uv01);
                float ringMask = RingBand(r, _RingRadius, ringWuv);

                // Dashes along angle
                if (_DashCount >= 1.0)
                {
                    float angle = atan2(p.y, p.x);               // [-pi, pi]
                    float u = angle / (2.0 * UNITY_PI) + 0.5;    // [0,1)
                    // rotate/scroll
                    u = frac(u + _Time.y * (_RotateSpeed * 0.1));
                    float seg = frac(u * _DashCount);
                    float dashMask = step(seg, _DashFill);       // 1 inside the filled portion
                    ringMask *= dashMask;
                }

                // ---- Crosshair arms (horizontal + vertical), with center gap ----
                float crossWuv = PixelsToUV(_CrossThicknessPx, i.uv01);
                float armLen = saturate(_CrossLength) * 1.0;     // up to quad edge
                float gap = _GapRadius;

                // Horizontal arm: |y| < thickness, and |x| in [gap, armLen]
                float horizCore = RectBand(p.y, crossWuv);
                float horizLen  = smoothstep(gap, gap + fwidth(p.x)*2.0, abs(p.x)) * (1.0 - smoothstep(armLen - fwidth(p.x)*2.0, armLen, abs(p.x)));
                float horizMask = horizCore * horizLen;

                // Vertical arm: |x| < thickness, and |y| in [gap, armLen]
                float vertCore  = RectBand(p.x, crossWuv);
                float vertLen   = smoothstep(gap, gap + fwidth(p.y)*2.0, abs(p.y)) * (1.0 - smoothstep(armLen - fwidth(p.y)*2.0, armLen, abs(p.y)));
                float vertMask  = vertCore * vertLen;

                float crossMask = saturate(horizMask + vertMask);

                // ---- Sweep highlight across ring ----
                float sweep = 0.0;
                if (_SweepStrength > 0.0001)
                {
                    float a = radians(_SweepAngleDeg);
                    float2 dir = normalize(float2(cos(a), sin(a)));
                    float tsec = _Time.y * _SweepSpeed;
                    float center;
                    if (_SweepPingPong > 0.5)
                        center = 1.0 - 2.0 * abs(frac(tsec * 0.5) - 0.5); // ping-pong [-1,1]
                    else
                        center = frac(tsec * 0.5) * 2.0 - 1.0;            // wrap [-1,1]

                    float s = dot(p, dir) - center;
                    float w2 = max(_SweepWidth * _SweepWidth, 1e-6);
                    float sweepBand = exp(-(s * s) / w2);                 // gaussian
                    // Apply only near the ring to keep it neat
                    float nearRing = smoothstep(_RingRadius - 0.1, _RingRadius, r) * (1.0 - smoothstep(_RingRadius, _RingRadius + 0.1, r));
                    sweep = sweepBand * nearRing * _SweepStrength;
                }

                // ---- Glow around ring ----
                float glowWuv = PixelsToUV(_GlowWidthPx, i.uv01);
                float glowMask = RingBand(r, _RingRadius, glowWuv) * _GlowStrength;

                // ---- Compose color/alpha ----
                float shapeMask = saturate(ringMask + crossMask);
                fixed3 cursorRGB = _CursorColor.rgb * (shapeMask + sweep) + _CursorColor.rgb * glowMask * 0.6;
                cursorRGB *= _Alpha;

                // Mix with base (keeps SpriteRenderer tint pipeline)
                // We output only the cursor (outside is transparent)
                float outAlpha = saturate(shapeMask * _Alpha + glowMask * 0.15 + _Pulse * 0.2);

                fixed3 finalRGB = cursorRGB;
                return fixed4(finalRGB, outAlpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
