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
        
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);
        
        [Header("Light Source")]
        public MinFloatParameter SkyLuminanceMultiplier = new MinFloatParameter(1.0f, 1.0f);
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
        
        internal static float GetSkyLuminanceMultiplier()
        {
            var skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            if (skyAtmosphere != null && skyAtmosphere.enable.value)
            {
                return skyAtmosphere.SkyLuminanceMultiplier.value;
            }
            return 1.0f;
        }
        
        private FAtmosphereSetup atmosphereSetup = new FAtmosphereSetup();
        
        internal FAtmosphereSetup GetAtmosphereSetup()
        {
            return atmosphereSetup;
        }

        internal void CopyAtmosphereSetupToUniformShaderParameters(ref ShaderVariablesEnvironments cb, RenderingData renderingData)
        {
            if (!SkyAtmosphere.IsSkyAtmosphereEnabled()) return;
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

            cb.SkyAtmosphereEnabled = 1;
            cb.SkyLuminanceMultiplier = SkyLuminanceMultiplier.value;
            
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