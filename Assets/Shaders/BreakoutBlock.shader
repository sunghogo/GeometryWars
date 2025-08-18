Shader "Custom/BreakoutBlock"
{
    Properties
    {
        // Sprite + base tint (SpriteRenderer multiplies this too)
        _MainTex        ("Sprite (usually white)", 2D) = "white" {}
        _Color          ("Sprite Tint", Color) = (1,1,1,1)

        // Core look
        _BaseColor      ("Base Color", Color) = (0.18,0.8,0.9,1)
        _TopColor       ("Top Gradient Color", Color) = (1,1,1,1)
        _TopStrength    ("Top Gradient Strength", Range(0,1)) = 0.35

        // Bevel (inner light/dark near edges)
        _BevelPixels    ("Bevel Width (px)", Range(0,16)) = 4
        _BevelIntensity ("Bevel Intensity", Range(0,2)) = 0.6

        // Rim glow (soft color band at edge)
        [HDR]_RimColor  ("Rim Color", Color) = (0.6,1,1,1)
        _RimPixels      ("Rim Width (px)", Range(0,12)) = 3
        _RimSoftness    ("Rim Softness", Range(0,1)) = 0.5
        _RimIntensity   ("Rim Intensity", Range(0,3)) = 1.0

        // Outline
        [HDR]_OutlineColor ("Outline Color", Color) = (0,0,0,0.6)
        _OutlinePixels     ("Outline Width (px)", Range(0,8)) = 2

        // 2D “light direction” for bevel (XY in world)
        _LightDir       ("Light Dir (World XY)", Vector) = ( -1, 1, 0, 0 )

        // --- Animated Sweep Highlight ---
        [HDR]_SweepColor  ("Sweep Color", Color) = (1.2,1.2,1.2,1)
        _SweepStrength     ("Sweep Strength", Range(0,3)) = 0.8
        _SweepWidth        ("Sweep Width (0..1)", Range(0.05,1)) = 0.28
        _SweepSpeed        ("Sweep Speed", Range(-4,4)) = 1.2
        _SweepAngleDeg     ("Sweep Angle (deg)", Range(0,180)) = 40
        _SweepPingPong     ("Sweep PingPong (0=loop,1=pingpong)", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _BaseColor;
            fixed4 _TopColor;
            float  _TopStrength;

            float  _BevelPixels;
            float  _BevelIntensity;

            fixed4 _RimColor;
            float  _RimPixels;
            float  _RimSoftness;
            float  _RimIntensity;

            fixed4 _OutlineColor;
            float  _OutlinePixels;

            float4 _LightDir; // xy used

            // Sweep uniforms
            fixed4 _SweepColor;
            float  _SweepStrength, _SweepWidth, _SweepSpeed, _SweepAngleDeg, _SweepPingPong;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;   // 0..1 sprite UV
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;    // for sampling (atlas corrected)
                float2 uv01  : TEXCOORD1;    // 0..1 sprite UV (for edge/sweep math)
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv01  = v.uv;              // normalized sprite rect
                o.color = v.color * _Color;
                return o;
            }

            // Helper: convert a desired pixel thickness to UV thickness using derivatives
            float PixelsToUV(float px, float2 uv01)
            {
                float ux = fwidth(uv01.x);
                float uy = fwidth(uv01.y);
                float uvPerPx = max(ux, uy);
                return max(px * uvPerPx, 1e-6);
            }

            // Simple Gaussian-ish band around x==0 with width parameter
            float SweepBand(float s, float width)
            {
                float w2 = max(width * width, 1e-6);
                return exp(-(s * s) / w2);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                // Base fill (solid color controlled by SpriteRenderer + _BaseColor)
                fixed3 baseCol = tex.rgb * _BaseColor.rgb;
                float  alpha   = tex.a * _BaseColor.a;

                // Distance to rectangle edges in UV space (0 at edge, 0.5 center)
                float2 dEdge = min(i.uv01, 1.0 - i.uv01);
                float  dMin  = min(dEdge.x, dEdge.y); // closest edge distance

                // ---------- Outline (tight band at very edge) ----------
                float uvOutline = PixelsToUV(_OutlinePixels, i.uv01);
                float outlineMask = 1.0 - smoothstep(uvOutline, uvOutline * 1.5, dMin);

                // ---------- Rim Glow (soft band a few pixels wide) ----------
                float uvRim = PixelsToUV(_RimPixels, i.uv01);
                float rim = 1.0 - smoothstep(uvRim, uvRim * (1.0 + _RimSoftness), dMin);
                rim = saturate(rim) * _RimIntensity;

                // ---------- Bevel Lighting ----------
                float useX = step(dEdge.x, dEdge.y); // 1 when x-edge is closer
                float nx = useX * (i.uv01.x < 0.5 ? -1.0 : 1.0);
                float ny = (1.0 - useX) * (i.uv01.y < 0.5 ? -1.0 : 1.0);
                float2 n2 = normalize(float2(nx, ny));
                float2 L = normalize(_LightDir.xy);
                float NdotL = dot(n2, L); // [-1..1]

                float uvBevel = PixelsToUV(_BevelPixels, i.uv01);
                float bevelMask = 1.0 - smoothstep(uvBevel, uvBevel * 1.5, dMin);
                float bevelLight = lerp(0.5, 0.5 + 0.5 * NdotL, _BevelIntensity);

                // ---------- Top Gradient ----------
                float t = saturate(i.uv01.y);
                fixed3 gradCol = lerp(baseCol, _TopColor.rgb, _TopStrength * t);

                // ---------- Compose base ----------
                fixed3 col = gradCol;
                col = lerp(col, col * (bevelLight * 1.1), bevelMask); // bevel
                col += _RimColor.rgb * rim;                           // rim

                // ---------- Animated Sweep (wrapped/ping-pong) ----------
                if (_SweepStrength > 0.0001)
                {
                    // Centered quad space
                    float2 p = i.uv01 * 2.0 - 1.0;

                    // Sweep axis from angle
                    float a = radians(_SweepAngleDeg);
                    float2 dir = normalize(float2(cos(a), sin(a)));

                    // time-based center position in [-1,1]
                    float tsec = _Time.y * _SweepSpeed; // _Time.y ~ t*2
                    float sweepCenter;
                    if (_SweepPingPong > 0.5)
                    {
                        // Ping-pong (-1 to +1 and back)
                        sweepCenter = 1.0 - 2.0 * abs(frac(tsec * 0.5) - 0.5);
                    }
                    else
                    {
                        // One-way wrap: loops -1 -> +1
                        sweepCenter = frac(tsec * 0.5) * 2.0 - 1.0;
                    }

                    // Signed coordinate along the sweep axis, centered on current sweep position
                    float s = dot(p, dir) - sweepCenter;

                    // Gaussian-ish band around the sweep line
                    float sweepMask = SweepBand(s, _SweepWidth);

                    // Additively tint with sweep color
                    col += _SweepColor.rgb * (sweepMask * _SweepStrength);
                }

                // ---------- Outline blend ----------
                col = lerp(col, _OutlineColor.rgb, _OutlineColor.a * outlineMask);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
