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
        const float earthBottomRadius = 6360.0f;
        const float earthTopRadius = 6420.0f;
        const float earthRayleighScaleHeight = 8.0f;
        const float earthMieScaleHeight = 1.2f;
        
        // Float to a u8 rgb + float length can lose some precision but it is better UI wise.
        static Color rayleightScatteringRaw = new Color(5.802f, 13.558f, 33.1f);
        static Color otherAbsorptionRaw = new Color(0.000650f, 0.001881f, 0.000085f);
        
        public BoolParameter enable = new BoolParameter(false);
        
        [Header("Planet")]
        public TransformModeParameter transformMode = new TransformModeParameter(TransformMode.PlanetTopAtAbsoluteWorldOrigin, false);
        public ClampedFloatParameter bottomRadius = new ClampedFloatParameter(earthBottomRadius, 1.0f, 7000.0f);
        public ColorParameter groundAlbedo = new ColorParameter(new Color32(170,170,170,255), false, false, true);

        [Header("Atmosphere")] 
        public ClampedFloatParameter atmosphereHeight = new ClampedFloatParameter(earthTopRadius - earthBottomRadius, 1.0f, 200.0f);
        public ClampedFloatParameter multiScatteringFactor = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        [AdditionalProperty] public ClampedFloatParameter traceSampleCountScale = new ClampedFloatParameter(1.0f, 0.25f, 8f);

        [Header("Atmosphere - Rayleigh")] 
        public ClampedFloatParameter rayleighScatteringScale = new ClampedFloatParameter(rayleightScatteringRaw.b, 0.0f, 2.0f);
        public ColorParameter rayleighScattering = new ColorParameter(rayleightScatteringRaw * (1.0f / rayleightScatteringRaw.b));
        public ClampedFloatParameter rayleighExponentialDistribution = new ClampedFloatParameter(earthRayleighScaleHeight, 0.01f, 20.0f);

        [Header("Atmosphere - Mie")] 
        public ClampedFloatParameter mieScatteringScale = new ClampedFloatParameter(0.003996f, 0.0f, 5.0f);
        public ColorParameter mieScattering = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter mieAbsorptionScale = new ClampedFloatParameter(0.000444f, 0.0f, 5.0f);
        public ColorParameter mieAbsorption = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter mieExponentialDistribution = new ClampedFloatParameter(earthMieScaleHeight, 0.01f, 20.0f);

        [Header("Atmosphere - Absorption")] 
        public ClampedFloatParameter otherAbsorptionScale = new ClampedFloatParameter(otherAbsorptionRaw.g, 0.0f, 0.2f);
        public ColorParameter otherAbsorption = new ColorParameter(otherAbsorptionRaw * (1.0f / otherAbsorptionRaw.g));
        [AdditionalProperty] public ClampedFloatParameter tipAltitude = new ClampedFloatParameter(25.0f, 0.0f, 60.0f);
        [AdditionalProperty] public ClampedFloatParameter tipValue = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [AdditionalProperty] public ClampedFloatParameter width = new ClampedFloatParameter(15.0f, 0.01f, 20.0f);

        [Header("Art Direction")] 
        public ColorParameter skyLuminanceFactor = new ColorParameter(Color.white, false, false,true);
        public ColorParameter skyAndAerialPerspectiveLuminanceFactor = new ColorParameter(Color.white, false, false, true);
        public ClampedFloatParameter aerialPespectiveViewDistanceScale = new ClampedFloatParameter(1.0f, 0.0f, 3.0f);
        public ClampedFloatParameter heightFogContribution = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        public ClampedFloatParameter transmittanceMinLightElevationAngle = new ClampedFloatParameter(-90.0f, -90.0f, 90.0f);
        public ClampedFloatParameter aerialPerspectiveStartDepth = new ClampedFloatParameter(0.1f, 0.001f, 10.0f);
        
        internal static bool IsSkyAtmosphereEnabled()
        {
            var skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            return skyAtmosphere != null && skyAtmosphere.enable.value;
        }
    }

    public enum TransformMode
    {
        PlanetTopAtAbsoluteWorldOrigin,
        PlanetTopAtComponentTransform,
        PlanetCenterAtComponentTransform,
    }
    
    [Serializable]
    public sealed class TransformModeParameter : VolumeParameter<TransformMode>
    {
        public TransformModeParameter(TransformMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}