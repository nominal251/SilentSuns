Shader "Silent Suns/Ship Screen Spill"
{
    Properties
    {
        _TerminalTexture ("Boot screen", 2D) = "black" {}
        _TextLightTexture ("Phosphor light", 2D) = "black" {}
        _Power ("Boot light", Float) = 1
        _Invitation ("Invitation light", Float) = 0
        _ContentFit ("Content height", Float) = 1
        _LightMip ("Diffuse light mip", Float) = 6
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest LEqual Blend One One
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_TerminalTexture); SAMPLER(sampler_TerminalTexture);
            TEXTURE2D(_TextLightTexture); SAMPLER(sampler_TextLightTexture);
            float4 _ScreenCenter, _ScreenRight, _ScreenUp;
            float _Power, _LightMip, _ContentFit, _Invitation;
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; };
            V Vert(A a) { V v; v.world=TransformObjectToWorld(a.p.xyz); v.p=TransformWorldToHClip(v.world); v.normal=TransformObjectToWorldNormal(a.n); v.uv=a.uv; return v; }
            half4 Frag(V v):SV_Target
            {
                float3 delta=v.world-_ScreenCenter.xyz;
                float2 p=float2(dot(delta,_ScreenRight.xyz)/_ScreenRight.w,dot(delta,_ScreenUp.xyz)/_ScreenUp.w);
                float edge=max(max(abs(p.x)-1,abs(p.y)-1),0);
                float3 front=-cross(_ScreenRight.xyz,_ScreenUp.xyz);
                float depth=abs(dot(delta,front));
                float falloff=exp2(-edge*2.8-depth*10);
                half3 blue=0; float white=0;
                // Integrate luminous strokes from the whole display; never project sharp letter shapes.
                [unroll] for(int y=0;y<3;y++) [unroll] for(int x=0;x<4;x++)
                {
                    float2 uv=(float2(x,y)+.5)/float2(4,3);
                    float weight=rsqrt(1+dot(uv*2-1-p,uv*2-1-p));
                    blue+=SAMPLE_TEXTURE2D_LOD(_TerminalTexture,sampler_TerminalTexture,uv,_LightMip).rgb*weight;
                    white+=SAMPLE_TEXTURE2D_LOD(_TextLightTexture,sampler_TextLightTexture,uv,_LightMip).r*(.4+.6*weight);
                }
                // A broad reflected wash follows luminous text even on the far edges.
                half3 reflected=(blue*.05+white*.43)*falloff;
                reflected+=half3(.002,.012,.065)*_Invitation*exp2(-edge*9-depth*12);
                reflected*=_Power;
                return half4(reflected,0);
            }
            ENDHLSL
        }
    }
}
