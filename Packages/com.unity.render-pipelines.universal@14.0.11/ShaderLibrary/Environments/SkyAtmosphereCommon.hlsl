#ifndef SKYATMOSPHERE_COMMON_INCLUDED
#define SKYATMOSPHERE_COMMON_INCLUDED

#define FarDepthValue (UNITY_REVERSED_Z ? 0 : 1)

#define M_TO_SKY_UNIT 0.001f
#define SKY_UNIT_TO_CM (1.0f / M_TO_SKY_UNIT)
// Float accuracy offset in Sky unit (km, so this is 1m). Should match the one in FAtmosphereSetup::ComputeViewData
#define PLANET_RADIUS_OFFSET 0.001f

void fromTransmittanceLutUVs(
    out float ViewHeight, out float ViewZenithCosAngle,
    in float BottomRadius, in float TopRadius, in float2 UV)
{
    float Xmu = UV.x;
    float Xr = UV.y;

    float H = sqrt(TopRadius * TopRadius - BottomRadius * BottomRadius);
    float Rho = H * Xr;
    ViewHeight = sqrt(Rho * Rho + BottomRadius * BottomRadius);

    float Dmin = TopRadius - ViewHeight;
    float Dmax = Rho + H;
    float D = Dmin + Xmu * (Dmax - Dmin);
    ViewZenithCosAngle = D == 0.0f ? 1.0f : (H * H - Rho * Rho - D * D) / (2.0f * ViewHeight * D);
    ViewZenithCosAngle = clamp(ViewZenithCosAngle, -1.0f, 1.0f);
}

void getTransmittanceLutUvs(
	in float viewHeight, in float viewZenithCosAngle, in float BottomRadius, in float TopRadius,
	out float2 UV)
{
	float H = sqrt(max(0.0f, TopRadius * TopRadius - BottomRadius * BottomRadius));
	float Rho = sqrt(max(0.0f, viewHeight * viewHeight - BottomRadius * BottomRadius));

	float Discriminant = viewHeight * viewHeight * (viewZenithCosAngle * viewZenithCosAngle - 1.0f) + TopRadius * TopRadius;
	float D = max(0.0f, (-viewHeight * viewZenithCosAngle + sqrt(Discriminant))); // Distance to atmosphere boundary

	float Dmin = TopRadius - viewHeight;
	float Dmax = Rho + H;
	float Xmu = (D - Dmin) / (Dmax - Dmin);
	float Xr = Rho / H;

	UV = float2(Xmu, Xr);
	//UV = float2(fromUnitToSubUvs(UV.x, TRANSMITTANCE_TEXTURE_WIDTH), fromUnitToSubUvs(UV.y, TRANSMITTANCE_TEXTURE_HEIGHT)); // No real impact so off
}

/**
 * Returns near intersection in x, far intersection in y, or both -1 if no intersection.
 * RayDirection does not need to be unit length.
 */
float2 RayIntersectSphere(float3 RayOrigin, float3 RayDirection, float4 Sphere)
{
    float3 LocalPosition = RayOrigin - Sphere.xyz;
    float LocalPositionSqr = dot(LocalPosition, LocalPosition);

    float3 QuadraticCoef;
    QuadraticCoef.x = dot(RayDirection, RayDirection);
    QuadraticCoef.y = 2 * dot(RayDirection, LocalPosition);
    QuadraticCoef.z = LocalPositionSqr - Sphere.w * Sphere.w;

    float Discriminant = QuadraticCoef.y * QuadraticCoef.y - 4 * QuadraticCoef.x * QuadraticCoef.z;

    float2 Intersections = -1;

    // Only continue if the ray intersects the sphere
    UNITY_FLATTEN
    if (Discriminant >= 0)
    {
        float SqrtDiscriminant = sqrt(Discriminant);
        Intersections = (-QuadraticCoef.y + float2(-1, 1) * SqrtDiscriminant) / (2 * QuadraticCoef.x);
    }

    return Intersections;
}

////////////////////////////////////////////////////////////
// Participating medium properties
////////////////////////////////////////////////////////////

float3 GetAlbedo(float3 Scattering, float3 Extinction)
{
	return Scattering / max(0.001f, Extinction);
}

struct MediumSampleRGB
{
	float3 Scattering;
	float3 Absorption;
	float3 Extinction;

	float3 ScatteringMie;
	float3 AbsorptionMie;
	float3 ExtinctionMie;

	float3 ScatteringRay;
	float3 AbsorptionRay;
	float3 ExtinctionRay;

	float3 ScatteringOzo;
	float3 AbsorptionOzo;
	float3 ExtinctionOzo;

	float3 Albedo;
};

// If this is changed, please also update USkyAtmosphereComponent::GetTransmittance 
MediumSampleRGB SampleAtmosphereMediumRGB(in float3 WorldPos)
{
	const float SampleHeight = max(0.0, (length(WorldPos) - BottomRadiusKm));

	const float DensityMie = exp(MieDensityExpScale * SampleHeight);

	const float DensityRay = exp(RayleighDensityExpScale * SampleHeight);

	const float DensityOzo = SampleHeight < AbsorptionDensity0LayerWidth ?
		saturate(AbsorptionDensity0LinearTerm * SampleHeight + AbsorptionDensity0ConstantTerm) :	// We use saturate to allow the user to create plateau, and it is free on GCN.
		saturate(AbsorptionDensity1LinearTerm * SampleHeight + AbsorptionDensity1ConstantTerm);

	MediumSampleRGB s;

	s.ScatteringMie = DensityMie * MieScattering.rgb;
	s.AbsorptionMie = DensityMie * MieAbsorption.rgb;
	s.ExtinctionMie = DensityMie * MieExtinction.rgb;

	s.ScatteringRay = DensityRay * RayleighScattering.rgb;
	s.AbsorptionRay = 0.0f;
	s.ExtinctionRay = s.ScatteringRay + s.AbsorptionRay;

	s.ScatteringOzo = 0.0f;
	s.AbsorptionOzo = DensityOzo * AbsorptionExtinction.rgb;
	s.ExtinctionOzo = s.ScatteringOzo + s.AbsorptionOzo;

	s.Scattering = s.ScatteringMie + s.ScatteringRay + s.ScatteringOzo;
	s.Absorption = s.AbsorptionMie + s.AbsorptionRay + s.AbsorptionOzo;
	s.Extinction = s.ExtinctionMie + s.ExtinctionRay + s.ExtinctionOzo;
	s.Albedo = GetAlbedo(s.Scattering, s.Extinction);

	return s;
}
#endif