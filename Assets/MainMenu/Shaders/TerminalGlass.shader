Shader "Silent Suns/Terminal Glass"
{
    Properties
    {
        _Reflection ("Reflection strength", Range(0,1)) = 0.045
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Reflection;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light room = GetMainLight();
                // Broad room-light reflection follows the convex mesh. Bezel fill lights stay out of the glass.
                float3 halfDirection = normalize(room.direction + view);
                float broad = pow(saturate(dot(normal, halfDirection)), 12);
                float fresnel = .04 + .96 * pow(1 - saturate(dot(normal,view)),5);
                half3 reflection = room.color * broad * _Reflection * (1 + fresnel * 4);
                return half4(reflection, .008);
            }
            ENDHLSL
        }
    }
}
