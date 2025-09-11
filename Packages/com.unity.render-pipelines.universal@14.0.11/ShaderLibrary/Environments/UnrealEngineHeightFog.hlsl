#ifndef UE_HEIGHT_FOG_INCLUDED
#define UE_HEIGHT_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/ShaderVariablesEnvironments.hlsl"

static const float FLT_EPSILON2 = 0.01f;

float CalculateRayOriginTerm(float density, float heightFalloff, float heightOffset)
{
    // float MaxObserverHeightDifference = 65536.0f;

    // maxObserverHeight = min(FLT_MAX, heightOffset + MaxObserverHeightDifference);
    // Clamping the observer height to avoid numerical precision issues in the height fog equation. The max observer height is relative to the fog height.
    // float ObserverHeight = min(_WorldSpaceCameraPos.y, maxObserverHeight);

    // maxObserverHeight = 0;

    float exponent = heightFalloff * (_WorldSpaceCameraPos.y - heightOffset);
    return density * pow(2.0f, -exponent);
}

// Calculate the line integral of the ray from the camera to the receiver position through the fog density function
// The exponential fog density function is d = GlobalDensity * exp(-HeightFalloff * y)
float CalculateLineIntegralShared(float FogHeightFalloff, float RayDirectionY, float RayOriginTerms)
{
    float Falloff = max(-127.0f, FogHeightFalloff * RayDirectionY); // if it's lower than -127.0, then exp2() goes crazy in OpenGL's GLSL.
    float LineIntegral = (1.0f - exp2(-Falloff)) / Falloff;
    float LineIntegralTaylor = log(2.0) - (0.5 * pow(log(2.0), 2.)) * Falloff; // Taylor expansion around 0

    return RayOriginTerms * (abs(Falloff) > FLT_EPSILON2 ? LineIntegral : LineIntegralTaylor);
}

