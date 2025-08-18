Shader "Custom/BorderGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1) // SpriteRenderer overrides this
        _RimColor("Rim Color", Color) = (1, 0.6, 0.2, 1)
        _RimThickness("Rim Thickness", Range(0.0, 0.5)) = 0.1
        _RimIntensity("Rim Intensity", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _RimColor;
            float _RimThickness;
            float _RimIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color; // SpriteRenderer tint
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Base fill (sprite texture + SpriteRenderer tint)
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // Distance to nearest edge in UV space
                float edgeDist = min(min(i.uv.x, 1.0 - i.uv.x),
                                     min(i.uv.y, 1.0 - i.uv.y));

                // Smooth rim glow near edges
                float rim = smoothstep(_RimThickness, 0.0, edgeDist);

                // Additive glow
                baseCol.rgb = lerp(baseCol.rgb, _RimColor.rgb, rim * _RimIntensity);

                return baseCol;
            }
            ENDCG
        }
    }
}
