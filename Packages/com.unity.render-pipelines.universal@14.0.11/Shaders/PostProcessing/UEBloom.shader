Shader "Hidden/Universal Render Pipeline/UEBloom"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_SourceTexLowMip);
    
    float4 _SampleOffsets[32];
    float4 _SampleWeights[32];
    float4 _BlitTexture_TexelSize;
    float _BloomThreshold;
    int _SampleCount;

    float4 BloomSetupPS(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

        #if _USE_THRESHOLD
            half TotalLuminance = Luminance(color);
            half BloomLuminance = TotalLuminance - _BloomThreshold;
            half BloomAmount = saturate(BloomLuminance * 0.5f);
        #else
            half BloomAmount = 1.0f;
        #endif
        
        return float4(color * BloomAmount, 1);
    }

    float4 BloomDowSample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        
        float2 uvs[4];
        uvs[0] = uv + _BlitTexture_TexelSize.xy * float2(-1, -1);
        uvs[1] = uv + _BlitTexture_TexelSize.xy * float2(1, -1);
        uvs[2] = uv + _BlitTexture_TexelSize.xy * float2(-1, 1);
        uvs[3] = uv + _BlitTexture_TexelSize.xy * float2(1, 1);

        float4 Sample[4];

        UNITY_LOOP
        for (int i = 0; i < 4; i++)
        {
            Sample[i] = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvs[i]);
        }

        float4 Color = (Sample[0] + Sample[1] + Sample[2] + Sample[3]) * 0.25f;

        return Color;
    }

    float4 BloomFilterPS(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        
        float4 Color = 0;
        for (int SampleIndex = 0; SampleIndex < _SampleCount; SampleIndex++)
        {
            float2 UV = uv + _SampleOffsets[SampleIndex].xy;
            Color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV) * _SampleWeights[SampleIndex];
        }

        #if _COMBINE_ADDITIVE
            Color += SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv);
        #endif
        
        return Color;
    }
    
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        
        Pass
        {
            Name "Bloom Prefilter"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment BloomSetupPS
                #pragma shader_feature_local_fragment _USE_THRESHOLD
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Downsample"
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment BloomDowSample
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Filter"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment BloomFilterPS
                #pragma multi_compile_fragment _ _COMBINE_ADDITIVE
            ENDHLSL
        }
    }
}