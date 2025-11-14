namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmosphereUtils
    {
        internal const float KM_TO_M = 1000.0f;
        internal const float M_TO_KM = 1.0f / 1000.0f;
        
        internal const float MToSkyUnit = 0.001f;            // Meters to Kilometers
        internal const float SkyUnitToM = 1.0f / 0.001f;	    // Kilometers to Meters
        
        internal const int CVarSkyAtmosphere = 1;
        internal const int CVarSupportSkyAtmosphere = 1;
        internal const int CVarSupportSkyAtmosphereAffectsHeightFog = 1;
        
        ////////////////////////////////////////////////////////////////////////// Regular sky
        internal const float CVarSkyAtmosphereSampleCountMin = 2.0f;
        internal const float CVarSkyAtmosphereSampleCountMax = 32.0f;
        internal const float CVarSkyAtmosphereDistanceToSampleCountMax = 150.0f;
        internal const float CVarSkyAtmosphereSampleLightShadowmap = 1;
        
        ////////////////////////////////////////////////////////////////////////// Fast sky4
        internal const int CVarSkyAtmosphereFastSkyLUT = 1;
        internal const float CVarSkyAtmosphereFastSkyLUTSampleCountMin = 4.0f;
        internal const float CVarSkyAtmosphereFastSkyLUTSampleCountMax = 32.0f;
        internal const float CVarSkyAtmosphereFastSkyLUTDistanceToSampleCountMax = 150.0f;
        internal const int CVarSkyAtmosphereFastSkyLUTWidth = 192;
        internal const int CVarSkyAtmosphereFastSkyLUTHeight = 104;
        
        ////////////////////////////////////////////////////////////////////////// Aerial perspective
        internal const int CVarSkyAtmosphereAerialPerspectiveDepthTest = 1;
        
        ////////////////////////////////////////////////////////////////////////// Aerial perspective LUT
        internal const float CVarSkyAtmosphereAerialPerspectiveLUTDepthResolution = 16.0f;
        internal const float CVarSkyAtmosphereAerialPerspectiveLUTDepth = 96.0f;
        internal const float CVarSkyAtmosphereAerialPerspectiveLUTSampleCountMaxPerSlice = 2.0f;
        internal const int CVarSkyAtmosphereAerialPerspectiveLUTWidth = 32;
        internal const int CVarSkyAtmosphereAerialPerspectiveApplyOnOpaque = 1;
        
        ////////////////////////////////////////////////////////////////////////// Transmittance LUT
        internal const int CVarSkyAtmosphereTransmittanceLUT = 1;
        internal const float CVarSkyAtmosphereTransmittanceLUTSampleCount = 10.0f;
        internal const int CVarSkyAtmosphereTransmittanceLUTUseSmallFormat = 0;
        internal const int CVarSkyAtmosphereTransmittanceLUTWidth = 256;
        internal const int CVarSkyAtmosphereTransmittanceLUTHeight = 64;
        
        ////////////////////////////////////////////////////////////////////////// Multi-scattering LUT
        internal const float CVarSkyAtmosphereMultiScatteringLUTSampleCount = 15.0f;
        internal const float CVarSkyAtmosphereMultiScatteringLUTHighQuality = 0.0f;
        internal const int CVarSkyAtmosphereMultiScatteringLUTWidth = 32;
        internal const int CVarSkyAtmosphereMultiScatteringLUTHeight = 32;

        ////////////////////////////////////////////////////////////////////////// Distant Sky Light LUT
        internal const int CVarSkyAtmosphereDistantSkyLightLUT = 1;
        internal const float CVarSkyAtmosphereDistantSkyLightLUTAltitude = 6.0f;
        
        internal static void SetupSkyAtmosphereInternalCommonParameters(ref ShaderVariablesEnvironments InternalCommonParameters, SkyAtmosphere skyAtmosphere, CameraData universalCamera)
        {
            if (!SkyAtmosphere.IsSkyAtmosphereEnabled()) return;

            InternalCommonParameters.TransmittanceLutSizeAndInvSize =
                GetSizeAndInvSize(CVarSkyAtmosphereTransmittanceLUTWidth, CVarSkyAtmosphereTransmittanceLUTHeight);
            InternalCommonParameters.MultiScatteredLuminanceLutSizeAndInvSize =
                GetSizeAndInvSize(CVarSkyAtmosphereMultiScatteringLUTWidth, CVarSkyAtmosphereMultiScatteringLUTHeight);
            InternalCommonParameters.SkyViewLutSizeAndInvSize = GetSizeAndInvSize(CVarSkyAtmosphereFastSkyLUTWidth, CVarSkyAtmosphereFastSkyLUTHeight);

            const float SkyAtmosphereBaseSampleCount = 32.0f;
            const float AerialPerspectiveBaseSampleCountPerSlice = 1.0f;
            InternalCommonParameters.SampleCountMin = CVarSkyAtmosphereSampleCountMin;
            InternalCommonParameters.SampleCountMax =
                Mathf.Min(SkyAtmosphereBaseSampleCount * skyAtmosphere.TraceSampleCountScale.value, CVarSkyAtmosphereSampleCountMax);
            float DistanceToSampleCountMaxInv = CVarSkyAtmosphereDistanceToSampleCountMax;

            InternalCommonParameters.FastSkySampleCountMin = CVarSkyAtmosphereFastSkyLUTSampleCountMin;
            InternalCommonParameters.FastSkySampleCountMax = Mathf.Min(SkyAtmosphereBaseSampleCount * skyAtmosphere.TraceSampleCountScale.value,
                CVarSkyAtmosphereFastSkyLUTSampleCountMax);
            float FastSkyDistanceToSampleCountMaxInv = CVarSkyAtmosphereFastSkyLUTDistanceToSampleCountMax;

            InternalCommonParameters.CameraAerialPerspectiveVolumeDepthResolution = CVarSkyAtmosphereAerialPerspectiveLUTDepthResolution;
            InternalCommonParameters.CameraAerialPerspectiveVolumeDepthResolutionInv = 1.0f / CVarSkyAtmosphereAerialPerspectiveLUTDepthResolution;
            float CameraAerialPerspectiveVolumeDepthKm = CVarSkyAtmosphereAerialPerspectiveLUTDepth;
            CameraAerialPerspectiveVolumeDepthKm = CameraAerialPerspectiveVolumeDepthKm < 1.0f ? 1.0f : CameraAerialPerspectiveVolumeDepthKm;
            float CameraAerialPerspectiveVolumeDepthSliceLengthKm = CameraAerialPerspectiveVolumeDepthKm / CVarSkyAtmosphereAerialPerspectiveLUTDepthResolution;
            InternalCommonParameters.CameraAerialPerspectiveVolumeDepthSliceLengthKm = CameraAerialPerspectiveVolumeDepthSliceLengthKm;
            InternalCommonParameters.CameraAerialPerspectiveVolumeDepthSliceLengthKmInv = 1.0f / CameraAerialPerspectiveVolumeDepthSliceLengthKm;
            InternalCommonParameters.CameraAerialPerspectiveSampleCountPerSlice = Mathf.Max(AerialPerspectiveBaseSampleCountPerSlice,
                Mathf.Min(2.0f * skyAtmosphere.TraceSampleCountScale.value, CVarSkyAtmosphereAerialPerspectiveLUTSampleCountMaxPerSlice));

            InternalCommonParameters.TransmittanceSampleCount = CVarSkyAtmosphereTransmittanceLUTSampleCount;
            InternalCommonParameters.MultiScatteringSampleCount = CVarSkyAtmosphereMultiScatteringLUTSampleCount;

            InternalCommonParameters.SkyLuminanceFactor = new Vector3
            (
                skyAtmosphere.SkyLuminanceFactor.value.r, 
                skyAtmosphere.SkyLuminanceFactor.value.g,
                skyAtmosphere.SkyLuminanceFactor.value.b
            );
            InternalCommonParameters.SkyAndAerialPerspectiveLuminanceFactor = new Vector3
            (
                skyAtmosphere.SkyAndAerialPerspectiveLuminanceFactor.value.r,
                skyAtmosphere.SkyAndAerialPerspectiveLuminanceFactor.value.g, 
                skyAtmosphere.SkyAndAerialPerspectiveLuminanceFactor.value.b
            );
            
            InternalCommonParameters.AerialPespectiveViewDistanceScale = skyAtmosphere.AerialPespectiveViewDistanceScale.value;
            InternalCommonParameters.FogShowFlagFactor = skyAtmosphere.enable.value && CoreUtils.IsSceneViewSkyboxEnabled(universalCamera.camera) ? 1.0f : 0.0f;
            
            ValidateSampleCountValue(InternalCommonParameters.SampleCountMin);
            ValidateMaxSampleCountValue(InternalCommonParameters.SampleCountMax, InternalCommonParameters.SampleCountMin);
            ValidateSampleCountValue(InternalCommonParameters.FastSkySampleCountMin);
            ValidateMaxSampleCountValue(InternalCommonParameters.FastSkySampleCountMax, InternalCommonParameters.FastSkySampleCountMin);
            ValidateSampleCountValue(InternalCommonParameters.CameraAerialPerspectiveSampleCountPerSlice);
            ValidateSampleCountValue(InternalCommonParameters.TransmittanceSampleCount);
            ValidateSampleCountValue(InternalCommonParameters.MultiScatteringSampleCount);
            ValidateDistanceValue(DistanceToSampleCountMaxInv);
            ValidateDistanceValue(FastSkyDistanceToSampleCountMaxInv);
            
            // Derived values post validation
            InternalCommonParameters.DistanceToSampleCountMaxInv = 1.0f / DistanceToSampleCountMaxInv;
            InternalCommonParameters.FastSkyDistanceToSampleCountMaxInv = 1.0f / FastSkyDistanceToSampleCountMaxInv;
            InternalCommonParameters.CameraAerialPerspectiveVolumeSizeAndInvSize = GetSizeAndInvSize(CVarSkyAtmosphereAerialPerspectiveLUTWidth, CVarSkyAtmosphereAerialPerspectiveLUTWidth);
        }
        
        internal static Vector4 GetSizeAndInvSize(int width, int height)
        {
            return new Vector4(width, height, 1.0f / width, 1.0f / height);
        }
        
        internal static float ValidateDistanceValue(float value)
        {
            return value < 1.0E-4f ? 1.0E-4f : value;
        }

        internal static float ValidateSampleCountValue(float value)
        {
            return value < 1.0f ? 1.0f : value;
        }

        internal static float ValidateMaxSampleCountValue(float value, float minValue)
        {
            return value < minValue ? minValue : value;
        }
        
        internal static float GetValidAerialPerspectiveStartDepthInM(SkyAtmosphere skyAtmosphere, Camera camera)
        {
            float AerialPerspectiveStartDepthKm = skyAtmosphere.AerialPerspectiveStartDepth.value;
            AerialPerspectiveStartDepthKm = AerialPerspectiveStartDepthKm < 0.0f ? 0.0f : AerialPerspectiveStartDepthKm;
            // For sky reflection capture, the start depth can be super large. So we max it to make sure the triangle is never in front the NearClippingDistance.
            float StartDepthInM = Mathf.Max(AerialPerspectiveStartDepthKm * SkyAtmosphereUtils.KM_TO_M, camera.nearClipPlane);
            return StartDepthInM;
        }
    }
}