// Procedural cursed-energy shader for beams, aura rings and slash smears (built-in pipeline, no textures).
// - Scrolling value noise along the stroke (U) gives the flowing, boiling look.
// - Across the stroke (V) the edges fall off softly and the centre burns white-hot.
// - Vertex alpha is a dissolve control: as a stroke fades, the noise eats it away instead of a flat fade.
Shader "ProjectSorcery/Energy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scroll ("Scroll speed", Float) = 4
        _NoiseScale ("Noise scale", Float) = 2.5
        _Hot ("White-hot core", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Scroll;
            float _NoiseScale;
            float _Hot;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float hash21 (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = float2(i.uv.x * _NoiseScale - _Time.y * _Scroll, i.uv.y * 3.0);
                float n = vnoise(p) * 0.6 + vnoise(p * 2.3 + float2(7.1, 3.7)) * 0.4;
                float edge = 1.0 - abs(i.uv.y * 2.0 - 1.0);              // 0 at the rims, 1 on the centre line
                float body = smoothstep(0.0, 0.7, edge);
                float dissolve = 1.0 - i.color.a;                        // fading strokes get eaten by the noise
                float mask = smoothstep(dissolve - 0.05, dissolve + 0.2, n * body + body * 0.35);
                float hot = smoothstep(0.72, 1.0, edge) * _Hot;
                fixed3 col = lerp(i.color.rgb, fixed3(1.0, 1.0, 1.0), hot);
                float a = mask * (0.35 + 0.65 * n) * saturate(i.color.a * 1.5);
                return fixed4(col * (0.7 + n * 0.8), a);
            }
            ENDCG
        }
    }
}
