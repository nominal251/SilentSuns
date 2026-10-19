Shader "Silent Suns/Wake Pulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI texture", 2D) = "white" {}
        _Phase ("Ripple phase", Float) = 0
        _Pulse ("Core pulse", Float) = 0
        _Aspect ("Screen aspect", Float) = 1.25
        _Hover ("Pointer proximity", Float) = 0
        _ReducedMotion ("Still invitation", Float) = 0
        _Reveal ("Invitation fade", Float) = 0
        _Warming ("Raster charging", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Phase, _Pulse, _Aspect, _Hover, _ReducedMotion, _Reveal, _Warming;
            struct A { float4 position:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V v; v.position=TransformObjectToHClip(a.position.xyz); v.uv=a.uv; return v; }
            half4 Frag(V v):SV_Target
            {
                float2 p=(v.uv-float2(.5,.56))*float2(_Aspect,1);
                float r=length(p);
                float energy=.22+_Pulse*.78+_Hover*.24;
                float core=exp2(-r*r/(.00012+energy*.00010));
                float halo=exp2(-r*r/.0045);
                float haze=exp2(-r*r/.025);
                float waveRadius=.035+.61*pow(_Phase,.73);
                float ring=exp2(-pow((r-waveRadius)/(.008+_Phase*.018),2));
                float echo=exp2(-pow((r-waveRadius*.67)/.009,2));
                float waves=(ring*.25+echo*.07)*pow(1-_Phase,1.6)*(1-_ReducedMotion);
                half3 blue=half3(.025,.16,.55)*(halo*energy*.48+haze*energy*.055+waves);
                blue+=half3(.26,.58,1)*core*energy;
                blue*=_Reveal;
                blue+=half3(.001,.003,.018)*_Warming*exp2(-dot(p,p)*2.5);
                return half4(blue,1);
            }
            ENDHLSL
        }
    }
}
