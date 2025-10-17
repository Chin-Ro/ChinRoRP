//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  雾效组件
//--------------------------------------------------------------------------------------------------------

using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Environment/Fog", typeof(UniversalRenderPipeline))]
    public class Fog : VolumeComponent
    {
        [Tooltip("开启雾效.")]
        public BoolParameter enabled = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup, true);
        
        public BoolParameter enableAtmosphereFog = new BoolParameter(false);
        public FloatParameter baseHeight = new FloatParameter(1000.0f);
        public FloatParameter maximumHeight = new FloatParameter(5000.0f);
        public MinFloatParameter meanFreePath = new MinFloatParameter(8000.0f, 100.0f);
        [AdditionalProperty] public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.625f, 0.0f, 1.0f);
        [AdditionalProperty] public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [AdditionalProperty] public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);
        
        public ClampedFloatParameter fogFirstDensity = new ClampedFloatParameter(0.02f, 0f, 0.05f, true);
        public ClampedFloatParameter fogFirstHeightFalloff = new ClampedFloatParameter(0.2f, 0.001f, 2f, true);
        [Header("Second Fog Data")]
        public ClampedFloatParameter fogSecondDensity = new ClampedFloatParameter(0f, 0f, 0.05f, true);
        public ClampedFloatParameter fogSecondHeightFalloff = new ClampedFloatParameter(0.2f, 0.001f, 2f, true);
        public MinFloatParameter fogSecondHeight = new MinFloatParameter(0.0f, 0.0f, true);
        public ColorParameter fogInscatteringColor = new ColorParameter(new Color(0f, 0f, 0f), false, false, true);
        public ClampedFloatParameter fogMaxOpacity = new ClampedFloatParameter(1f, 0f, 1f);
        [AdditionalProperty] public ClampedFloatParameter skyContributeFactor = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter fogStartDistance = new ClampedFloatParameter(0f, 0f, 10000f);
        public ClampedFloatParameter fogEndDistance = new ClampedFloatParameter(0f, 0f, 20000f);
        public ClampedFloatParameter fogCutoffDistance = new ClampedFloatParameter(0f, 0f, 200000);
        
        public ClampedFloatParameter inScatterExponent = new ClampedFloatParameter(4f, 2f, 64f);
        public MinFloatParameter inScatteringStartDistance = new MinFloatParameter(0.0f, 0.0f);
        public ColorParameter inScatterColor = new ColorParameter(new Color(0f, 0f, 0f), false, false, true);
        
        public BoolParameter enableLightShafts = new BoolParameter(false, true);
        public ClampedFloatParameter occlusionMaskDarkness = new ClampedFloatParameter(0.25f, 0.0f, 1.0f, true);
        public MinFloatParameter occlusionDepthRange = new MinFloatParameter(300.0f, 0.0f);
        public BoolParameter lightShaftBloom = new BoolParameter(false, true);
        public ClampedFloatParameter bloomScale = new ClampedFloatParameter(0.2f, 0.0f, 10.0f);
        public ClampedFloatParameter bloomThreshold = new ClampedFloatParameter(0.0f, 0.0f, 4.0f);
        public ClampedFloatParameter bloomMaxBrightness = new ClampedFloatParameter(100.0f, 0.0f, 100.0f);
        public ColorParameter bloomTint = new ColorParameter(Color.white);
        
        public BoolParameter enableVolumetricFog = new BoolParameter(false, true);
        public ColorParameter albedo = new ColorParameter(Color.white);
        public ClampedFloatParameter extinctionScale = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.2f, -1.0f, 1.0f);
        
        // Limit parameters for the fog quality
        internal const float minFogScreenResolutionPercentage = (1.0f / 16.0f) * 100;
        internal const float optimalFogScreenResolutionPercentage = (1.0f / 8.0f) * 100;
        internal const float maxFogScreenResolutionPercentage = 0.5f * 100;
        internal const int maxFogSliceCount = 512;
        
        [AdditionalProperty] public BoolParameter directionalLightsOnly = new BoolParameter(false);

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
        
        internal static bool IsVolumetricReprojectionEnabled(CameraData cameraData)
        {
            bool a = IsVolumetricFogEnabled(cameraData);
            // We only enable volumetric re projection if we are processing the game view or a scene view with animated materials on
            bool b = cameraData.cameraType == CameraType.Game || (cameraData.cameraType == CameraType.SceneView && CoreUtils.AreAnimatedMaterialsEnabled(cameraData.camera));

            bool c = ((int)EnvironmentsRendererFeature.m_DenoisingMode & (int)FogDenoisingMode.Reprojection) != 0;
            
            return a && b && c;
        }

        internal void UpdateShaderVariablesEnvironmentsCBFogParameters(ref ShaderVariablesEnvironments cb, CameraData universalCamera, bool isMainLightingExists, bool m_EnableLightShafts, bool m_EnableVolumetricFog)
        {
            int _FogEnabled = enabled.value ? 1 : 0;
            int _EnableAtmosphereFog = enableAtmosphereFog.value ? 1 : 0;
            int _EnableLightShafts = m_EnableLightShafts ? 1 : 0;
            int _EnableVolumetricFog = m_EnableVolumetricFog ? 1 : 0;
            
            cb._MipFogParameters = new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, _EnableAtmosphereFog);
            
            const float USELESS_VALUE = 0.0f;
            var ExponentialFogParameters = new Vector4(fogFirstDensity.value / 10f, fogFirstHeightFalloff.value / 10f,
                0, fogStartDistance.value);
            var ExponentialFogParameters2 = new Vector4(fogSecondDensity.value / 10f, fogSecondHeightFalloff.value / 10f,
               fogSecondHeight.value, fogCutoffDistance.value);
            var DirectionalInscatteringColor = new Vector4(
                inScatterColor.value.r,
                inScatterColor.value.g,
                inScatterColor.value.b,
                inScatterExponent.value
            );
            
            var ExponentialFogParameters3 = new Vector4(USELESS_VALUE, USELESS_VALUE, fogEndDistance.value,  isMainLightingExists ? inScatteringStartDistance.value : -1);
            var ExponentialFogColorParameter = new Vector4(
                fogInscatteringColor.value.r,
                fogInscatteringColor.value.g,
                fogInscatteringColor.value.b,
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
            cb._GlobalFogParam1 = new Vector4(anisotropy.value, skyContributeFactor.value, extinctionScale.value, _FogEnabled);
            int _VolumetricFilteringEnabled = ((int)EnvironmentsRendererFeature.m_DenoisingMode & (int)FogDenoisingMode.Gaussian) != 0 ? 1 : 0;
            int _FogDirectionalOnly = directionalLightsOnly.value ? 1 : 0;
            cb._GlobalFogParam2 = new Vector4(_EnableLightShafts, _EnableVolumetricFog, _VolumetricFilteringEnabled, _FogDirectionalOnly);
        }
    }
}