Shader "Ludonoetics/Beam_Volume_URP"
{
    Properties
    {
        _BaseColor ("Base Color (RGBA = color + master opacity)", Color) = (0.92,0.92,0.92,0.6)

        // Shape in object space: x/z radii + "boxiness"
        _RadiusX   ("Radius X (local units)", Float) = 0.5
        _RadiusZ   ("Radius Z (local units)", Float) = 3.0
        _ShapePow  ("Boxiness (2=circle, 10=boxy)", Float) = 6.0
        _EdgeSoft  ("Edge Softness (0-0.5)", Range(0.0,0.5)) = 0.18

        // Vertical fade (normalize with local Y range)
        _YMin ("Local Y Min", Float) = -5.0
        _YMax ("Local Y Max", Float) =  5.0
        _BottomFadeStart ("Bottom Fade Start (0-1)", Range(0,1)) = 0.00
        _BottomFadeEnd   ("Bottom Fade End (0-1)",   Range(0,1)) = 0.10
        _TopFadeStart    ("Top Fade Start (0-1)",    Range(0,1)) = 0.78
        _TopFadeEnd      ("Top Fade End (0-1)",      Range(0,1)) = 1.00

        // Depth fade into scene geometry
        _DepthFadeDist ("Depth Fade Distance", Float) = 2.0

        // Subtle moving noise (optional)
        _NoiseTex    ("Noise (L)", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling XY", Vector) = (1.2, 6.0, 0, 0)
        _NoiseSpeed  ("Noise Scroll XY", Vector) = (0.02, 0.06, 0, 0)
        _NoiseAmt    ("Noise Amount (0-1)", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off                // two-sided so the box works from inside/outside
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // URP / VR / instancing
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS // (not used, but keeps compatibility)
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _RadiusX;
                float  _RadiusZ;
                float  _ShapePow;
                float  _EdgeSoft;

                float  _YMin;
                float  _YMax;
                float  _BottomFadeStart;
                float  _BottomFadeEnd;
                float  _TopFadeStart;
                float  _TopFadeEnd;

                float  _DepthFadeDist;

                float4 _NoiseTiling; // xy
                float4 _NoiseSpeed;  // xy
                float  _NoiseAmt;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // URP camera depth texture
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 screenPos   : TEXCOORD3; // for depth sampling
                float  fogFactor   : TEXCOORD4; // URP fog
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.positionOS  = v.positionOS.xyz;
                o.uv          = v.uv;

                // Screen position for depth texture lookups
                o.screenPos   = ComputeScreenPos(o.positionHCS);

                // Compute fog factor for URP
                o.fogFactor   = ComputeFogFactor(o.positionHCS.z);
                return o;
            }

            // Lp norm: p=2 circle, p=10 ~ box
            float lpNorm(float x, float z, float p)
            {
                x = abs(x);
                z = abs(z);
                return pow( pow(x, p) + pow(z, p), 1.0/p );
            }

            float sampleSceneDepthFade(float4 screenPos, float3 positionWS, float fadeDist)
            {
                // sample scene depth
                float2 uv = screenPos.xy / screenPos.w;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif

                float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float sceneEye = LinearEyeDepth(raw, _ZBufferParams);

                // current fragment eye depth
                float3 viewPos = TransformWorldToView(positionWS);
                float  pixelEye = -viewPos.z; // forward is -Z

                float d = sceneEye - pixelEye;   // positive when close to geometry
                return saturate(d / max(fadeDist, 1e-4));
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // --- 1) base color / opacity
                float3 baseRGB = _BaseColor.rgb;
                float  masterA = _BaseColor.a;

                // --- 2) object-space radial/box mask
                float x = i.positionOS.x / max(_RadiusX, 1e-4);
                float z = i.positionOS.z / max(_RadiusZ, 1e-4);
                float p = max(_ShapePow, 1.0001);
                float d = lpNorm(x, z, p);      // 0 center, 1 on edge
                float edge0 = saturate(1.0 - _EdgeSoft);
                float edge1 = 1.0;
                float shapeMask = 1.0 - smoothstep(edge0, edge1, d);

                // --- 3) vertical fade (normalize local y)
                float y01 = saturate( (i.positionOS.y - _YMin) / max(_YMax - _YMin, 1e-4) );
                float bottom = smoothstep(_BottomFadeStart, _BottomFadeEnd, y01);
                float top    = 1.0 - smoothstep(_TopFadeStart, _TopFadeEnd, y01);
                float yMask  = saturate(bottom * top);

                // --- 4) depth fade into geometry
                float depthFade = sampleSceneDepthFade(i.screenPos, i.positionWS, _DepthFadeDist);

                // --- 5) subtle animated noise (object-space stable in VR)
                float2 nuv = i.uv * _NoiseTiling.xy + _Time.y * _NoiseSpeed.xy;
                float  n   = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;
                float  noiseTerm = lerp(1.0 - _NoiseAmt, 1.0, n);

                // final alpha
                float alpha = masterA * shapeMask * yMask * depthFade * noiseTerm;

                // apply fog on color (keeps alpha separate) - URP fog
                half3 col = MixFog(baseRGB, i.fogFactor);

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
