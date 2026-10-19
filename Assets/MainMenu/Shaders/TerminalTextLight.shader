Shader "Silent Suns/Terminal Text Light"
{
    Properties { _MainTex ("Terminal", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float White(float2 uv)
            {
                float3 c = tex2D(_MainTex, uv).rgb;
                return min(c.r,min(c.g,c.b));
            }
            float4 Frag(v2f_img input) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * 5;
                float surround = (White(input.uv + float2(d.x,0)) + White(input.uv - float2(d.x,0)) +
                    White(input.uv + float2(0,d.y)) + White(input.uv - float2(0,d.y))) * .25;
                // Isolate luminous strokes before blur; broad white panels must not dominate.
                float text = max(White(input.uv) - surround, 0);
                return float4(text,text,text,1);
            }
            ENDHLSL
        }
    }
}
