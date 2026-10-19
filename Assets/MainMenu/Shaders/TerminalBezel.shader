Shader "Silent Suns/Terminal Bezel"
{
    Properties
    {
        _BaseColor ("Housing color", Color) = (.16,.17,.155,1)
        _TerminalTexture ("Live terminal", 2D) = "black" {}
        _TextLightTexture ("Text emission", 2D) = "black" {}
        _Power ("Screen illumination", Float) = 0
        _SpillStrength ("Screen bounce", Range(0,12)) = 8
        _ReflectionStrength ("Soft text reflection", Range(0,2)) = .16
        _TextScatter ("Bright content scatter", Range(0,8)) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_TerminalTexture); SAMPLER(sampler_TerminalTexture);
            TEXTURE2D(_TextLightTexture); SAMPLER(sampler_TextLightTexture);
            float4 _TerminalTexture_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Power, _SpillStrength, _ReflectionStrength, _TextScatter;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionOS : TEXCOORD2; float3 normalOS : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half3 SoftScreen(float2 uv, float2 radius, float mip)
            {
                half3 color = 0;
                float total = 0;
                [unroll] for (int y = -1; y <= 1; y++)
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float weight = (x == 0 ? 2 : 1) * (y == 0 ? 2 : 1);
                        color += SAMPLE_TEXTURE2D_LOD(_TerminalTexture,sampler_TerminalTexture,saturate(uv + float2(x,y)*radius),mip).rgb * weight;
                        total += weight;
                    }
                return color / total;
            }
            half3 ScreenBounce(float3 surfacePosition, float3 normal)
            {
                half3 color = 0;
                float total = 0;
                // Overlapping mip footprints integrate the whole display, including distant typing.
                float mip = max(0,log2(max(_TerminalTexture_TexelSize.z,1) / 8));
                [unroll] for (int y = 0; y < 5; y++)
                    [unroll] for (int x = 0; x < 8; x++)
                    {
                        float2 uv = (float2(x,y)+.5) / float2(8,5);
                        float3 delta = float3((uv-.5)*float2(16,9),.55) - surfacePosition;
                        float distanceSquared = dot(delta,delta);
                        float weight = rcp(1 + distanceSquared * .065);
                        float facing = saturate(dot(normal,delta * rsqrt(max(distanceSquared,.001))));
                        half3 radiance = SAMPLE_TEXTURE2D_LOD(_TerminalTexture,sampler_TerminalTexture,uv,mip).rgb;
                        // Raise the diffuse contribution of white phosphors without raising the blue field.
                        // A small indirect term carries the reveal's reflected light onto the face.
                        color += radiance * weight * (.06 + .94 * facing);
                        total += weight;
                    }
                return color / total;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalOS);
                float3 view = TransformWorldToObjectDir(GetWorldSpaceNormalizeViewDir(input.positionWS));
                float3 p = input.positionOS;
                Light room = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 color = _BaseColor.rgb * (SampleSH(normalize(input.normalWS)) + room.color * saturate(dot(input.normalWS,room.direction)) * room.shadowAttenuation);

                // A broad wash wraps the four sides, tapering naturally across the rounded reveal.
                float2 size = float2(16,9);
                float distanceFromGlass = max(max(abs(p.x)-8,abs(p.y)-4.5),0);
                float falloff = exp2(-distanceFromGlass*2.8);
                half3 bounce = ScreenBounce(p,normal);
                color += bounce * _BaseColor.rgb * _SpillStrength * falloff * _Power;

                // Broad frosted reflections preserve where text is appearing without printing
                // hard letter silhouettes on the housing. Each edge samples into the picture.
                float2 edgeUV = p.xy / size + .5;
                bool side = abs(p.x) / 8 > abs(p.y) / 4.5;
                float2 projected = saturate(edgeUV);
                if (side) projected.x = p.x < 0 ? .18 : .82;
                else projected.y = p.y < 0 ? .18 : .82;
                float blurMip = max(0, log2(max(_TerminalTexture_TexelSize.z,1) / 64));
                half3 nearby = SoftScreen(projected, side ? float2(.15,.045) : float2(.045,.15), blurMip);
                float lightMip = max(0, log2(max(_TerminalTexture_TexelSize.z,1) / 12));
                float whiteLight = SAMPLE_TEXTURE2D_LOD(_TextLightTexture,sampler_TextLightTexture,projected,lightMip).r;
                float averageLight = SAMPLE_TEXTURE2D_LOD(_TextLightTexture,sampler_TextLightTexture,float2(.5,.5),lightMip + 5).r;
                float reveal = exp2(-distanceFromGlass * 3.5);
                color += (nearby * _BaseColor.rgb * .06 + (whiteLight * .38 + averageLight * .65) * _TextScatter) * reveal * _Power;

                // Reflect the screen plane into the sloping lip, then soften the letters for rough plastic.
                float3 reflected = reflect(-view,normal);
                float distanceAlongRay = (.55-p.z) / max(reflected.z,.0001);
                float2 reflectionUV = (p.xy + reflected.xy * distanceAlongRay) / size + .5;
                float edge = min(min(reflectionUV.x,1-reflectionUV.x),min(reflectionUV.y,1-reflectionUV.y));
                float visible = smoothstep(-.08,.15,edge) * step(.001,reflected.z) * step(0,distanceAlongRay);
                float roughness = .065 + min(distanceAlongRay,8) * .006;
                float mip = max(0,log2(max(_TerminalTexture_TexelSize.z,1) / 24));
                half3 reflection = SoftScreen(reflectionUV,float2(roughness,roughness*1.4),mip);
                float fresnel = .12 + .88 * pow(1-saturate(dot(normal,view)),5);
                color += reflection * visible * fresnel * _ReflectionStrength * falloff * _Power;
                // Fixed fine material variation belongs to the plastic, not animated screen noise.
                float grain = frac(sin(dot(floor(p.xy*950),float2(12.9898,78.233))) * 43758.5453);
                color *= .97 + grain * .06;
                return half4(color,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
