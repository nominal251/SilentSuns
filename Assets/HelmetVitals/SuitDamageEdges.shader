Shader "Silent Suns/Suit Damage Edges"
{
    Properties
    {
        _Shadow ("Shadow ink", Color) = (0.075, 0.063, 0.106, 1)
        _Spread ("Corner reach", Range(0.18, 0.50)) = 0.36
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Shadow;
            float _Strength, _Impact, _Aspect, _Spread, _Critical, _Pulse;
            Varyings vert(appdata_base input)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(input.vertex);
                o.uv = input.texcoord.xy;
                return o;
            }
            // A stationary four-by-four ordered screen breaks up the ink at its boundary.
            float bayer2(float2 p)
            {
                float2 bit = fmod(p, 2.0);
                return 2.0 * bit.x + 3.0 * bit.y - 4.0 * bit.x * bit.y;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 size = float2(_Aspect, 1.0);
                float2 p = floor(input.uv * size * 480);
                float2 edge = min(input.uv, 1.0 - input.uv) * size;
                float distance = edge.x + 0.86 * edge.y;
                float reach = _Spread * (0.38 + 0.84 * _Strength + 0.28 * _Impact);
                float corner = saturate(1.0 - distance / reach);
                float rim = saturate(1.0 - min(edge.x, edge.y) / (0.013 + 0.038 * _Strength));
                float density = max(corner, rim * 0.35);
                float dither = (4.0 * bayer2(p) + bayer2(floor(p / 2.0)) + 0.5) / 16.0;
                float steps = floor(density * 5.0 + dither) / 5.0;
                float shadow = steps * saturate(_Strength * 0.81 + _Impact * 0.39);
                // Loss of peripheral vision: the shaded area moves inward on each low-health beat.
                float2 fromCenter = abs(input.uv * 2.0 - 1.0);
                float radius = lerp(length(fromCenter), max(fromCenter.x, fromCenter.y), 0.45);
                float periphery = smoothstep(0.26 - 0.12 * _Pulse, 1.13 - 0.22 * _Pulse, radius);
                float veil = saturate(_Critical * (0.15 + 1.08 * periphery + _Pulse * (0.10 + 0.40 * periphery)));
                shadow += (1.0 - shadow) * veil;
                return half4(_Shadow.rgb, shadow);
            }
            ENDCG
        }
    }
}
