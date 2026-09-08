// Lit billboard sprite for HD-2D.
//
// Renders in the AlphaTest queue with ZWrite on, so sprites intersect and sort
// against 3D level geometry the way a real object would - that interpenetration
// is what sells the "sprites living inside a diorama" illusion. Transparent
// sprites cannot do this; they would always sort as a flat layer.
Shader "AAZ/HD2D/Sprite Lit"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite", 2D) = "white" {}   // named _MainTex so an existing SpriteRenderer (and its Animator sprite swaps) keeps binding the texture
        [MainColor]   _BaseColor("Tint", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [KeywordEnum(None, YAxis, Full)] _Billboard("Billboard Mode", Float) = 1
        _VerticalTilt("Vertical Tilt (deg)", Range(-30, 30)) = 0

        [Header(Shading)]
        _LightWrap("Light Wrap", Range(0.0, 1.0)) = 0.4
        _NormalBend("Normal Bend", Range(0.0, 1.0)) = 0.5
        _ShadowTint("Ambient Tint", Color) = (0.55, 0.62, 0.85, 1)
        _AmbientBoost("Ambient Boost", Range(0.0, 3.0)) = 1
        _ShadowStrength("Received Shadow Strength", Range(0.0, 1.0)) = 1
        [Toggle(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff("Disable Received Shadows", Float) = 0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission("Use Emission", Float) = 0
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        [Header(Rim)]
        [HDR] _RimColor("Rim Color", Color) = (0,0,0,0)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        [Toggle] _AlphaToMask("Alpha To Coverage", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HD2DVertex
            #pragma fragment FragForward

            #pragma shader_feature_local_vertex _BILLBOARD_NONE _BILLBOARD_YAXIS _BILLBOARD_FULL
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "HD2D_SpriteInput.hlsl"

            half4 FragForward(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 albedo = HD2DSampleAlbedo(IN);
                float3 normalWS = HD2DShadingNormal(IN);
                float3 positionWS = IN.positionWS;
                half3 viewDirWS = half3(normalize(GetWorldSpaceViewDir(positionWS)));

                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = HD2DShadowCoord(IN);
                inputData.fogCoord = IN.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half occlusion = 1.0h;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                occlusion = aoFactor.indirectAmbientOcclusion;
            #endif

                // Sky/ambient is deliberately tinted: the cool fill against a warm key
                // is what makes HD-2D scenes read as dioramas rather than flat sprites.
                half3 lighting = SampleSH(normalWS) * _AmbientBoost * _ShadowTint.rgb * occlusion;

                Light mainLight = GetMainLight(inputData.shadowCoord, positionWS, inputData.shadowMask);
                half mainShadow = mainLight.shadowAttenuation;
            #if defined(_RECEIVE_SHADOWS_OFF)
                mainShadow = 1.0h;
            #else
                mainShadow = lerp(1.0h, mainShadow, _ShadowStrength);
            #endif
                lighting += mainLight.color
                          * HD2DWrapDiffuse(normalWS, mainLight.direction)
                          * (mainLight.distanceAttenuation * mainShadow);

            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if defined(LIGHT_LOOP_BEGIN)
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS, inputData.shadowMask);
                    half atten = light.distanceAttenuation;
                    #if !defined(_RECEIVE_SHADOWS_OFF)
                        atten *= lerp(1.0h, light.shadowAttenuation, _ShadowStrength);
                    #endif
                    lighting += light.color * HD2DWrapDiffuse(normalWS, light.direction) * atten;
                LIGHT_LOOP_END
                #else
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS, inputData.shadowMask);
                    half atten = light.distanceAttenuation;
                    #if !defined(_RECEIVE_SHADOWS_OFF)
                        atten *= lerp(1.0h, light.shadowAttenuation, _ShadowStrength);
                    #endif
                    lighting += light.color * HD2DWrapDiffuse(normalWS, light.direction) * atten;
                }
                #endif
            #endif

                half3 color = albedo.rgb * lighting;

            #if defined(_EMISSION)
                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;
            #endif

                half rim = 1.0h - saturate(dot(viewDirWS, half3(normalWS)));
                color += _RimColor.rgb * pow(rim, _RimPower) * _RimColor.a;

                color = MixFog(color, inputData.fogCoord);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertShadow
            #pragma fragment FragShadow

            #pragma shader_feature_local_vertex _BILLBOARD_NONE _BILLBOARD_YAXIS _BILLBOARD_FULL
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "HD2D_SpriteInput.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings VertShadow(Attributes IN)
            {
                // Reuse the main billboard basis, oriented to the *player's* camera, so
                // the cast shadow matches the silhouette actually on screen.
                Varyings OUT = HD2DVertex(IN);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - OUT.positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(OUT.positionWS, OUT.normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 FragShadow(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                HD2DSampleAlbedo(IN);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HD2DVertex
            #pragma fragment FragDepth

            #pragma shader_feature_local_vertex _BILLBOARD_NONE _BILLBOARD_YAXIS _BILLBOARD_FULL
            #pragma multi_compile_instancing

            #include "HD2D_SpriteInput.hlsl"

            half4 FragDepth(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                HD2DSampleAlbedo(IN);
                return 0;
            }
            ENDHLSL
        }

        // Feeds SSAO and any depth-normal driven effects. Without it, sprites punch
        // holes in the ambient occlusion pass.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HD2DVertex
            #pragma fragment FragDepthNormals

            #pragma shader_feature_local_vertex _BILLBOARD_NONE _BILLBOARD_YAXIS _BILLBOARD_FULL
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma multi_compile_instancing

            #include "HD2D_SpriteInput.hlsl"

            half4 FragDepthNormals(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                HD2DSampleAlbedo(IN);
                float3 normalWS = HD2DShadingNormal(IN);
                return half4(NormalizeNormalPerPixel(normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "AAZ.HD2D.Editor.HD2DSpriteShaderGUI"
    Fallback "Universal Render Pipeline/Unlit"
}
