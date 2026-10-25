Shader "Silent Suns/Terminal CRT"
{
    Properties
    {
        _ContentFit ("Content height", Float) = 1
        _ShipOverlay ("Cover the native instrument", Float) = 0
        _GameplayBlend ("Instrument handback", Float) = 0
        _Dormant ("Waiting for touch", Float) = 0
        _WakePulse ("Standby pulse", Float) = 0
        _HandoffFault ("Instrument sync", Float) = 0
        _SignalLevel ("Signal brightness", Float) = 1
        _RespectSourceAlpha ("Clip to panel", Float) = 0
        _Power ("Illumination", Float) = 0
        _MainTex ("Terminal", 2D) = "black" {}
        _PreviousTex ("Previous frame", 2D) = "black" {}
        _Curvature ("Glass curvature", Float) = 0.0025
        _ScanStrength ("Raster depth", Float) = 0.38
        _ScanLines ("Scan line count", Float) = 360
        _PhosphorStrength ("Shadow mask", Float) = 0.14
        _Glow ("Phosphor glow", Float) = 0.16
        _Persistence ("Persistence", Float) = 0.035
        _Vignette ("Corner falloff", Float) = 0.1
        _Convergence ("Edge convergence", Float) = 0.2
        _Noise ("Signal noise", Float) = 0.002
        _Flicker ("Luminance instability", Float) = 0.003
        _DisplayTime ("Display time", Float) = 0
        _InterferenceStrength ("Motion enabled", Float) = 1
        _Disturbance ("Local disturbance", Float) = 0
        _DiagnosticGlitch ("Diagnostic instability", Float) = 0
        _Warmup ("Power settling", Float) = 0
        _DisplayBrightness ("Overlay brightness", Float) = 0
        _DisplayContrast ("Overlay contrast", Float) = 1
        _ClearDisplay ("Settings and calibration", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_PreviousTex); SAMPLER(sampler_PreviousTex);
            float _ShipOverlay, _GameplayBlend, _Dormant, _WakePulse;
            float _HandoffFault, _SignalLevel;
            float4 _MainTex_TexelSize, _DisturbanceRegion;
            float _Power, _RespectSourceAlpha, _ContentFit;
            float _Curvature, _ScanStrength, _ScanLines, _PhosphorStrength, _Glow, _Persistence;
            float _Vignette, _Convergence, _Noise, _Flicker, _DisplayTime;
            float _InterferenceStrength, _Disturbance, _DiagnosticGlitch, _Warmup;
            float _DisplayBrightness, _DisplayContrast, _ClearDisplay;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half3 Sample(float2 uv)
            {
                uv.y = (uv.y - .5) / max(_ContentFit,.01) + .5;
                if (uv.y < 0 || uv.y > 1) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Calibration needs the original picture, without the CRT's color or motion effects.
                // Keep the overlay's brightness/contrast because its camera has no post processing.
                if (_ClearDisplay > .5)
                {
                    half3 clean = Sample(input.uv);
                    float cleanAlpha = 1;
                    if (_RespectSourceAlpha > .5)
                    {
                        clean = max(0, (clean - .18) * _DisplayContrast + .18) * exp2(_DisplayBrightness * 2);
                        cleanAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                    }
                    if (_ShipOverlay > .5)
                        return half4(clean * _Power * _SignalLevel, 1 - _GameplayBlend);
                    return half4(clean, cleanAlpha * _Power);
                }
                // UV is the position on the screen, from 0 to 1. Bend it slightly for curved glass.
                float2 glass = input.uv * 2 - 1;
                float radius = dot(glass, glass) * 0.5;
                float2 curved = glass * (1 + _Curvature * radius * radius);
                float2 corner = abs(curved) - 0.99;
                float aperture = length(max(corner, 0)) + min(max(corner.x, corner.y), 0) - 0.01;
                float alpha = 1 - smoothstep(-0.0015, 0.0015, aperture);
                if (alpha < 0.001) return half4(0, 0, 0, _ShipOverlay * (1 - _GameplayBlend));
                float2 uv = curved * 0.5 + 0.5;
                float2 pixel = floor(input.positionCS.xy);
                float frame = floor(_DisplayTime * 24);

                // Short sync slips can reach the edges, with no falling particles or moving bars.
                float2 region = abs(uv - _DisturbanceRegion.xy) / max(_DisturbanceRegion.zw, 0.0001);
                float localFault = (1 - smoothstep(0.6, 1, max(region.x, region.y))) * _Disturbance;
                float rowBlock = floor(uv.y * 38);
                float diagnosticBand = step(.78, Hash(float2(rowBlock, floor(_DisplayTime * 9))));
                float fault = max(localFault, diagnosticBand * max(_DiagnosticGlitch, _HandoffFault)) * _InterferenceStrength;
                uv.x += (Hash(float2(floor(uv.y * 180), frame)) - 0.5) * fault * _MainTex_TexelSize.x * 26;
                uv.y += (Hash(float2(frame, 3)) - .5) * _DiagnosticGlitch * .003 * _InterferenceStrength;
                float handoff = _HandoffFault * _InterferenceStrength;
                // Larger horizontal hold failures are exclusive to the instrument handoff.
                uv.x += (Hash(float2(rowBlock, frame)) - .5) * diagnosticBand * handoff * .15;
                if (handoff > .001) uv.y = frac(uv.y + handoff * .09 * sin(_DisplayTime * 6.1));
                // The deflection field opens, then the focus and horizontal hold settle.
                float heating = 1 - _Warmup;
                float rasterHeight = lerp(.08, 1, smoothstep(0, .46, heating));
                float rasterAperture = 1 - smoothstep(rasterHeight - .025, rasterHeight, abs(curved.y));
                uv.y = (uv.y - .5) / rasterHeight + .5;
                uv.x += _Warmup * _Warmup * sin(uv.y * 23 + _DisplayTime * 19) * .002;
                half3 center = Sample(uv);
                float edge = smoothstep(0.55, 1, max(abs(glass.x), abs(glass.y)));
                float2 split = float2(_MainTex_TexelSize.x * (_Convergence * edge + _Warmup * 1.6 + fault * 2.4), 0);
                half3 screen = half3(Sample(uv - split).r, center.g, Sample(uv + split).b);

                // Emission bleeds only around bright strokes; the original letter shape stays sharp.
                float2 texel = _MainTex_TexelSize.xy;
                half3 neighbors = (Sample(uv + float2(texel.x, 0)) + Sample(uv - float2(texel.x, 0)) +
                                   Sample(uv + float2(0, texel.y)) + Sample(uv - float2(0, texel.y))) * 0.25;
                half3 wide = (Sample(uv + texel * 2) + Sample(uv - texel * 2)) * 0.5;
                half3 emission = max(neighbors - 0.18, 0) * 0.7 + max(wide - 0.18, 0) * 0.3;
                screen += emission * _Glow;
                screen = lerp(screen, max(screen, neighbors * 0.75), 0.025);
                half3 old = SAMPLE_TEXTURE2D(_PreviousTex, sampler_PreviousTex, uv).rgb;
                screen += max(old - center, 0) * _Persistence;

                // Resolve each beam across at least 2.5 pixels, avoiding shimmer on smaller windows.
                float screenHeight = rcp(max(length(float2(ddx(input.uv.y),ddy(input.uv.y))),.00001));
                float lines = min(_ScanLines, screenHeight / 2.5);
                float bright = smoothstep(0.2, 0.8, max(center.r, max(center.g, center.b)));
                float beamDistance = abs(frac(input.uv.y * lines) - .5);
                float row = floor(input.uv.y * lines);
                float rowVariation = Hash(float2(row, 4.7)) - .5;
                float beamWidth = lerp(.20, .29, bright) + rowVariation * .025;
                float beam = exp2(-beamDistance * beamDistance / (beamWidth * beamWidth));
                // Bright phosphors bloom across the dark gaps; dim blue keeps the raster visible.
                float raster = 1 - beam;
                screen *= (1 - _ScanStrength * raster) * (1 + _ScanStrength * .18);
                // No staggered RGB lattice: it aliases into disorienting diagonal grain.
                // Beam widths and luminance vary slightly between horizontal raster rows.
                screen *= 1 + rowVariation * .045;

                // Very low amplitude electrical hum and broad retrace illumination, without tracking bars.
                float slow = sin(_DisplayTime * 1.37) * sin(_DisplayTime * 0.31);
                float roll = sin(uv.y * 6.283185 - _DisplayTime * 0.41);
                screen *= 1 + (slow + roll * 0.35) * _Flicker;
                screen += (Hash(pixel + float2(frame * 11, frame * 7)) - 0.5) * _Noise;
                screen *= 1 - fault * 0.16;
                screen *= 1 - _Vignette * radius * radius;

                // Black-level emission is confined to the powered display.
                screen += half3(0.0013, 0.0015, 0.0022) * (1 - bright);
                float rim = 1 - smoothstep(0, 0.012, -aperture);
                screen *= 1 - rim * 0.35;
                float settling = 1 + _Warmup * .055 * sin(_DisplayTime * 17);
                screen = lerp(screen, neighbors, _Warmup * .42) * settling;
                // Invitation ripples are drawn in the source UI and illuminate the real casing.
                // Reveal the original material underneath instead of approximating its HDR
                // emission/room lighting by sampling its texture in this unlit shader.
                if (_ShipOverlay > .5)
                {
                    // TerminalShipIntro controls these glitches during the camera handoff.
                    // Make this layer transparent at the end so the normal telescope shows through.
                    float tornRow = step(.64, Hash(float2(floor(input.uv.y * 19), floor(_DisplayTime * 8))));
                    float rollingGap = 1 - smoothstep(.07, .13, abs(frac(input.uv.y + _DisplayTime * .42) - .5));
                    float lostSync = max(tornRow * .82, rollingGap) * handoff;
                    float cover = max(1 - _GameplayBlend, max(1 - _SignalLevel, lostSync));
                    half3 picture = max(screen, 0) * _Power * _SignalLevel * rasterAperture * alpha;
                    half3 syncNoise = half3(.025, .045, .11) * Hash(float2(pixel.x, row + frame)) * handoff;
                    picture = lerp(picture, syncNoise, _GameplayBlend);
                    return half4(picture, cover);
                }
                // Pause rendering is transparent outside its own panel; never tint gameplay.
                float panelAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                alpha *= lerp(1, panelAlpha, _RespectSourceAlpha);
                // Overlay menus bypass the gameplay camera's color adjustments.
                if (_RespectSourceAlpha > .5)
                    screen = max(0, (screen - .18) * _DisplayContrast + .18) * exp2(_DisplayBrightness * 2);
                return half4(max(screen, 0), alpha * _Power * rasterAperture);
            }
            ENDHLSL
        }
    }
}
