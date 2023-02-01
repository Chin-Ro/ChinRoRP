#ifndef CHINRO_UNLIT_PASS_INCLUDED
#define CHINRO_UNLIT_PASS_INCLUDED

//#include "../ShaderLibrary/Common.hlsl"

// CBUFFER_START(UnityPerMaterial)
//     float4 _BaseColor;
// CBUFFER_END
// TEXTURE2D(_BaseMap);
// SAMPLER(sampler_BaseMap);
//
// UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//     UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
// UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float2 detailUV : VAR_DETAIL_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    //float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.detailUV = TransformDetailUV(input.baseUV);
    
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET 
{
    UNITY_SETUP_INSTANCE_ID(input)
    //float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    //float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    InputConfig config = GetInoutConfig(input.baseUV, input.detailUV);
    float4 base = GetBase(config);
    
    #if defined(_SHADOWS_CLIP)
    clip(base.a - GetCutoff(input.baseUV));
    #endif
    
    return base;
}
#endif