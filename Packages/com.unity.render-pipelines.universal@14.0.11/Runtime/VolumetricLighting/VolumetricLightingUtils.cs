//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体积光照数学库
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class VolumetricLightingUtils
    {
        public static float MeanFreePathFromExtinction(float extinction)
        {
            return 1.0f / extinction;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
        {
            return new Vector3(extinction, extinction, extinction) - scattering;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
        {
            return meanFreePath * scattering;
        }
        
        public static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }
    }
    
    struct LocalVolumetricFogEngineData
    {
        public Vector3 scattering;    // [0, 1]
        public float extinction;    // [0, 1]
        public Vector3 textureTiling;
        public int invertFade;    // bool...
        public Vector3 textureScroll;
        public float rcpDistFadeLen;
        public Vector3 rcpPosFaceFade;
        public float endTimesRcpDistFadeLen;
        public Vector3 rcpNegFaceFade;
        public LocalVolumetricFogBlendingMode blendingMode;
        public Vector3 albedo;
        public LocalVolumetricFogFalloffMode falloffMode;

        public static LocalVolumetricFogEngineData GetNeutralValues()
        {
            LocalVolumetricFogEngineData data;

            data.scattering = Vector3.zero;
            data.extinction = 0;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade = 0;
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;
            data.falloffMode = LocalVolumetricFogFalloffMode.Linear;
            data.blendingMode = LocalVolumetricFogBlendingMode.Additive;
            data.albedo = Vector3.zero;

            return data;
        }
    } // struct VolumeProperties
    
    public enum LocalVolumetricFogFalloffMode
    {
        /// <summary>Fade using a linear function.</summary>
        Linear,
        /// <summary>Fade using an exponential function.</summary>
        Exponential,
    }

    /// <summary>Local volumetric fog blending mode.</summary>
    public enum LocalVolumetricFogBlendingMode
    {
        /// <summary>Replace the current fog, it is similar to disabling the blending.</summary>
        Overwrite   = 0,
        /// <summary>Additively blend fog volumes. This is the default behavior.</summary>
        Additive    = 1,
        /// <summary>Multiply the fog values when doing the blending. This is useful to make the fog density relative to other fog volumes.</summary>
        Multiply    = 2,
        /// <summary>Performs a minimum operation when blending the volumes.</summary>
        Min         = 3,
        /// <summary>Performs a maximum operation when blending the volumes.</summary>
        Max         = 4,
    }
    
    /// <summary>Select which mask mode to use for the local volumetric fog.</summary>
    public enum LocalVolumetricFogMaskMode
    {
        /// <summary>Use a 3D texture as mask.</summary>
        Texture,
        /// <summary>Use a material as mask. The material must use the "Fog Volume" material type in Shader Graph.</summary>
        Material,
    }
}