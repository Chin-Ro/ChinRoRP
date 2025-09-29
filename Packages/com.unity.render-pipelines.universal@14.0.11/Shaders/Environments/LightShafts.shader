Shader "Hidden/LightShafts"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _DirectionalLightScreenPos;

        /** directional in Screen State is x, BloomScale in y, 1.0 / OcclusionDepthRange in z, OcclusionMaskDarkness in w. */
        float4 _LightShaftParameters;
        ENDHLSL

        Pass
        {
            Name "LightShafts DownSample"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                float4 OutColor = 1;
                
                float SceneDepth = SampleSceneDepth(input.texcoord);
                SceneDepth = LinearEyeDepth(SceneDepth, _ZBufferParams);

	            float2 NormalizedCoordinates = input.texcoord;
	            // Setup a mask that is 1 at the edges of the screen and 0 at the center
	            float EdgeMask = 1.0f - NormalizedCoordinates.x * (1.0f - NormalizedCoordinates.x) * NormalizedCoordinates.y * (1.0f - NormalizedCoordinates.y) * 8.0f;
	            EdgeMask = EdgeMask * EdgeMask * EdgeMask * EdgeMask;
            	
	            float InvOcclusionDepthRange = _LightShaftParameters.z;
	            // Filter the occlusion mask instead of the depths
	            float OcclusionMask = saturate(SceneDepth * InvOcclusionDepthRange);
	            // Apply the edge mask to the occlusion factor
	            OutColor.x = max(OcclusionMask, EdgeMask);
                
                return OutColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "LightShafts DownSample(Bloom)"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            float _BloomMaxBrightness;
            /** Tint in rgb, threshold in a. */
            float4 _BloomTintAndThreshold;

            float4 Frag(Varyings input) : SV_Target
            {
                float4 OutColor = 1;
            
                float3 SceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
	            float SceneDepth = SampleSceneDepth(input.texcoord);
                SceneDepth = LinearEyeDepth(SceneDepth, _ZBufferParams);
                
                // Setup a mask that is 1 at the edges of the screen and 0 at the center
                float EdgeMask = 1.0f - input.texcoord.x * (1.0f - input.texcoord.x) * input.texcoord.y * (1.0f - input.texcoord.y) * 8.0f;
	            EdgeMask = EdgeMask * EdgeMask * EdgeMask * EdgeMask;
                //OutColor.rgb = SceneColor;
                // Only bloom colors according if post exposure brightness is over BloomThreshold,
	            // and if brightness is greater than BloomMaxBrightness, scale it down to BloomMaxBrightness to avoid saturated artefacts.
	            float Luminance = max(dot(SceneColor, half3(.3f, .59f, .11f)), 6.10352e-5);
	            float AdjustedLuminance = clamp(Luminance - _BloomTintAndThreshold.a, 0.0f, _BloomMaxBrightness);
	            float3 BloomColor = _LightShaftParameters.y * SceneColor / Luminance * AdjustedLuminance;

                float InvOcclusionDepthRange = _LightShaftParameters.z;

                // Only allow bloom from pixels whose depth are in the far half of OcclusionDepthRange
	            float BloomDistanceMask = saturate((SceneDepth - .5f / InvOcclusionDepthRange) * InvOcclusionDepthRange);
                
                float2 lightPosUV = GetNormalizedScreenSpaceUV(_DirectionalLightScreenPos) * _LightShaftParameters.x;

                float2 uv = input.texcoord  * _LightShaftParameters.x;
                
	            // Setup a mask that is 0 at TextureSpaceBlurOrigin and increases to 1 over distance
	            float BlurOriginDistanceMask = 1.0f - saturate(length(lightPosUV - uv) * 2.0f);
	            // Calculate bloom color with masks applied
	            OutColor.rgb = BloomColor * _BloomTintAndThreshold.rgb * BloomDistanceMask * (1.0f - EdgeMask) * BlurOriginDistanceMask * BlurOriginDistanceMask;
                
	            // // Filter the occlusion mask instead of the depths
	            float OcclusionMask = saturate(SceneDepth * InvOcclusionDepthRange);
	            // // Apply the edge mask to the occlusion factor
	            OutColor.a = max(OcclusionMask, EdgeMask);
                
                return OutColor;
            }
            ENDHLSL
        }

		Pass
		{
			Name "Radial Blur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // x = Blur Num Samples, y = firstPassDistance, z = pass Index
            float4 _RadialBlurParameters;

            #define NUM_SAMPLES _RadialBlurParameters.x

            float4 Frag(Varyings input) : SV_Target
            {
                // Increase the blur distance exponentially in each pass
                float PassScale = pow(abs(.4f * NUM_SAMPLES), _RadialBlurParameters.z);
                
                float2 lightPosUV = GetNormalizedScreenSpaceUV(_DirectionalLightScreenPos) * _LightShaftParameters.x;

                float2 uv = input.texcoord  * _LightShaftParameters.x;
                
                float2 BlurVector = (lightPosUV - uv) * min(_RadialBlurParameters.y * PassScale, 1);
                
                float4 BlurredValues = 0;
                for (int SampleIndex = 0; SampleIndex < NUM_SAMPLES; SampleIndex++)
                {
                    float2 SampleUVs = uv + BlurVector * SampleIndex / NUM_SAMPLES;
                    float2 ClampedUVs = clamp(SampleUVs, 0, 1);
                    float4 SampleColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, ClampedUVs);
                    BlurredValues += SampleColor;
                }
                float4 OutColor = BlurredValues / NUM_SAMPLES;
                
                return OutColor;
            }
            ENDHLSL
		}

		Pass
		{
			Name "Finish Occlusion LightShafts"
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float LightShaftOcclusion = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).x;

                // LightShaftParameters.w is OcclusionMaskDarkness, use that to control what an occlusion value of 0 maps to
	            float FinalOcclusion = lerp(_LightShaftParameters.w, 1, LightShaftOcclusion * LightShaftOcclusion);
                
                float2 lightPosUV = GetNormalizedScreenSpaceUV(_DirectionalLightScreenPos) * _LightShaftParameters.x;

                float2 uv = input.texcoord  * _LightShaftParameters.x;
                
	            // Setup a mask based on where the blur origin is
	            float BlurOriginDistanceMask = saturate(length(lightPosUV - uv) * .2f);
                
	            // Fade out occlusion over distance away from the blur origin
	            FinalOcclusion = lerp(FinalOcclusion, 1, BlurOriginDistanceMask);
                
	            float4 OutColor = float4(FinalOcclusion, 1, 1, 1);

                return OutColor;
            }
            ENDHLSL
		}

		Pass
        {
            Name "Finish Occlusion LightShafts(Bloom)"
            
            Blend One One
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D(_LightShaftTexture);
            
            float4 Frag(Varyings input) : SV_Target
            {
                float4 color = 1;
            	
            	float4 lightShaftsBloomColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

            	float LightShaftOcclusion = SAMPLE_TEXTURE2D(_LightShaftTexture, sampler_LinearClamp, input.texcoord);
                 // LightShaftParameters.w is OcclusionMaskDarkness, use that to control what an occlusion value of 0 maps to
	            float FinalOcclusion = lerp(_LightShaftParameters.w, 1, LightShaftOcclusion * LightShaftOcclusion);
                
                float2 lightPosUV = GetNormalizedScreenSpaceUV(_DirectionalLightScreenPos) * _LightShaftParameters.x;

                float2 uv = input.texcoord  * _LightShaftParameters.x;
                
	            // Setup a mask based on where the blur origin is
	            float BlurOriginDistanceMask = saturate(length(lightPosUV - uv) * .2f);
            	
	            // Fade out occlusion over distance away from the blur origin
	            FinalOcclusion = lerp(FinalOcclusion, 1, BlurOriginDistanceMask);
            	
                color.rgb = lightShaftsBloomColor.rgb * FinalOcclusion;
                
                return color;
            }
            ENDHLSL
        }
    }
}