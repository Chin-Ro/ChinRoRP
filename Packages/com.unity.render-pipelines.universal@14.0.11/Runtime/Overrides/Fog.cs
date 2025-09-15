using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Environment/Fog", typeof(UniversalRenderPipeline))]
    public class Fog : VolumeComponent
    {
        [Tooltip("开启雾效.")]
        public BoolParameter enabled = new BoolParameter(false);
        
        [Header("Atmosphere Fog")]
        public BoolParameter enableAtmosphereFog = new BoolParameter(false);
        public FloatParameter baseHeight = new FloatParameter(1000.0f);
        public FloatParameter maximumHeight = new FloatParameter(5000.0f);
        public MinFloatParameter meanFreePath = new MinFloatParameter(8000.0f, 100.0f);
        [AdditionalProperty] public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.625f, 0.0f, 1.0f);
        [AdditionalProperty] public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [AdditionalProperty] public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);
        
        [Header("Height Fog")]
        public ClampedFloatParameter fogMaxOpacity = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter fogFirstDensity = new ClampedFloatParameter(0.05f, 0f, 0.05f);
        public ClampedFloatParameter fogFirstHeightFalloff = new ClampedFloatParameter(0.2f, 0.001f, 2f);
        public ClampedFloatParameter fogSecondDensity = new ClampedFloatParameter(0.05f, 0f, 0.05f);
        public ClampedFloatParameter fogSecondHeightFalloff = new ClampedFloatParameter(0.2f, 0.001f, 2f);
        public MinFloatParameter fogSecondHeight = new MinFloatParameter(0.0f, 0.0f);
        public ColorParameter fogColor = new ColorParameter(new Color(0.447f, 0.639f, 1.0f), true, false, true);
        public ClampedFloatParameter fogStartDistance = new ClampedFloatParameter(0f, 0f, 10000f);
        public ClampedFloatParameter fogEndDistance = new ClampedFloatParameter(0f, 0f, 20000f);
        public ClampedFloatParameter fogCutoffDistance = new ClampedFloatParameter(0f, 0f, 200000);
        
        [Header("InScattering")]
        public ClampedFloatParameter inScatterExponent = new ClampedFloatParameter(4f, 2f, 64f);
        public MinFloatParameter inScatteringStartDistance = new MinFloatParameter(0.0f, 0.0f);
        public ColorParameter inScatterColor = new ColorParameter(new Color(0.25f, 0.25f, 0.125f), true, false, true);
        public ClampedFloatParameter inScatterLuminance = new ClampedFloatParameter(0.5f, 0f, 1f);
        
        [Header("Light Shafts")]
        public BoolParameter enableLightShafts = new BoolParameter(false);
        public ClampedFloatParameter occlusionMaskDarkness = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
        public MinFloatParameter occlusionDepthRange = new MinFloatParameter(300.0f, 0.0f);
        public BoolParameter lightShaftBloom = new BoolParameter(false);
        public ClampedFloatParameter bloomScale = new ClampedFloatParameter(0.2f, 0.0f, 10.0f);
        public ClampedFloatParameter bloomThreshold = new ClampedFloatParameter(0.0f, 0.0f, 4.0f);
        public ClampedFloatParameter bloomMaxBrightness = new ClampedFloatParameter(100.0f, 0.0f, 100.0f);
        public ColorParameter bloomTint = new ColorParameter(Color.white);
        [AdditionalProperty]
        public IntParameter lightShaftBlurNumSamples = new IntParameter(12);
        [AdditionalProperty]
        public FloatParameter lightShaftFirstPassDistance = new FloatParameter(0.1f);
        
        [Header("Volumetric Fog")]
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        public ColorParameter albedo = new ColorParameter(Color.white);
        public ClampedFloatParameter extinctionScale = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        public FogDenoisingModeParameter denoisingMode = new FogDenoisingModeParameter(FogDenoisingMode.Gaussian);
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        [AdditionalProperty] public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);
        
        // Limit parameters for the fog quality
        internal const float minFogScreenResolutionPercentage = (1.0f / 16.0f) * 100;
        internal const float optimalFogScreenResolutionPercentage = (1.0f / 8.0f) * 100;
        internal const float maxFogScreenResolutionPercentage = 0.5f * 100;
        internal const int maxFogSliceCount = 512;
        
        [AdditionalProperty] public FogControlParameter fogControlMode = new FogControlParameter(FogControl.Balance);
        [AdditionalProperty] public ClampedFloatParameter screenResolutionPercentage = new ClampedFloatParameter(optimalFogScreenResolutionPercentage, minFogScreenResolutionPercentage, maxFogScreenResolutionPercentage);
        [AdditionalProperty] public ClampedIntParameter volumeSliceCount = new ClampedIntParameter(64, 1, maxFogSliceCount);
        [AdditionalProperty] public ClampedFloatParameter volumetricFogBudget = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [AdditionalProperty] public ClampedFloatParameter resolutionDepthRatio = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [AdditionalProperty] public BoolParameter directionalLightsOnly = new BoolParameter(false);
        public enum FogDenoisingMode
        {
            None = 0,
            Reprojection = 1 << 0,
            Gaussian = 1 << 1,
            Both = Reprojection | Gaussian
        }
        
        /// <summary>
        /// Options that control the quality and resource intensity of the volumetric fog.
        /// </summary>
        public enum FogControl
        {
            /// <summary>
            /// Use this mode if you want to change the fog control properties based on a higher abstraction level centered around performance.
            /// </summary>
            Balance,

            /// <summary>
            /// Use this mode if you want to have direct access to the internal properties that control volumetric fog.
            /// </summary>
            Manual
        }
        
        [Serializable]
        public sealed class FogControlParameter : VolumeParameter<FogControl>
        {
            /// <summary>
            /// Creates a new <see cref="FogControlParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public FogControlParameter(FogControl value, bool overrideState = false) : base(value, overrideState) { }
        }
        
        [Serializable]
        public sealed class FogDenoisingModeParameter : VolumeParameter<FogDenoisingMode>
        {
            public FogDenoisingModeParameter(FogDenoisingMode value, bool overrideState = false) : base(value, overrideState) { }
        }

        internal static bool IsFogEnabled(CameraData universalCamera)
        {
            return CoreUtils.IsSceneViewFogEnabled(universalCamera.camera) && VolumeManager.instance.stack.GetComponent<Fog>().enabled.value;
        }

        internal static bool IsVolumetricFogEnabled(CameraData universalCamera)
        {
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            bool a = fog.enableVolumetricFog.value;
            bool b = CoreUtils.IsSceneViewFogEnabled(universalCamera.camera);
            bool c = fog.enabled.value;

            return a && b && c;
        }
        
        internal static bool IsLightShaftsEnabled(CameraData universalCamera)
        {
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            bool a = fog.enableLightShafts.value;
            bool b = CoreUtils.IsSceneViewFogEnabled(universalCamera.camera);
            bool c = fog.enabled.value;

            return a && b && c;
        }
        
        internal static bool IsLightShaftsBloomEnabled(CameraData universalCamera)
        {
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            bool a = fog.enableLightShafts.value;
            bool b = fog.lightShaftBloom.value;
            bool c = CoreUtils.IsSceneViewFogEnabled(universalCamera.camera);
            bool d = fog.enabled.value;

            return a && b && c && d;
        }

        internal void UpdateShaderVariablesEnvironmentsCBFogParameters(ref ShaderVariablesEnvironments cb, CameraData universalCamera, bool isMainLightingExists)
        {
            int _FogEnabled = enabled.value ? 1 : 0;
            int _EnableAtmosphereFog = enableAtmosphereFog.value ? 1 : 0;
            int _EnableLightShafts = enableLightShafts.value ? 1 : 0;
            int _EnableVolumetricFog = enableVolumetricFog.value ? 1 : 0;
            
            cb._MipFogParameters = new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, _EnableAtmosphereFog);
            
            const float USELESS_VALUE = 0.0f;
            var ExponentialFogParameters = new Vector4(fogFirstDensity.value / 10f, fogFirstHeightFalloff.value / 10f,
                0, fogStartDistance.value);
            var ExponentialFogParameters2 = new Vector4(fogSecondDensity.value / 10f, fogSecondHeightFalloff.value / 10f,
               fogSecondHeight.value, fogCutoffDistance.value);
            var DirectionalInscatteringColor = new Vector4(
                inScatterLuminance.value * inScatterColor.value.r,
                inScatterLuminance.value * inScatterColor.value.g,
                inScatterLuminance.value * inScatterColor.value.b,
                inScatterExponent.value
            );
            
            var ExponentialFogParameters3 = new Vector4(USELESS_VALUE, USELESS_VALUE, fogEndDistance.value,  isMainLightingExists ? inScatteringStartDistance.value : -1);
            var ExponentialFogColorParameter = new Vector4(
                fogColor.value.r,
                fogColor.value.g,
                fogColor.value.b,
                enabled.value ?fogMaxOpacity.value : 0
            );
            
            cb._ExponentialFogParameters = ExponentialFogParameters;
            cb._ExponentialFogParameters2 = ExponentialFogParameters2;
            cb._DirectionalInscatteringColor = DirectionalInscatteringColor;
            cb._ExponentialFogParameters3 = ExponentialFogParameters3;
            cb._ExponentialFogColorParameter = ExponentialFogColorParameter;
            
            LocalVolumetricFogArtistParameters param = new LocalVolumetricFogArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            LocalVolumetricFogEngineData data = param.ConvertToEngineData();
            
            cb._HeightFogBaseScattering = enableVolumetricFog.value ? albedo.value : Vector4.one;
            
            float crBaseHeight = baseHeight.value;
            crBaseHeight -= universalCamera.camera.transform.position.y;
            
            float layerDepth = Mathf.Max(0.01f, maximumHeight.value - baseHeight.value);
            float H = VolumetricLightingUtils.ScaleHeightFromLayerDepth(layerDepth);
            cb._HeightFogExponents = new Vector4(1.0f / H, H, data.extinction, crBaseHeight);
            cb._GlobalFogParam1 = new Vector4(anisotropy.value, globalLightProbeDimmer.value, extinctionScale.value, _FogEnabled);
            int _VolumetricFilteringEnabled = ((int)denoisingMode.value & (int)Fog.FogDenoisingMode.Gaussian) != 0 ? 1 : 0;
            int _FogDirectionalOnly = directionalLightsOnly.value ? 1 : 0;
            cb._GlobalFogParam2 = new Vector4(_EnableLightShafts, _EnableVolumetricFog, _VolumetricFilteringEnabled, _FogDirectionalOnly);
        }
    }
}