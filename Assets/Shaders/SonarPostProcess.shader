Shader "EchoThief/SonarPostProcess"
{
    Properties
    {
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "black" {}
        _EdgeDetectionThreshold ("Edge Detection Threshold", Range(0.001, 0.1)) = 0.01
        _GlowIntensity ("Glow Intensity", Range(0.5, 5.0)) = 2.0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "SonarPass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // -- Textures --
            // _BlitTexture is declared in Blit.hlsl

            // -- Properties --
            float _EdgeDetectionThreshold;
            float _GlowIntensity;
            float4 _BackgroundColor;

            // -- Sonar Pulse Data (set globally by SonarManager.cs) --
            // Max 20 pulses to match SonarManager._maxPulses
            #define MAX_PULSES 20

            int _SonarPulseCount;
            float4 _SonarPulseOrigins[MAX_PULSES];  // xyz = world position
            float _SonarPulseRadii[MAX_PULSES];      // current expanding radius
            float _SonarPulseThickness[MAX_PULSES];  // ring band width
            float _SonarPulseFades[MAX_PULSES];      // 1 = full, 0 = gone
            float4 _SonarPulseColors[MAX_PULSES];    // rgba neon color

            // Vert is provided by Blit.hlsl
            // It outputs Varyings with positionCS and texcoord

            // Reconstruct world position from depth buffer
            float3 ReconstructWorldPos(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                return worldPos;
            }

            // Simple depth-based edge detection (Sobel-like)
            float EdgeDetection(float2 uv)
            {
                float2 texelSize = _BlitTexture_TexelSize.xy;

                float depthCenter = SampleSceneDepth(uv);
                float depthLeft   = SampleSceneDepth(uv + float2(-texelSize.x, 0));
                float depthRight  = SampleSceneDepth(uv + float2( texelSize.x, 0));
                float depthUp     = SampleSceneDepth(uv + float2(0,  texelSize.y));
                float depthDown   = SampleSceneDepth(uv + float2(0, -texelSize.y));

                float edgeH = abs(depthLeft - depthRight);
                float edgeV = abs(depthUp - depthDown);
                float edge = max(edgeH, edgeV);

                return step(_EdgeDetectionThreshold, edge);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 worldPos = ReconstructWorldPos(uv);
                float edge = EdgeDetection(uv);

                float3 finalColor = _BackgroundColor.rgb;
                float totalVisibility = 0;

                // Accumulate visibility from all active sonar pulses
                for (int i = 0; i < _SonarPulseCount; i++)
                {
                    float3 pulseOrigin = _SonarPulseOrigins[i].xyz;
                    float radius = _SonarPulseRadii[i];
                    float thickness = _SonarPulseThickness[i];
                    float fade = _SonarPulseFades[i];
                    float3 pulseColor = _SonarPulseColors[i].rgb;

                    float dist = distance(worldPos, pulseOrigin);

                    // Ring band: visible between (radius - thickness) and (radius + thickness)
                    float ringInner = smoothstep(radius - thickness, radius - thickness * 0.5, dist);
                    float ringOuter = 1.0 - smoothstep(radius + thickness * 0.5, radius + thickness, dist);
                    float ring = ringInner * ringOuter;

                    // Also show a fading "trail" inside the ring (already-scanned area)
                    float trail = saturate(1.0 - (dist / (radius + 0.001))) * 0.15;

                    float visibility = (ring + trail) * fade;
                    totalVisibility += visibility;

                    // Blend pulse color
                    finalColor += pulseColor * visibility * _GlowIntensity;
                }

                totalVisibility = saturate(totalVisibility);

                // Multiply by edge detection to get that wireframe/outline look
                // Mix between full fill (slight) and edge-only (strong)
                float edgeMix = lerp(0.1, 1.0, edge);
                finalColor *= edgeMix * totalVisibility;

                // Add a subtle base scene color in sonar-lit areas (so geometry has some fill)
                float3 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
                finalColor += sceneColor * totalVisibility * 0.05;

                // Background where nothing is visible
                finalColor = lerp(_BackgroundColor.rgb, finalColor, totalVisibility);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
