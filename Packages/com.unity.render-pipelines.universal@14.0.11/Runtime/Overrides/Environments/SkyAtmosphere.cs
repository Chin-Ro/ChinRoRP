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
        
        [Header("Planet")]
        public SkyAtmosphereTransformMode TransforMode = new SkyAtmosphereTransformMode(ESkyAtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin, false);
        public ClampedFloatParameter BottomRadius = new ClampedFloatParameter(EarthBottomRadius, 1.0f, 7000.0f);
        public ColorParameter GroundAlbedo = new ColorParameter(new Color32(170,170,170,255), false, false, true);

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
        public ClampedFloatParameter HeightFogContribution = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        public ClampedFloatParameter TransmittanceMinLightElevationAngle = new ClampedFloatParameter(-90.0f, -90.0f, 90.0f);
        public ClampedFloatParameter AerialPerspectiveStartDepth = new ClampedFloatParameter(0.1f, 0.001f, 10.0f);
        
        internal static bool IsSkyAtmosphereEnabled()
        {
            var skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            return skyAtmosphere != null && skyAtmosphere.enable.value;
        }
        
        private FAtmosphereSetup atmosphereSetup = new FAtmosphereSetup();

        internal void CopyAtmosphereSetupToUniformShaderParameters(ref ShaderVariablesEnvironments cb, CameraData cameraData)
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
            atmosphereSetup.ComputeViewData(cameraData.camera.transform.position, -cameraData.camera.transform.position, cameraData.camera.transform.forward,
                cameraData.camera.transform.right, out SkyCameraTranslatedWorldOrigin, out TempSkyPlanetData, out SkyViewLutReferential);
            cb.SkyPlanetTranslatedWorldCenterAndViewHeight = TempSkyPlanetData;
            cb.SkyViewLutReferential = SkyViewLutReferential;
            cb.SkyCameraTranslatedWorldOrigin = SkyCameraTranslatedWorldOrigin;
            Matrix4x4 ScreenToClipMatrix = new Matrix4x4(
                new Vector4(1, 0, 0, 0), 
                new Vector4(0, 1, 0, 0), 
                new Vector4(0,0, cameraData.GetProjectionMatrix().m22, cameraData.GetProjectionMatrix().m23),
                new Vector4(0,0, -1,0));
            cb.ScreenToTranslatedWorld = ScreenToClipMatrix * cameraData.GetProjectionMatrix().inverse * cameraData.GetViewMatrix().inverse;
            
            Debug.Log(cameraData.GetProjectionMatrix());
            Debug.LogWarning(cameraData.GetGPUProjectionMatrix());

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
                Forward = new Vector3( 1 + Sign * a * Mathf.Pow(Up.z, 2.0f), Sign * b, -Sign * Up.z );
                Left = new Vector3(b,  Sign + a * Mathf.Pow(Up.x, 2.0f), -Up.x );

                SkyViewLutReferential.SetColumn(0, Left);
                SkyViewLutReferential.SetColumn(1, Up);
                SkyViewLutReferential.SetColumn(2, Forward);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
            else
            {
                // This is better as it should be more stable with respect to camera forward.
                Forward = Vector3.Cross(Up, Left);
                Forward.Normalize();
                SkyViewLutReferential.SetColumn(0, Left);
                SkyViewLutReferential.SetColumn(1, Up);
                SkyViewLutReferential.SetColumn(2, Forward);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
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