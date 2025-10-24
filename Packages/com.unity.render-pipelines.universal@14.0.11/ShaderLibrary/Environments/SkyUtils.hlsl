#ifndef SKY_UTILS_INCLUDED
#define SKY_UTILS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Generates a world-space view direction for sky and atmospheric effects
float3 GetSkyViewDirWS(float2 positionCS)
{
    float4 viewDirWS = mul(float4(positionCS.xy, 1.0f, 1.0f), _PixelCoordToViewDirWS);
    return normalize(viewDirWS.xyz);
}

// Returns latlong coords from view direction
float2 GetLatLongCoords(float3 dir, float upperHemisphereOnly)
{
    const float2 invAtan = float2(0.1591, 0.3183);

    float fastATan2 = FastAtan2(dir.x, dir.z);
    float2 uv = float2(fastATan2, FastASin(dir.y)) * invAtan + 0.5;
    uv.y = upperHemisphereOnly ? uv.y * 2.0 - 1.0 : uv.y;
    return uv;
}

float3 RotationUp(float3 p, float2 cos_sin)
{
    float3 rotDirX = float3(cos_sin.x, 0, -cos_sin.y);
    float3 rotDirY = float3(cos_sin.y, 0,  cos_sin.x);

    return float3(dot(rotDirX, p), p.y, dot(rotDirY, p));
}

// Follows PBRT convention http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html#PhaseHG
float HenyeyGreensteinPhase(float G, float CosTheta)
{
    // Reference implementation (i.e. not schlick approximation). 
    // See http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
    float Numer = 1.0f - G * G;
    float Denom = 1.0f + G * G + 2.0f * G * CosTheta;
    return Numer / (4.0f * PI * Denom * sqrt(Denom));
}

float RayleighPhase(float CosTheta)
{
    float Factor = 3.0f / (16.0f * PI);
    return Factor * (1.0f + CosTheta * CosTheta);
}
#endif