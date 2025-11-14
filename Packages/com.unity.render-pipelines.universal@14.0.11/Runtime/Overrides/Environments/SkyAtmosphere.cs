//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  大气组件
//--------------------------------------------------------------------------------------------------------
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Environments/Sky Atmosphere", typeof(UniversalRenderPipeline))]
    public class SkyAtmosphere : VolumeComponent
    {
        // All distance here are in kilometer and scattering/absorptions coefficient in 1/kilometers.
        const float EarthBottomRadius = 6360.0f;
        const float EarthTopRadius = 6420.0f;
        const float EarthRayleighScaleHeight = 8.0f;
        const float EarthMieScaleHeight = 1.2f;
        
        // Float to a u8 rgb + float length can lose some precision but it is better UI wise.
        static Color RayleightScatteringRaw = new Color(0.005802f, 0.013558f, 0.033100f);
        static Color OtherAbsorptionRaw = new Color(0.000650f, 0.001881f, 0.000085f);
        
        public BoolParameter enable = new BoolParameter(false);
        
        [Header("Light Source")]
        public MinFloatParameter SkyLuminanceMultiplier = new MinFloatParameter(25.0f, 0.0f);
        public ClampedFloatParameter LightSourceAngle = new ClampedFloatParameter(0.5357f, 0.0f, 5.0f);
        public ClampedFloatParameter SecondLightSourceAngle = new ClampedFloatParameter(0.5357f, 0.0f, 5.0f);
        
        [Header("Planet")]
        public SkyAtmosphereTransformMode TransforMode = new SkyAtmosphereTransformMode(ESkyAtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin, false);
        public ClampedFloatParameter BottomRadius = new ClampedFloatParameter(EarthBottomRadius, 1.0f, 7000.0f);
        public ColorParameter GroundAlbedo = new ColorParameter(new Color(0.402f, 0.402f,0.402f), false, false, true);

        [Header("Atmosphere")] 
        public ClampedFloatParameter AtmosphereHeight = new ClampedFloatParameter(EarthTopRadius - EarthBottomRadius, 1.0f, 200.0f);
        public ClampedFloatParameter MultiScatteringFactor = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        [AdditionalProperty] public ClampedFloatParameter TraceSampleCountScale = new ClampedFloatParameter(1.0f, 0.25f, 8f);

        [Header("Atmosphere - Rayleigh")] 
        public ClampedFloatParameter RayleighScatteringScale = new ClampedFloatParameter(RayleightScatteringRaw.b, 0.0f, 2.0f);
        public ColorParameter RayleighScattering = new ColorParameter(RayleightScatteringRaw * (1.0f / RayleightScatteringRaw.b), false, false, true);
        public ClampedFloatParameter RayleighExponentialDistribution = new ClampedFloatParameter(EarthRayleighScaleHeight, 0.01f, 20.0f);

        [Header("Atmosphere - Mie")] 
        public ClampedFloatParameter MieScatteringScale = new ClampedFloatParameter(0.003996f, 0.0f, 5.0f);
        public ColorParameter MieScattering = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter MieAbsorptionScale = new ClampedFloatParameter(0.000444f, 0.0f, 5.0f);
        public ColorParameter MieAbsorption = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter MieAnisotropy = new ClampedFloatParameter(0.8f, 0.0f, 0.999f);
        public ClampedFloatParameter MieExponentialDistribution = new ClampedFloatParameter(EarthMieScaleHeight, 0.01f, 20.0f);

        [Header("Atmosphere - Absorption")] 
        public ClampedFloatParameter OtherAbsorptionScale = new ClampedFloatParameter(OtherAbsorptionRaw.g, 0.0f, 0.2f);
        public ColorParameter OtherAbsorption = new ColorParameter(OtherAbsorptionRaw * (1.0f / OtherAbsorptionRaw.g), false, false, true);
        [AdditionalProperty] public ClampedFloatParameter TipAltitude = new ClampedFloatParameter(25.0f, 0.0f, 60.0f);
        [AdditionalProperty] public ClampedFloatParameter TipValue = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [AdditionalProperty] public ClampedFloatParameter Width = new ClampedFloatParameter(15.0f, 0.01f, 20.0f);

        [Header("Art Direction")] 
        public ColorParameter SkyLuminanceFactor = new ColorParameter(Color.white, false, false,true);
        public ColorParameter SkyAndAerialPerspectiveLuminanceFactor = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter AerialPespectiveViewDistanceScale = new ClampedFloatParameter(1.0f, 0.0f, 3.0f);
        public ClampedFloatParameter HeightFogContribution = new ClampedFloatParameter(1f, 0.0f, 1.0f);
        public ClampedFloatParameter TransmittanceMinLightElevationAngle = new ClampedFloatParameter(-90.0f, -90.0f, 90.0f);
        public ClampedFloatParameter AerialPerspectiveStartDepth = new ClampedFloatParameter(0.1f, 0.001f, 10.0f);
        
        internal static bool IsSkyAtmosphereEnabled()
        {
            var skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            return skyAtmosphere != null && skyAtmosphere.enable.value;
        }
        
        private FAtmosphereSetup atmosphereSetup = new FAtmosphereSetup();
        
        internal FAtmosphereSetup GetAtmosphereSetup()
        {
            return atmosphereSetup;
        }

        internal void CopyAtmosphereSetupToUniformShaderParameters(ref ShaderVariablesEnvironments cb, RenderingData renderingData)
        {
            void TentToCoefficients(float TipAltitude, float TipValue, float Width, ref float LayerWidth, ref float LinTerm0, ref float LinTerm1, ref float ConstTerm0, ref float ConstTerm1)
            {
                if (Width > 0.0f && TipValue > 0.0f)
                {
                    float px = TipAltitude;
                    float py = TipValue;
                    float slope = TipValue / Width;
                    LayerWidth = px;
                    LinTerm0 = slope;
                    LinTerm1 = -slope;
                    ConstTerm0 = py - px * LinTerm0;
                    ConstTerm1 = py - px * LinTerm1;
                }
                else
                {
                    LayerWidth = 0.0f;
                    LinTerm0 = 0.0f;
                    LinTerm1 = 0.0f;
                    ConstTerm0 = 0.0f;
                    ConstTerm1 = 0.0f;
                }
            }
            
            atmosphereSetup.BottomRadiusKm = BottomRadius.value;
            atmosphereSetup.TopRadiusKm = BottomRadius.value + Mathf.Max(0.1f, AtmosphereHeight.value);
            atmosphereSetup.GroundAlbedo = GroundAlbedo.value;
            atmosphereSetup.MultiScatteringFactor = Mathf.Clamp(MultiScatteringFactor.value, 0.0f, 100.0f);
            
            // Rayleigh scattering
            {
                atmosphereSetup.RayleighScattering = RayleighScattering.value * RayleighScatteringScale.value;
                atmosphereSetup.RayleighDensityExpScale = -1.0f / RayleighExponentialDistribution.value;
            }
            
            // Mie scattering
            {
                atmosphereSetup.MieScattering = MieScattering.value * MieScatteringScale.value;
                atmosphereSetup.MieAbsorption = MieAbsorption.value * MieAbsorptionScale.value;

                atmosphereSetup.MieExtinction = atmosphereSetup.MieScattering + atmosphereSetup.MieAbsorption;
                atmosphereSetup.MiePhaseG = MieAnisotropy.value;
                atmosphereSetup.MieDensityExpScale = -1.0f / MieExponentialDistribution.value;
            }
            
            // Ozone
            {
                atmosphereSetup.AbsorptionExtinction = OtherAbsorption.value * OtherAbsorptionScale.value;
                TentToCoefficients(TipAltitude.value, TipValue.value, Width.value, ref atmosphereSetup.AbsorptionDensity0LayerWidth,
                    ref atmosphereSetup.AbsorptionDensity0LinearTerm, ref atmosphereSetup.AbsorptionDensity1LinearTerm,
                    ref atmosphereSetup.AbsorptionDensity0ConstantTerm, ref atmosphereSetup.AbsorptionDensity1ConstantTerm);
            }

            atmosphereSetup.TransmittanceMinLightElevationAngle = TransmittanceMinLightElevationAngle.value;

            switch (TransforMode.value)
            {
                case ESkyAtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin:
                    atmosphereSetup.PlanetCenterKm = new Vector3(0.0f, -atmosphereSetup.BottomRadiusKm, 0.0f);
                    break;
                case ESkyAtmosphereTransformMode.PlanetTopAtComponentTransform:
                    var volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (var volume in volumes)
                    {
                        foreach (var component in volume.profile.components)
                        {
                            if (component is SkyAtmosphere && component == this)
                            {
                                atmosphereSetup.PlanetCenterKm = new Vector3(0.0f, -atmosphereSetup.BottomRadiusKm, 0.0f) + volume.transform.position * SkyAtmosphereUtils.MToSkyUnit;
                            }
                        }
                    }
                    break;
                case ESkyAtmosphereTransformMode.PlanetCenterAtComponentTransform:
                    volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (var volume in volumes)
                    {
                        foreach (var component in volume.profile.components)
                        {
                            if (component is SkyAtmosphere && component == this)
                            {
                                atmosphereSetup.PlanetCenterKm = volume.transform.position * SkyAtmosphereUtils.MToSkyUnit;
                            }
                        }
                    }
                    break;
            }

            Vector3 SkyCameraTranslatedWorldOrigin;
            Matrix4x4 SkyViewLutReferential;
            Vector4 TempSkyPlanetData;
            atmosphereSetup.ComputeViewData(renderingData.cameraData.camera.transform.position, -renderingData.cameraData.camera.transform.position, renderingData.cameraData.camera.transform.forward,
                renderingData.cameraData.camera.transform.right, out SkyCameraTranslatedWorldOrigin, out TempSkyPlanetData, out SkyViewLutReferential);
            cb.SkyPlanetTranslatedWorldCenterAndViewHeight = TempSkyPlanetData;
            cb.SkyViewLutReferential = SkyViewLutReferential;
            cb.SkyCameraTranslatedWorldOrigin = SkyCameraTranslatedWorldOrigin;
            // Camera camera = renderingData.cameraData.camera;
            // float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            // float tanHalfFov = Mathf.Tan(fovY * 0.5f);
            //
            // float m00 = 1.0f / (camera.aspect * tanHalfFov);
            // float m11 = 1.0f / tanHalfFov;
            // float m22 = camera.nearClipPlane / (camera.nearClipPlane - camera.farClipPlane);
            // float m23 = (camera.farClipPlane * camera.nearClipPlane) / (camera.nearClipPlane - camera.farClipPlane);
            //
            // Matrix4x4 ProjectionMatrix = new Matrix4x4
            // (
            //     new Vector4(m00, 0, 0, 0),
            //     new Vector4(0, m11, 0, 0),
            //     new Vector4(0, 0, m22, m23),
            //     new Vector4(0, 0, 1, 0)
            //     );
            //
            // Matrix4x4 ScreenToClipMatrix = new Matrix4x4(
            //     new Vector4(1, 0, 0, 0), 
            //     new Vector4(0, 1, 0, 0), 
            //     new Vector4(0,0, ProjectionMatrix.m22, ProjectionMatrix.m32),
            //     new Vector4(0,0, 0,0));
            //
            // cb.ScreenToTranslatedWorld = CreateScreenToTranslatedWorldMatrix(camera);
            //cb.ScreenToTranslatedWorld = ScreenToClipMatrix * (renderingData.cameraData.GetGPUProjectionMatrix().inverse * renderingData.cameraData.GetViewMatrix().inverse);
            
            int mainLightIndex = renderingData.lightData.mainLightIndex;
            float SunSolidAngle = 2.0f * Mathf.PI * (1.0f - Mathf.Cos(0.5f * LightSourceAngle.value * Mathf.Deg2Rad));			// Solid angle from aperture https://en.wikipedia.org/wiki/Solid_angle 
            Color SunDiskOuterSpaceLuminance = renderingData.lightData.visibleLights[mainLightIndex].finalColor / (SunSolidAngle * Mathf.PI);
            cb.AtmosphereLightColor = renderingData.lightData.visibleLights[mainLightIndex].finalColor * SkyLuminanceMultiplier.value;
            cb.AtmosphereLightDiscLuminance = new Vector4(SunDiskOuterSpaceLuminance.r, SunDiskOuterSpaceLuminance.g, SunDiskOuterSpaceLuminance.b, 0.0f);
            cb.AtmosphereLightDiscCosHalfApexAngle_PPTrans = Mathf.Cos(0.5f * LightSourceAngle.value * Mathf.Deg2Rad);
            if (renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional)
            {
                float SecondSunSolidAngle = 2.0f * Mathf.PI * (1.0f - Mathf.Cos(0.5f * SecondLightSourceAngle.value * Mathf.Deg2Rad));
                Color SecondSunDiskOuterSpaceLuminance = renderingData.lightData.visibleLights[1].finalColor / (SecondSunSolidAngle * Mathf.PI);
                cb.SecondAtmosphereLightColor = renderingData.lightData.visibleLights[1].finalColor * SkyLuminanceMultiplier.value;
                cb.SecondAtmosphereLightDiscLuminance = new Vector4(SecondSunDiskOuterSpaceLuminance.r, SecondSunDiskOuterSpaceLuminance.g, SecondSunDiskOuterSpaceLuminance.b, 0.0f);
                cb.SecondAtmosphereLightDiscCosHalfApexAngle_PPTrans = Mathf.Cos(0.5f * SecondLightSourceAngle.value * Mathf.Deg2Rad);
            }
            
            cb.MultiScatteringFactor = atmosphereSetup.MultiScatteringFactor;
            cb.BottomRadiusKm = atmosphereSetup.BottomRadiusKm;
            cb.TopRadiusKm = atmosphereSetup.TopRadiusKm;
            cb.RayleighDensityExpScale = atmosphereSetup.RayleighDensityExpScale;
            cb.RayleighScattering = atmosphereSetup.RayleighScattering;
            cb.MieScattering = atmosphereSetup.MieScattering;
            cb.MieDensityExpScale = atmosphereSetup.MieDensityExpScale;
            cb.MieExtinction = atmosphereSetup.MieExtinction;
            cb.MiePhaseG = atmosphereSetup.MiePhaseG;
            cb.MieAbsorption = atmosphereSetup.MieAbsorption;
            cb.AbsorptionDensity0LayerWidth = atmosphereSetup.AbsorptionDensity0LayerWidth;
            cb.AbsorptionDensity0ConstantTerm = atmosphereSetup.AbsorptionDensity0ConstantTerm;
            cb.AbsorptionDensity0LinearTerm = atmosphereSetup.AbsorptionDensity0LinearTerm;
            cb.AbsorptionDensity1ConstantTerm = atmosphereSetup.AbsorptionDensity1ConstantTerm;
            cb.AbsorptionDensity1LinearTerm = atmosphereSetup.AbsorptionDensity1LinearTerm;
            cb.AbsorptionExtinction = atmosphereSetup.AbsorptionExtinction;
            cb.GroundAlbedo = atmosphereSetup.GroundAlbedo;
        }
    }

    struct FAtmosphereSetup
    {
        //////////////////////////////////////////////// Runtime
        
        public Vector3 PlanetCenterKm;		// In sky unit (kilometers)
        public float BottomRadiusKm;			// idem
        public float TopRadiusKm;				// idem
        
        public float MultiScatteringFactor;

        public Color RayleighScattering;// Unit is 1/km
        public float RayleighDensityExpScale;
        
        public Color MieScattering;		// Unit is 1/km
        public Color MieExtinction;		// idem
        public Color MieAbsorption;		// idem
        public float MieDensityExpScale;
        public float MiePhaseG;
        
        public Color AbsorptionExtinction;
        public float AbsorptionDensity0LayerWidth;
        public float AbsorptionDensity0ConstantTerm;
        public float AbsorptionDensity0LinearTerm;
        public float AbsorptionDensity1ConstantTerm;
        public float AbsorptionDensity1LinearTerm;
        
        public Color GroundAlbedo;
        public float TransmittanceMinLightElevationAngle;
        
        internal void ComputeViewData(Vector3 WorldCameraOrigin, Vector3 PreViewTranslation, Vector3 ViewForward, Vector3 ViewRight,
            out Vector3 SkyCameraTranslatedWorldOriginTranslatedWorld, out Vector4 SkyPlanetTranslatedWorldCenterAndViewHeight, out Matrix4x4 SkyViewLutReferential)
        {
            // The constants below should match the one in SkyAtmosphereCommon.ush
            // Always force to be 5 meters above the ground/sea level (to always see the sky and not be under the virtual planet occluding ray tracing) and lower for small planet radius
            float PlanetRadiusOffset = 0.005f;		
            
            float Offset = PlanetRadiusOffset * SkyAtmosphereUtils.SkyUnitToM;
            float BottomRadiusWorld = BottomRadiusKm * SkyAtmosphereUtils.SkyUnitToM;
            Vector3 PlanetCenterWorld = PlanetCenterKm * SkyAtmosphereUtils.SkyUnitToM;
            Vector3 PlanetCenterTranslatedWorld = PlanetCenterWorld + PreViewTranslation;
            Vector3 WorldCameraOriginTranslatedWorld = WorldCameraOrigin + PreViewTranslation;
            Vector3 PlanetCenterToCameraTranslatedWorld = WorldCameraOriginTranslatedWorld - PlanetCenterTranslatedWorld;
            float DistanceToPlanetCenterTranslatedWorld = Mathf.Sqrt(PlanetCenterToCameraTranslatedWorld.x * PlanetCenterToCameraTranslatedWorld.x +
                                                                       PlanetCenterToCameraTranslatedWorld.y * PlanetCenterToCameraTranslatedWorld.y +
                                                                       PlanetCenterToCameraTranslatedWorld.z * PlanetCenterToCameraTranslatedWorld.z);
            
            // If the camera is below the planet surface, we snap it back onto the surface.
            // This is to make sure the sky is always visible even if the camera is inside the virtual planet.
            SkyCameraTranslatedWorldOriginTranslatedWorld = DistanceToPlanetCenterTranslatedWorld < (BottomRadiusWorld + Offset) ?
                    PlanetCenterTranslatedWorld + (BottomRadiusWorld + Offset) * (PlanetCenterToCameraTranslatedWorld / DistanceToPlanetCenterTranslatedWorld) :
                    WorldCameraOriginTranslatedWorld;
            
            Vector3 Temp = (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld);
            float normalizedUp = Mathf.Sqrt(Temp.x * Temp.x + Temp.y * Temp.y + Temp.z * Temp.z);
            
            SkyPlanetTranslatedWorldCenterAndViewHeight = new Vector4(PlanetCenterTranslatedWorld.x, PlanetCenterTranslatedWorld.y, PlanetCenterTranslatedWorld.z, normalizedUp);
            
            // Now compute the referential for the SkyView LUT
            Vector3 PlanetCenterToWorldCameraPos = (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld) * SkyAtmosphereUtils.MToSkyUnit;
            Vector3 Up = PlanetCenterToWorldCameraPos.normalized;
            Vector3 Forward = ViewForward;          // This can make texel visible when the camera is rotating. Use constant world direction instead?
            //FVector3f	Left = normalize(cross(Forward, Up)); 
            Vector3	Left = Vector3.Normalize(Vector3.Cross(Forward, Up));
            float DotMainDir = Mathf.Abs(Vector3.Dot(Up, Forward));
            SkyViewLutReferential = Matrix4x4.identity;
            if (DotMainDir > 0.999f)
            {
                // When it becomes hard to generate a referential, generate it procedurally.
                // [ Duff et al. 2017, "Building an Orthonormal Basis, Revisited" ]
                float Sign = Up.y >= 0.0f ? 1.0f : -1.0f;
                float a = -1.0f / (Sign + Up.y);
                float b = Up.x * Up.z * a;
                Forward = new Vector3(Sign * b, -Sign * Up.z, 1 + Sign * a * Mathf.Pow(Up.z, 2.0f));
                Left = new Vector3(Sign + a * Mathf.Pow(Up.x, 2.0f), -Up.x, b);

                SkyViewLutReferential.SetColumn(0, -Forward);
                SkyViewLutReferential.SetColumn(1, Left);
                SkyViewLutReferential.SetColumn(2, Up);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
            else
            {
                // This is better as it should be more stable with respect to camera forward.
                Forward = Vector3.Cross(Up, Left);
                Forward.Normalize();
                SkyViewLutReferential.SetColumn(0, -Forward);
                SkyViewLutReferential.SetColumn(1, Left);
                SkyViewLutReferential.SetColumn(2, Up);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
        }

        private Vector2 GetAzimuthAndElevation(Vector3 Direction, Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ)
        {
            Vector3 NormalDir =  Direction.normalized;
            // Find projected point (on AxisX and AxisY, remove AxisZ component)
            Vector3 NoZProjDir = (NormalDir - Vector3.Dot(NormalDir, AxisZ) * AxisZ).normalized;
            // Figure out if projection is on right or left.
            float AzimuthSign = (Vector3.Dot(NoZProjDir, AxisY) < 0.0f) ? -1.0f : 1.0f;
            float ElevationSin = Vector3.Dot(NormalDir, AxisZ);
            float AzimuthCos = Vector3.Dot(NoZProjDir, AxisX);

            // Convert to Angles in Radian.
            return new Vector2(Mathf.Acos(AzimuthCos) * AzimuthSign, Mathf.Asin(ElevationSin));
        }
        
        // The following code is from SkyAtmosphere.usf and has been converted to lambda functions. 
        // It compute transmittance from the origin towards a sun direction. 

        Vector2 RayIntersectSphere(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector3 LocalPosition = RayOrigin - SphereOrigin;
            float LocalPositionSqr = Vector3.Dot(LocalPosition, LocalPosition);

            Vector3 QuadraticCoef;
            QuadraticCoef.x = Vector3.Dot(RayDirection, RayDirection);
            QuadraticCoef.y = 2.0f * Vector3.Dot(RayDirection, LocalPosition);
            QuadraticCoef.z = LocalPositionSqr - SphereRadius * SphereRadius;

            float Discriminant = QuadraticCoef.y * QuadraticCoef.y - 4.0f * QuadraticCoef.x * QuadraticCoef.z;

            // Only continue if the ray intersects the sphere
            Vector2 Intersections = new Vector2(-1.0f, -1.0f );
            if (Discriminant >= 0)
            {
                float SqrtDiscriminant = Mathf.Sqrt(Discriminant);
                Intersections.x = (-QuadraticCoef.y - 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
                Intersections.y = (-QuadraticCoef.y + 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
            }
            return Intersections;
        }
        
        // Nearest intersection of ray r,mu with sphere boundary
        float raySphereIntersectNearest(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector2 sol = RayIntersectSphere(RayOrigin, RayDirection, SphereOrigin, SphereRadius);
            float sol0 = sol.x;
            float sol1 = sol.y;
            if (sol0 < 0.0f && sol1 < 0.0f)
            {
                return -1.0f;
            }
            if (sol0 < 0.0f)
            {
                return Mathf.Max(0.0f, sol1);
            }
            else if (sol1 < 0.0f)
            {
                return Mathf.Max(0.0f, sol0);
            }
            return Mathf.Max(0.0f, Mathf.Min(sol0, sol1));
        }
        
        Color OpticalDepth(Vector3 RayOrigin, Vector3 RayDirection)
        {
            float TMax = raySphereIntersectNearest(RayOrigin, RayDirection, Vector3.zero, TopRadiusKm);

            Color OpticalDepthRGB = Color.clear;
            Vector3 VectorZero = Vector3.zero;
            if (TMax > 0.0f)
            {
                float SampleCount = 15.0f;
                float SampleStep = 1.0f / SampleCount;
                float SampleLength = SampleStep * TMax;
                for (float SampleT = 0.0f; SampleT < 1.0f; SampleT += SampleStep)
                {
                    Vector3 Pos = RayOrigin + RayDirection * (TMax * SampleT);
                    float viewHeight = (Vector3.Distance(Pos, VectorZero) - BottomRadiusKm);

                    float densityMie = Mathf.Max(0.0f, Mathf.Exp(MieDensityExpScale * viewHeight));
                    float densityRay = Mathf.Max(0.0f, Mathf.Exp(RayleighDensityExpScale * viewHeight));
                    float densityOzo = Mathf.Clamp(viewHeight < AbsorptionDensity0LayerWidth ?
                            AbsorptionDensity0LinearTerm * viewHeight + AbsorptionDensity0ConstantTerm :
                            AbsorptionDensity1LinearTerm * viewHeight + AbsorptionDensity1ConstantTerm,
                        0.0f, 1.0f);

                    Color SampleExtinction = densityMie * MieExtinction + densityRay * RayleighScattering + densityOzo * AbsorptionExtinction;
                    OpticalDepthRGB += SampleLength * SampleExtinction;
                }
            }

            return OpticalDepthRGB;
        }

        internal Color GetTransmittanceAtGroundLevel(Vector3 SunDirection)
        {
            // Assuming camera is along Z on (0,0,earthRadius + 500m)
            Vector3 WorldPos = new Vector3(0.0f, BottomRadiusKm + 0.5f, 0.0f);
            Vector2 AzimuthElevation = GetAzimuthAndElevation(SunDirection, Vector3.forward, Vector3.left, Vector3.up);
            AzimuthElevation.y = Mathf.Max(Mathf.Deg2Rad * TransmittanceMinLightElevationAngle, AzimuthElevation.y);
            Vector3 WorldDir = new Vector3(0.0f, Mathf.Sin(AzimuthElevation.y), Mathf.Cos(AzimuthElevation.y));
            Color OpticalDepthRGB = OpticalDepth(WorldPos, WorldDir);
            return new Color(Mathf.Exp(-OpticalDepthRGB.r), Mathf.Exp(-OpticalDepthRGB.g), Mathf.Exp(-OpticalDepthRGB.b), 1.0f);
        }
    }

    public enum ESkyAtmosphereTransformMode
    {
        PlanetTopAtAbsoluteWorldOrigin,
        PlanetTopAtComponentTransform,
        PlanetCenterAtComponentTransform,
    }
    
    [Serializable]
    public sealed class SkyAtmosphereTransformMode : VolumeParameter<ESkyAtmosphereTransformMode>
    {
        public SkyAtmosphereTransformMode(ESkyAtmosphereTransformMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}