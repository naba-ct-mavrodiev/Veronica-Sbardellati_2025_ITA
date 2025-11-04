Shader "Custom/VolumetricFog"
{
    Properties
    {
        [Header(Fog Settings)]
        _FogColor ("Fog Color", Color) = (0.5, 0.6, 0.7, 1.0)
        _Density ("Density", Range(0, 5)) = 1.0
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1.0
        _NoiseSpeed ("Animation Speed", Vector) = (0.1, 0.0, 0.1, 0.0)
        
        [Header(Volume Type)]
        [Toggle] _IsGroundFog ("Ground Fog Mode", Float) = 1
        _HeightFalloff ("Height Falloff", Range(0, 10)) = 2.0
        _FogFloor ("Fog Floor Height", Float) = 0.0
        _FogCeiling ("Fog Ceiling Height", Float) = 5.0
        
        [Header(Noise Input)]
        [NoScaleOffset] _NoiseTex3D ("3D Noise Texture (Optional)", 3D) = "white" {}
        [Toggle] _UseProceduralNoise ("Use Procedural Noise", Float) = 0
        _DetailNoiseTex ("Detail Texture (Optional)", 2D) = "white" {}
        _DetailScale ("Detail Scale", Range(0.1, 10)) = 3.0
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.3
        
        [Header(Raymarch Settings)]
        _StepCount ("Step Count", Range(4, 32)) = 16
        _MaxDistance ("Max Ray Distance", Range(10, 100)) = 50
        _Jitter ("Ray Jitter", Range(0, 1)) = 0.5
        
        [Header(Integration)]
        _SoftParticlesFactor ("Soft Particles Factor", Range(0, 10)) = 1.0
        _DepthFade ("Depth Fade", Range(0, 10)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        ZTest Always
        
        Pass
        {
            Name "VolumetricFogPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            // For VR single pass instanced
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 rayOrigin : TEXCOORD1;
                float3 rayDirection : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _NoiseScale;
                float3 _NoiseSpeed;
                float _IsGroundFog;
                float _HeightFalloff;
                float _FogFloor;
                float _FogCeiling;
                float _UseProceduralNoise;
                float _DetailScale;
                float _DetailStrength;
                float _StepCount;
                float _MaxDistance;
                float _Jitter;
                float _SoftParticlesFactor;
                float _DepthFade;
                float3 _VolumeCenter;
                float3 _VolumeSize;
                float3 _VolumeExtents;
            CBUFFER_END
            
            TEXTURE3D(_NoiseTex3D);
            SAMPLER(sampler_NoiseTex3D);
            
            TEXTURE2D(_DetailNoiseTex);
            SAMPLER(sampler_DetailNoiseTex);
            
            // Simple hash for jittering
            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453f);
            }

            // Simple 3D noise function (fallback when no texture)
            float SimpleNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0f - 2.0f * f);

                float n = i.x + i.y * 57.0f + 113.0f * i.z;

                // Trilinear interpolation
                float h000 = Hash(n + 0.0f);
                float h100 = Hash(n + 1.0f);
                float h010 = Hash(n + 57.0f);
                float h110 = Hash(n + 58.0f);
                float h001 = Hash(n + 113.0f);
                float h101 = Hash(n + 114.0f);
                float h011 = Hash(n + 170.0f);
                float h111 = Hash(n + 171.0f);

                float res = lerp(
                    lerp(lerp(h000, h100, f.x),
                         lerp(h010, h110, f.x), f.y),
                    lerp(lerp(h001, h101, f.x),
                         lerp(h011, h111, f.x), f.y), f.z);
                return res;
            }
            
            // Sample density at a world position
            float SampleDensity(float3 worldPos)
            {
                float density = 1.0;
                float3 samplePos = worldPos * _NoiseScale + _Time.y * _NoiseSpeed;
                
                // Main noise
                if (_UseProceduralNoise > 0.5)
                {
                    // Procedural noise (slower)
                    density = SimpleNoise3D(samplePos);
                    density += SimpleNoise3D(samplePos * 2.37) * 0.5;
                    density += SimpleNoise3D(samplePos * 4.96) * 0.25;
                    density /= 1.75;
                }
                else
                {
                    // 3D texture sampling (faster)
                    density = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D, samplePos * 0.1, 0).r;
                }
                
                // Optional detail texture
                float2 detailUV = worldPos.xz * _DetailScale + _Time.y * _NoiseSpeed.xz;
                float detail = SAMPLE_TEXTURE2D_LOD(_DetailNoiseTex, sampler_DetailNoiseTex, detailUV, 0).r;
                density = lerp(density, density * detail, _DetailStrength);
                
                // Apply height falloff for ground fog
                if (_IsGroundFog > 0.5f)
                {
                    // Use volume bounds for height calculations
                    float volumeBottom = _VolumeCenter.y - _VolumeExtents.y;
                    float volumeTop = _VolumeCenter.y + _VolumeExtents.y;
                    float heightFactor = 1.0f - saturate((worldPos.y - volumeBottom) / (volumeTop - volumeBottom));
                    heightFactor = pow(heightFactor, _HeightFalloff);
                    density *= heightFactor;
                }
                
                return saturate(density) * _Density;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                // Calculate ray for box intersection
                output.rayOrigin = GetCameraPositionWS();
                output.rayDirection = normalize(output.positionWS - output.rayOrigin);
                
                return output;
            }
            
            // Ray-AABB intersection
            float2 RayBoxIntersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
            {
                float3 invRayDir = 1.0 / rayDir;
                float3 t0 = (boxMin - rayOrigin) * invRayDir;
                float3 t1 = (boxMax - rayOrigin) * invRayDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                
                float dstA = max(max(tMin.x, tMin.y), tMin.z);
                float dstB = min(min(tMax.x, tMax.y), tMax.z);
                
                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                
                return float2(dstToBox, dstInsideBox);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Get depth for soft particles
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDiff = sceneDepth - fragDepth;
                
                // Calculate ray through volume
                float3 rayOrigin = input.rayOrigin;
                float3 rayDir = normalize(input.rayDirection);

                // Use actual volume bounds from the transform
                float3 boxMin = _VolumeCenter - _VolumeExtents;
                float3 boxMax = _VolumeCenter + _VolumeExtents;

                // Get intersection with box
                float2 intersection = RayBoxIntersection(rayOrigin, rayDir, boxMin, boxMax);
                float dstToBox = intersection.x;
                float dstInsideBox = intersection.y;
                
                if (dstInsideBox <= 0) 
                    discard;
                
                // Limit ray distance by scene depth
                float maxDst = min(dstInsideBox, min(depthDiff * 0.1, _MaxDistance));
                if (maxDst <= 0)
                    discard;
                
                // Jitter starting position for temporal AA
                float jitter = Hash(input.positionCS.x + input.positionCS.y * 100 + _Time.y * 1000) * _Jitter;
                
                // Raymarching
                float stepSize = maxDst / _StepCount;
                float3 rayStep = rayDir * stepSize;
                float3 currentPos = rayOrigin + rayDir * (dstToBox + stepSize * jitter);
                
                float transmittance = 1.0;
                float3 scattering = 0;
                
                for (int i = 0; i < _StepCount; i++)
                {
                    if (transmittance < 0.01)
                        break;
                        
                    float density = SampleDensity(currentPos);
                    
                    if (density > 0.01)
                    {
                        float absorption = exp(-density * stepSize);
                        float3 scattered = _FogColor.rgb * density * transmittance * stepSize;
                        
                        scattering += scattered;
                        transmittance *= absorption;
                    }
                    
                    currentPos += rayStep;
                }
                
                // Soft particle fade
                float softParticle = saturate(depthDiff * _SoftParticlesFactor);
                
                // Final color and alpha
                float alpha = 1.0 - transmittance;
                alpha *= softParticle;
                alpha *= _FogColor.a;
                
                return float4(scattering, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}