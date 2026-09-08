// Tilt-shift depth of field for HD-2D.
//
// The signature Octopath/Triangle-Strategy look is a *band* of focus across the
// screen rather than a radial blur: everything above and below a horizontal strip
// melts, which reads as a miniature diorama. We build that in four passes so the
// expensive blur runs at quarter resolution and only the composite is full-res.
Shader "Hidden/AAZ/HD2D/TiltShift"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_HD2DBlurTex);
        SAMPLER(sampler_HD2DBlurTex);
        float4 _HD2DBlurTex_TexelSize;

        // xy = focus centre in viewport space, z = half-height of the sharp band,
        // w = falloff distance from the edge of the band to fully blurred.
        float4 _TiltShiftBand;
        // x = cos(angle), y = sin(angle), z = near-side strength, w = far-side strength.
        float4 _TiltShiftAxis;
        // x = blur radius in texels, y = max blur, z = mask exponent, w = depth mode blend.
        float4 _TiltShiftParams;
        // x = focus distance, y = focus range, z = depth falloff, w = unused.
        float4 _TiltShiftDepth;

        // How out of focus is this pixel, 0 = sharp, 1 = fully blurred.
        half CoC(float2 uv)
        {
            // Screen-space band. Project onto the axis perpendicular to the focus strip.
            float2 p = uv - _TiltShiftBand.xy;
            float2 axis = float2(-_TiltShiftAxis.y, _TiltShiftAxis.x);
            float signedDist = dot(p, axis);
            float d = abs(signedDist);

            half band = saturate((d - _TiltShiftBand.z) / max(_TiltShiftBand.w, 1e-4));
            // Bias: the far half of the screen (up) usually blurs harder than the near half.
            half sideScale = signedDist >= 0.0 ? _TiltShiftAxis.w : _TiltShiftAxis.z;
            band = pow(band, max(_TiltShiftParams.z, 1e-4)) * sideScale;

            // Optional true-depth term, blended in by _TiltShiftParams.w.
            UNITY_BRANCH
            if (_TiltShiftParams.w > 0.0)
            {
                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float dist = abs(eyeDepth - _TiltShiftDepth.x);
                half depthCoC = saturate((dist - _TiltShiftDepth.y) / max(_TiltShiftDepth.z, 1e-4));
                band = lerp(band, max(band, depthCoC), _TiltShiftParams.w);
            }

            return saturate(band) * _TiltShiftParams.y;
        }

        // 9-tap separable Gaussian. Weights are the standard binomial approximation
        // folded into 5 taps using linear-filter midpoints.
        half4 BlurAxis(float2 uv, float2 dir)
        {
            const half w0 = 0.2270270270h;
            const half w1 = 0.3162162162h;
            const half w2 = 0.0702702703h;
            const float o1 = 1.3846153846;
            const float o2 = 3.2307692308;

            half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * w0;
            c += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + dir * o1) * w1;
            c += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - dir * o1) * w1;
            c += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + dir * o2) * w2;
            c += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - dir * o2) * w2;
            return c;
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        // 0 - downsample
        Pass
        {
            Name "HD2D TiltShift Downsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.texcoord);
            }
            ENDHLSL
        }

        // 1 - horizontal blur
        Pass
        {
            Name "HD2D TiltShift Blur H"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 dir = float2(_BlitTexture_TexelSize.x * _TiltShiftParams.x, 0.0);
                return BlurAxis(IN.texcoord, dir);
            }
            ENDHLSL
        }

        // 2 - vertical blur
        Pass
        {
            Name "HD2D TiltShift Blur V"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 dir = float2(0.0, _BlitTexture_TexelSize.y * _TiltShiftParams.x);
                return BlurAxis(IN.texcoord, dir);
            }
            ENDHLSL
        }

        // 3 - composite sharp + blurred using the tilt-shift mask
        Pass
        {
            Name "HD2D TiltShift Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                half4 blurred = SAMPLE_TEXTURE2D_X(_HD2DBlurTex, sampler_HD2DBlurTex, IN.texcoord);
                half coc = CoC(IN.texcoord);
                return half4(lerp(sharp.rgb, blurred.rgb, coc), sharp.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
