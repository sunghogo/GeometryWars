Shader "Custom/BreakoutPaddle"
{
    Properties
    {
        [PerRendererData] _MainTex        ("Sprite (usually white)", 2D) = "white" {}
        _Color                              ("Sprite Tint (SpriteRenderer)", Color) = (1,1,1,1)

        // Vibe (similar to your block shader)
        _TopColor                           ("Top Gradient Color", Color) = (1,1,1,1)
        _TopStrength                        ("Top Gradient Strength", Range(0,1)) = 0.35

        _BevelPixels                        ("Bevel Width (px)", Range(0,16)) = 4
        _BevelIntensity                     ("Bevel Intensity", Range(0,2)) = 0.6

        [HDR]_RimColor                      ("Rim Color", Color) = (0.6,1,1,1)
        _RimPixels                          ("Rim Width (px)", Range(0,12)) = 3
        _RimSoftness                        ("Rim Softness", Range(0,1)) = 0.5
        _RimIntensity                       ("Rim Intensity", Range(0,3)) = 1.0

        [HDR]_OutlineColor                  ("Outline Color", Color) = (0,0,0,0.6)
        _OutlinePixels                      ("Outline Width (px)", Range(0,8)) = 2

        _LightDir                           ("Bevel Light Dir (XY)", Vector) = (-1, 1, 0, 0)

        // Paddle-specific sauce
        _VelX                               ("Paddle Vel X (units/s)", Float) = 0
        [HDR]_VelGlowColor                  ("Velocity Glow Color", Color) = (0.2, 0.9, 1.8, 1)
        _VelGlowStrength                    ("Velocity Glow Strength", Range(0,2)) = 0.8
        _VelGlowPixels                      ("Velocity Glow Width (px)", Range(0,16)) = 5

        // Hit pulse (set from code, fades quickly)
        [HDR]_PulseColor                    ("Pulse Color", Color) = (1,0.95,0.6,1)
        _PulseStrength                      ("Pulse Strength", Range(0,2)) = 0
        _PulseTightness                     ("Pulse Tightness", Range(0.5,4)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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

            fixed4 _TopColor;       float _TopStrength;
            float _BevelPixels;     float _BevelIntensity;
            fixed4 _RimColor;       float _RimPixels;     float _RimSoftness; float _RimIntensity;
            fixed4 _OutlineColor;   float _OutlinePixels;
            float4 _LightDir;

            // Paddle extras
            float   _VelX;
            fixed4  _VelGlowColor;  float _VelGlowStrength; float _VelGlowPixels;

            fixed4  _PulseColor;    float _PulseStrength;  float _PulseTightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;   // atlas-corrected
                float2 uv01  : TEXCOORD1;   // 0..1 rect
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv01  = v.uv;
                o.color = v.color * _Color; // SpriteRenderer tint
                return o;
            }

            float PixelsToUV(float px, float2 uv01)
            {
                float ux = fwidth(uv01.x);
                float uy = fwidth(uv01.y);
                float uvPerPx = max(ux, uy);
                return max(px * uvPerPx, 1e-6);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                // Base fill = sprite * SR tint
                fixed3 baseCol = tex.rgb;
                float  alpha   = tex.a;

                // Distances to edges
                float2 dEdge = min(i.uv01, 1.0 - i.uv01);
                float  dMin  = min(dEdge.x, dEdge.y);

                // ----- Outline -----
                float uvOutline   = PixelsToUV(_OutlinePixels, i.uv01);
                float outlineMask = 1.0 - smoothstep(uvOutline, uvOutline * 1.5, dMin);

                // ----- Rim -----
                float uvRim = PixelsToUV(_RimPixels, i.uv01);
                float rim   = 1.0 - smoothstep(uvRim, uvRim * (1.0 + _RimSoftness), dMin);
                rim = saturate(rim) * _RimIntensity;

                // ----- Bevel lighting (fake normal from nearest edge) -----
                float useX = step(dEdge.x, dEdge.y);
                float nx = useX * (i.uv01.x < 0.5 ? -1.0 : 1.0);
                float ny = (1.0 - useX) * (i.uv01.y < 0.5 ? -1.0 : 1.0);
                float2 n2 = normalize(float2(nx, ny));
                float2 L  = normalize(_LightDir.xy);
                float NdotL = dot(n2, L); // [-1..1]

                float uvBevel    = PixelsToUV(_BevelPixels, i.uv01);
                float bevelMask  = 1.0 - smoothstep(uvBevel, uvBevel * 1.5, dMin);
                float bevelLight = lerp(0.5, 0.5 + 0.5 * NdotL, _BevelIntensity);

                // ----- Top gradient -----
                float t = saturate(i.uv01.y);
                fixed3 gradCol = lerp(baseCol, _TopColor.rgb, _TopStrength * t);

                // Start composing
                fixed3 col = gradCol;

                // Bevel
                col = lerp(col, col * (bevelLight * 1.1), bevelMask);

                // Rim
                col += _RimColor.rgb * rim;

                // ----- Velocity-based leading-edge glow -----
                float minVel = 0.1;
                float side = (_VelX >  minVel) ? 1.0 : (_VelX < -minVel) ? 0.0 : 0.5; // 0=left, 1=right

                float uvx = i.uv01.x; // or add the _FlipX switch if needed
                float leadingDist = lerp(uvx, 1.0 - uvx, side);

                float uvVel   = PixelsToUV(_VelGlowPixels, i.uv01);
                float velMask = (side == 0.5) ? 0.0 : (1.0 - smoothstep(uvVel, uvVel * 1.5, leadingDist));

                float velAmp = saturate((abs(_VelX) - minVel) / (12.0 - minVel)) * _VelGlowStrength;
                col += _VelGlowColor.rgb * (velMask * velAmp);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
