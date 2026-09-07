Shader "EchoThief/SonarPostProcess"
{
    Properties
    {
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "black" {}
        _EdgeDetectionThreshold ("Edge Detection Threshold", Range(0.001, 0.1)) = 0.01
        _GlowIntensity ("Glow Intensity", Range(0.5, 5.0)) = 2.0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _SonarArcThickness ("Arc Thickness", Range(0.1, 4.0)) = 1.5
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

            float _EdgeDetectionThreshold;
            float _GlowIntensity;
            float4 _BackgroundColor;
            float _SonarArcThickness;

            #define MAX_ARCS 64

            int _SonarArcCount;
            float4 _SonarArcOrigins[MAX_ARCS]; // xz = center
            float _SonarArcRadii[MAX_ARCS];
            float4 _SonarArcAngles[MAX_ARCS]; // x = start, y = end
            float _SonarArcFades[MAX_ARCS];
            float4 _SonarArcColors[MAX_ARCS];

            float3 ReconstructWorldPos(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                return worldPos;
            }

            float EdgeDetection(float2 uv)
            {
                float2 texelSize = _BlitTexture_TexelSize.xy;

                float depthCenter = SampleSceneDepth(uv);
                float depthLeft = SampleSceneDepth(uv + float2(-texelSize.x, 0));
                float depthRight = SampleSceneDepth(uv + float2(texelSize.x, 0));
                float depthUp = SampleSceneDepth(uv + float2(0, texelSize.y));
                float depthDown = SampleSceneDepth(uv + float2(0, -texelSize.y));

                float edgeH = abs(depthLeft - depthRight);
                float edgeV = abs(depthUp - depthDown);
                float edge = max(edgeH, edgeV);

                return step(_EdgeDetectionThreshold, edge);
            }

            float NormalizeAngle(float angle)
            {
                float twoPi = 6.2831853;
                float wrapped = fmod(angle, twoPi);
                return wrapped < 0 ? wrapped + twoPi : wrapped;
            }

            float AngleMask(float angle, float startAngle, float endAngle)
            {
                float twoPi = 6.2831853;
                float start = NormalizeAngle(startAngle);
                float end = NormalizeAngle(endAngle);
                float span = end - start;
                if (span < 0) span += twoPi;
                if (span < 0.001) return 1.0;

                float local = angle - start;
                if (local < 0) local += twoPi;
                return local <= span ? 1.0 : 0.0;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 worldPos = ReconstructWorldPos(uv);
                float edge = EdgeDetection(uv);

                float3 finalColor = _BackgroundColor.rgb;
                float totalVisibility = 0;

                float2 worldXZ = float2(worldPos.x, worldPos.z);

                for (int i = 0; i < _SonarArcCount; i++)
                {
                    float2 centerXZ = float2(_SonarArcOrigins[i].x, _SonarArcOrigins[i].z);
                    float radius = _SonarArcRadii[i];
                    float startAngle = _SonarArcAngles[i].x;
                    float endAngle = _SonarArcAngles[i].y;
                    float fade = _SonarArcFades[i];
                    float3 pulseColor = _SonarArcColors[i].rgb;

                    float2 arcVector = worldXZ - centerXZ;
                    float dist = length(arcVector);
                    float angle = atan2(arcVector.y, arcVector.x);

                    float angleMask = AngleMask(angle, startAngle, endAngle);
                    if (angleMask <= 0.0)
                    {
                        continue;
                    }

                    float ringInner = smoothstep(radius - _SonarArcThickness, radius - _SonarArcThickness * 0.5, dist);
                    float ringOuter = 1.0 - smoothstep(radius + _SonarArcThickness * 0.5, radius + _SonarArcThickness, dist);
                    float ring = ringInner * ringOuter;
                    float trail = 0.0;

                    float visibility = (ring + trail) * fade * angleMask;
                    if (visibility > totalVisibility)
                    {
                        totalVisibility = visibility;
                        finalColor = pulseColor * visibility * _GlowIntensity;
                    }
                }

                totalVisibility = saturate(totalVisibility);
                float edgeMix = lerp(0.1, 1.0, edge);
                finalColor *= edgeMix * totalVisibility;

                float3 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
                finalColor += sceneColor * totalVisibility * 0.05;
                finalColor = lerp(_BackgroundColor.rgb, finalColor, totalVisibility);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