float4 GetExponentialHeightFogUE(float3 WorldPositionRelativeToCamera, float3 V) // camera to vertex
{
    const half MinFogOpacity = 1.0f - ExponentialFogColorParameter.w;
    if (MinFogOpacity < 1)
    {
        float3 WorldCameraOrigin = GetCurrentViewPosition();
        float3 CameraToReceiver = WorldPositionRelativeToCamera;

        // FogDensity * exp2(-FogHeightFalloff * (CameraWorldPosition.y - FogHeight))
        // float maxObserverHeight = 0;
        float RayOriginTerms = CalculateRayOriginTerm(ExponentialFogParameters.x, ExponentialFogParameters.y,
                                                      ExponentialFogParameters.z);
        float RayOriginTermsSecond = CalculateRayOriginTerm(ExponentialFogParameters2.x, ExponentialFogParameters2.y,
                                                            ExponentialFogParameters2.z);

        // const float MaxWorldObserverHeight = maxObserverHeight;

        // const float3 WorldObserverOrigin = float3(WorldCameraOrigin.x, min(WorldCameraOrigin.y, MaxWorldObserverHeight), WorldCameraOrigin.z); // Clamp Y to max height

        // Apply end fog distance from view projected on the XZ plane.
        const float CameraToReceiverLenXYSqr = dot(CameraToReceiver.xz, CameraToReceiver.xz);
        if (ExponentialFogParameters3.z > 0.0f && CameraToReceiverLenXYSqr > (ExponentialFogParameters3.z * ExponentialFogParameters3.z))
        {
            CameraToReceiver *= ExponentialFogParameters3.z / sqrt(max(1.0, CameraToReceiverLenXYSqr));
        }

        // CameraToReceiver.y += WorldCameraOrigin.y - WorldObserverOrigin.y; // Compensate this vector for clamping the observer height
        float CameraToReceiverLengthSqr = dot(CameraToReceiver, CameraToReceiver);
        float CameraToReceiverLengthInv = rsqrt(max(CameraToReceiverLengthSqr, 0.00000001f));
        // float CameraToReceiverLengthInv = rsqrt(CameraToReceiverLengthSqr);
        float CameraToReceiverLength = CameraToReceiverLengthSqr * CameraToReceiverLengthInv;
        half3 CameraToReceiverNormalized = CameraToReceiver * CameraToReceiverLengthInv;

        float RayLength = CameraToReceiverLength;
        //float RayLength2 = CameraToReceiverLength;
        float RayDirectionY = CameraToReceiver.y;
        //float RayDirectionY2 = CameraToReceiver.y;

        // Factor in StartDistance
        // _ExponentialFogParameters.w 
        float ExcludeDistance = ExponentialFogParameters.w;

        if (ExcludeDistance > 0)
        {
            float ExcludeIntersectionTime = ExcludeDistance * CameraToReceiverLengthInv;
            float CameraToExclusionIntersectionY = ExcludeIntersectionTime * CameraToReceiver.y;
            float ExclusionIntersectionY = WorldCameraOrigin.y + CameraToExclusionIntersectionY;
            float ExclusionIntersectionToReceiverY = CameraToReceiver.y - CameraToExclusionIntersectionY;

            // Calculate fog off of the ray starting from the exclusion distance, instead of starting from the camera
            RayLength = (1.0f - ExcludeIntersectionTime) * CameraToReceiverLength;
            RayDirectionY = ExclusionIntersectionToReceiverY;

            // height falloff * height
            float Exponent = max(-127.0f, ExponentialFogParameters.y * (ExclusionIntersectionY - ExponentialFogParameters.z));
            RayOriginTerms = ExponentialFogParameters.x * exp2(-Exponent);

            // _ExponentialFogParameters2.y : FogHeightFalloffSecond
            // _ExponentialFogParameters2.z : fog height second
            float ExponentSecond = max(-127.0f, ExponentialFogParameters2.y * (ExclusionIntersectionY - ExponentialFogParameters2.z));
            RayOriginTermsSecond = ExponentialFogParameters2.x * exp2(-ExponentSecond);
        }

        // if (_FogStartDistance2 > 0)
        // {
        //     float ExcludeIntersectionTime = _FogStartDistance2 * CameraToReceiverLengthInv;
        //     float CameraToExclusionIntersectionY = ExcludeIntersectionTime * CameraToReceiver.y;
        //     float ExclusionIntersectionY = WorldCameraOrigin.y + CameraToExclusionIntersectionY;
        //     float ExclusionIntersectionToReceiverY = CameraToReceiver.y - CameraToExclusionIntersectionY;
        //
        //     // Calculate fog off of the ray starting from the exclusion distance, instead of starting from the camera
        //     RayLength2 = (1.0f - ExcludeIntersectionTime) * CameraToReceiverLength;
        //     RayDirectionY2 = ExclusionIntersectionToReceiverY;
        //
        //     // _ExponentialFogParameters2.y : FogHeightFalloffSecond
        //     // _ExponentialFogParameters2.z : fog height second
        //     float ExponentSecond = max(-127.0f, ExponentialFogParameters2.y * (ExclusionIntersectionY - ExponentialFogParameters2.z));
        //     RayOriginTermsSecond = ExponentialFogParameters2.x * exp2(-ExponentSecond);
        // }

        // Calculate the "shared" line integral (this term is also used for the directional light inscattering) by adding the two line integrals together (from two different height falloffs and densities)
        // _ExponentialFogParameters.y : fog height falloff
        float ExponentialHeightLineIntegralShared1 = CalculateLineIntegralShared(ExponentialFogParameters.y, RayDirectionY, RayOriginTerms);
        float ExponentialHeightLineIntegralShared2 = CalculateLineIntegralShared(ExponentialFogParameters2.y, RayDirectionY, RayOriginTermsSecond);
        float ExponentialHeightLineIntegral1 = ExponentialHeightLineIntegralShared1 * RayLength;
        float ExponentialHeightLineIntegral2 = ExponentialHeightLineIntegralShared2 * RayLength;

        float ExponentialHeightLineIntegralShared = ExponentialHeightLineIntegralShared1 + ExponentialHeightLineIntegralShared2;
        float ExponentialHeightLineIntegral = ExponentialHeightLineIntegral1 + ExponentialHeightLineIntegral2;

        half3 InscatteringColor = ExponentialFogColorParameter.xyz;
        half3 DirectionalInscattering = 0;

        // if _ExponentialFogParameters3.w is negative then it's disabled, otherwise it holds directional inscattering start distance
        if (ExponentialFogParameters3.w >= 0)
        {
            float DirectionalInscatteringStartDistance = ExponentialFogParameters3.w;
            // Setup a cosine lobe around the light direction to approximate inscattering from the directional light off of the ambient haze;
            // const float UniformPhaseFunction = 1.0f / (4.0f * PI);
            half3 DirectionalLightInscattering = DirectionalInscatteringColor.xyz * pow(saturate(dot(CameraToReceiverNormalized, _MainLightPosition.xyz)), DirectionalInscatteringColor.w);

            // Calculate the line integral of the eye ray through the haze, using a special starting distance to limit the inscattering to the distance
            float DirExponentialHeightLineIntegral = ExponentialHeightLineIntegralShared * max(RayLength - DirectionalInscatteringStartDistance, 0.0f);
            // Calculate the amount of light that made it through the fog using the transmission equation
            half DirectionalInscatteringFogFactor = saturate(exp2(-DirExponentialHeightLineIntegral));
            // Final inscattering from the light
            DirectionalInscattering = DirectionalLightInscattering * (1. - DirectionalInscatteringFogFactor);
        }

        // Calculate the amount of light that made it through the fog using the transmission equation
        half ExpFogFactor = max(saturate(exp2(-ExponentialHeightLineIntegral)), MinFogOpacity);
        // Calculate the amount of light that made it through the fog using the transmission equation
        // half ExpFogFactor1 = max(saturate(exp2(-ExponentialHeightLineIntegral1)), MinFogOpacity);
        // half ExpFogFactor2 = max(saturate(exp2(-ExponentialHeightLineIntegral2)), MinFogOpacity);

        // ExponentialFogParameters2.w : FogCutoffDistance
        if (ExponentialFogParameters2.w > 0 && CameraToReceiverLength > ExponentialFogParameters2.w)
        {
            ExpFogFactor = 1;
            // ExpFogFactor1 = 1;
            // ExpFogFactor2 = 1;
            DirectionalInscattering = 0;
        }

        // float3 inScattering, extinction, scatterR, scatterM;
        // SkyRadiance(_WorldSpaceCameraPos, -V, _MainLightPosition.xyz,inScattering, extinction, scatterR, scatterM);

        // half3 fogColor = InscatteringColor * (1. - ExpFogFactor1);
        // half3 fogColor2 = ExponentialFogColorParameter2.rgb * (1. - ExpFogFactor2);

        half3 FogColor = (InscatteringColor) * (1 - ExpFogFactor) + DirectionalInscattering;

        return half4(FogColor, ExpFogFactor);
    }
    else
    {
        return half4(0., 0., 0., 1);
    }
}
#endif