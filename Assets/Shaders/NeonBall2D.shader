Shader "Custom/BulletNeon"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData]_Color("Tint", Color) = (1,1,1,1)

        // Look & feel
        _CoreColor   ("Core Color", Color) = (1,1,1,1)
        _GlowColor   ("Glow Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _EdgeColor   ("Edge Color", Color) = (0.2, 0.9, 1.0, 1.0)

        _CoreRadius  ("Core Radius", Range(0.0, 1.0)) = 0.25
        _Feather     ("Feather", Range(0.001, 1.0)) = 0.25
        _GlowMul     ("Glow Strength", Range(0.0, 6.0)) = 2.0

        // Streak / tail aligned to velocity
        _TailLen     ("Tail Length", Range(0.0, 2.0)) = 1.0
        _TailPower   ("Tail Sharpness", Range(0.5, 6.0)) = 2.0

        // Sparkle
        _TwinkleAmt  ("Twinkle Amount", Range(0.0, 1.0)) = 0.25
        _TwinkleSpd  ("Twinkle Speed", Range(0.0, 20.0)) = 6.0

        // Set per-bullet from script
        _Dir         ("Direction XY (normalized)", Vector) = (1,0,0,0)
        _SpeedMag    ("Speed Magnitude", Float) = 1.0
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        ZWrite Off
        Blend One One // additive for hot neon

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;

            fixed4 _CoreColor;
            fixed4 _GlowColor;
            fixed4 _EdgeColor;

            float _CoreRadius;
            float _Feather;
            float _GlowMul;

            float _TailLen;
            float _TailPower;

            float _TwinkleAmt;
            float _TwinkleSpd;

            float4 _Dir;      // xy used
            float  _SpeedMag; // optional, for subtle boost

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                fixed4 col : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.col = v.color * _Color;
                return o;
            }

            // Cheap hash for twinkle
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample sprite alpha as mask (use white sprite)
                fixed4 texCol = tex2D(_MainTex, i.uv);
                float a = texCol.a * i.col.a;
                if (a <= 0) discard;

                // UV in -1..1 space, X horizontal, Y vertical (centered)
                float2 uv = i.uv * 2.0 - 1.0;

                // Core: circular falloff
                float r = length(uv);
                float core = smoothstep(_CoreRadius, _CoreRadius - _Feather, r);

                // Velocity-aligned tail: project uv onto direction
                float2 dir = normalize(_Dir.xy + 1e-5);
                float along = dot(uv, dir);      // forward axis
                float perp  = dot(uv, float2(-dir.y, dir.x)); // sideways axis

                // Tail is bright in front, stretches backward (negative along)
                float tail = saturate(1.0 - (perp*perp));
                float back = saturate(-along * _TailLen);
                tail = pow(tail * back, _TailPower);

                // Edge highlight (rim)
                float rim = smoothstep(1.0, 1.0 - _Feather, r);

                // Twinkle (subtle flicker)
                float t = _Time.y * _TwinkleSpd;
                float tw = (hash21(floor(uv * 7.0 + t)) * 2.0 - 1.0) * _TwinkleAmt;

                // Compose
                float glow = (1.0 - core) + tail * 1.5 + rim * 0.25 + tw;
                glow = max(glow, 0.0);

                // Speed can modestly boost intensity
                float boost = saturate(_SpeedMag * 0.1 + 0.9);

                fixed3 col =
                    _CoreColor.rgb * (1.0 - core) * 2.0 +
                    _GlowColor.rgb * glow * _GlowMul * boost;

                // Add a hint of edge color
                col = lerp(col, _EdgeColor.rgb, rim * 0.35);

                // Additive output uses alpha as intensity gate from source sprite
                return fixed4(col * a, a);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
