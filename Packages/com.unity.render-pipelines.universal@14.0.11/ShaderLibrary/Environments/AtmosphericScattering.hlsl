#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/UnrealEngineHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#define ENVCONSTANTS_CONVOLUTION_MIP_COUNT (7)

#define _MipFogNear                     _MipFogParameters.x
#define _MipFogFar                      _MipFogParameters.y
#define _MipFogMaxMip                   _MipFogParameters.z
#define _EnableAtmosphereFog            _MipFogParameters.w

half3 SampleSkyCubemap(float3 reflectVector, float mip)
{
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip));
    return DecodeHDREnvironment(encodedIrradiance, _GlossyEnvironmentCubeMap_HDR);
}

float3 GetSkyColor(float3 V, float fragDist)
{
    // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
    float mipLevel = (1.0 - _MipFogMaxMip * saturate((fragDist - _MipFogNear) / (_MipFogFar - _MipFogNear))) * (ENVCONSTANTS_CONVOLUTION_MIP_COUNT - 1);
    // For the atmospheric scattering, we use the GGX convoluted version of the cubemap. That matches the of the index 0
    return SampleSkyCubemap(V, mipLevel); // '_FogColor' is the tint
}

void EvaluateAtmosphericScattering(PositionInputs posInput, float3 V, out float3 color, out float3 opacity)
{
    color = opacity = float3(0, 0, 0);

#ifdef DEBUG_DISPLAY
    return;
#endif

    // TODO: do not recompute this, but rather pass it directly.
    // Note1: remember the hacked value of 'posInput.positionWS'.
    // Note2: we do not adjust it anymore to account for the distance to the planet. This can lead to wrong results (since the planet does not write depth).
    float fogFragDist = distance(posInput.positionWS, GetCurrentViewPosition());

    if (_FogEnabled)
    {
        float4 volFog = float4(0.0, 0.0, 0.0, 0.0);

        float expFogStart = 0.0f;

        

        // TODO: if 'posInput.linearDepth' is computed using 'posInput.positionWS',
        // and the latter resides on the far plane, the computation will be numerically unstable.
        float distDelta = fogFragDist - expFogStart;

        if (_EnableVolumetricFog != 0)
        {
            
        }
        
        // Height Fog
        float4 HeightFogInscatteringAndOpacity = GetExponentialHeightFogUE(posInput.positionWS - GetCurrentViewPosition(), V);

        volFog.rgb += (1 - volFog.a) * HeightFogInscatteringAndOpacity.rgb;
        volFog.a = 1 - (1 - volFog.a) * HeightFogInscatteringAndOpacity.a;

        if (_EnableAtmosphereFog != 0)
        {
            // Atmosphere Fog
            // Apply the distant (fallback) fog.
            float3 positionWS = GetCurrentViewPosition() - V * expFogStart;
            float startHeight = positionWS.y;
            float cosZenith = V.y;

            // For both homogeneous and exponential media,
            // Integrate[Transmittance[x] * Scattering[x], {x, 0, t}] = Albedo * Opacity[t].
            // Note that pulling the incoming radiance (which is affected by the fog) out of the
            // integral is wrong, as it means that shadow rays are not volumetrically shadowed.
            // This will result in fog looking overly bright.

            float heightFogBaseExtinction = _HeightFogBaseExtinction;
            float heightFogBaseHeight = _HeightFogBaseHeight;

            float odFallback = OpticalDepthHeightFog(heightFogBaseExtinction, heightFogBaseHeight,
                                                     _HeightFogExponents.xy, cosZenith, startHeight, distDelta);
            float trFallback = saturate(TransmittanceFromOpticalDepth(odFallback));
            float trCamera = 1 - volFog.a;

            volFog.rgb += trCamera * GetSkyColor(V, fogFragDist) * (1 - trFallback);
            volFog.a = 1 - (trCamera * trFallback);
        }

        color = volFog.rgb; // Already pre-exposed
        opacity = volFog.a;

        if (_EnableLightShafts > 0)
        {
            
        }
    }
    
}
#endif