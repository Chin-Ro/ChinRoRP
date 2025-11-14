
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmosphereLookUpTablesPass : ScriptableRenderPass
    {
        private const int GroupSize = 8;
        const float GroupSizeInv = 1.0f / GroupSize;

        private RTHandle TransmittanceLut;
        private RTHandle MultiScatteredLuminanceLut;
        private RTHandle SkyAtmosphereViewLutTexture;
        private RTHandle SkyAtmosphereCameraAerialPerspectiveVolume;
        private RTHandle SkyAtmosphereCameraAerialPerspectiveVolumeMieOnly;
        private RTHandle SkyAtmosphereCameraAerialPerspectiveVolumeRayOnly;
        
        private RenderTextureDescriptor descriptor;

        private ComputeShader m_SkyAtmosphereLookUpTablesCS;
        private int m_TransmittanceLutKernel;
        private int m_MultiScatteredLuminanceLutKernel;
        private int m_DisatantSkyLightLutKernel;
        private int m_SkyViewLutKernel;
        private int m_CameraAerialPerspectiveVolumeKernel;

        private ComputeBuffer UniformSphereSamplesBuffer = new ComputeBuffer(GroupSize * GroupSize, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);
        private ComputeBuffer DistantSkyLightLutBuffer;
        private Vector4[] Dest = new Vector4[GroupSize * GroupSize];
        
        public SkyAtmosphereLookUpTablesPass(EnvironmentsData data, RenderPassEvent evt)
        {
            renderPassEvent = evt;
            m_SkyAtmosphereLookUpTablesCS = data.skyAtmosphereLookUpTablesCS;
            
            for (uint i = 0; i < GroupSize; ++i)
            {
                for (uint j = 0; j < GroupSize; ++j)
                {
                    float u0 = (i + Random.Range(0f, 1f)) * GroupSizeInv;
                    float u1 = (j + Random.Range(0f, 1f)) * GroupSizeInv;
                    
                    float a = 1.0f - 2.0f * u0;
                    float b = Mathf.Sqrt(1.0f - a * a);
                    float phi = 2 * Mathf.PI * u1;

                    uint idx = j * GroupSize + i;
                    Dest[idx].x = b * Mathf.Cos(phi);
                    Dest[idx].y = b * Mathf.Sin(phi);
                    Dest[idx].z = a;
                    Dest[idx].w = 0.0f;
                }
                
                UniformSphereSamplesBuffer.SetData(Dest); 
            }
            
            m_TransmittanceLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderTransmittanceLutCS");
            m_MultiScatteredLuminanceLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderMultiScatteredLuminanceLutCS");
            m_DisatantSkyLightLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderDistantSkyLightLutCS");
            m_SkyViewLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderSkyViewLutCS");
            m_CameraAerialPerspectiveVolumeKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderCameraAerialPerspectiveVolumeCS");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Transmittance LUT
            if (SkyAtmosphereUtils.CVarSkyAtmosphereTransmittanceLUT > 0)
            {
                descriptor.width = SkyAtmosphereUtils.CVarSkyAtmosphereTransmittanceLUTWidth;
                descriptor.height = SkyAtmosphereUtils.CVarSkyAtmosphereTransmittanceLUTHeight;
                descriptor.dimension = TextureDimension.Tex2D;
                descriptor.graphicsFormat = SkyAtmosphereUtils.CVarSkyAtmosphereTransmittanceLUTUseSmallFormat > 0
                    ? GraphicsFormat.R8G8B8_UNorm
                    : GraphicsFormat.B10G11R11_UFloatPack32;
                descriptor.useMipMap = false;
                descriptor.msaaSamples = 1;
                descriptor.enableRandomWrite = true;
                RenderingUtils.ReAllocateIfNeeded(ref TransmittanceLut, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "TransmittanceLut");
            }
            
            // Multi-Scattering LUT
            {
                descriptor.width = SkyAtmosphereUtils.CVarSkyAtmosphereMultiScatteringLUTWidth;
                descriptor.height = SkyAtmosphereUtils.CVarSkyAtmosphereMultiScatteringLUTHeight;
                descriptor.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                RenderingUtils.ReAllocateIfNeeded(ref MultiScatteredLuminanceLut, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "MultiScatteredLuminanceLut");
            }

            if (SkyAtmosphereUtils.CVarSkyAtmosphereDistantSkyLightLUT > 0)
            {
                DistantSkyLightLutBuffer ??= new ComputeBuffer(1, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Structured);
            }

            // Sky View LUT
            {
                descriptor.width = SkyAtmosphereUtils.CVarSkyAtmosphereFastSkyLUTWidth;
                descriptor.height = SkyAtmosphereUtils.CVarSkyAtmosphereFastSkyLUTHeight;
                RenderingUtils.ReAllocateIfNeeded(ref SkyAtmosphereViewLutTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "SkyAtmosphereViewLutTexture");
            }

            // Camera Aerial Perspective Volume
            {
                descriptor.width = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveLUTWidth;
                descriptor.height = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveLUTWidth;
                descriptor.volumeDepth = (int)SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveLUTDepthResolution;
                descriptor.dimension = TextureDimension.Tex3D;
                descriptor.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
                RenderingUtils.ReAllocateIfNeeded(ref SkyAtmosphereCameraAerialPerspectiveVolume, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "SkyAtmosphereCameraAerialPerspectiveVolume");
            }
            
            bool bSeparatedAtmosphereMieRayLeigh = false; // todo: Volumetric Clouds need this.

            if (bSeparatedAtmosphereMieRayLeigh)
            {
                RenderingUtils.ReAllocateIfNeeded(ref SkyAtmosphereCameraAerialPerspectiveVolumeMieOnly, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "SkyAtmosphereCameraAerialPerspectiveVolumeMieOnly");
                RenderingUtils.ReAllocateIfNeeded(ref SkyAtmosphereCameraAerialPerspectiveVolumeRayOnly, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "SkyAtmosphereCameraAerialPerspectiveVolumeRayOnly");
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!SkyAtmosphere.IsSkyAtmosphereEnabled() || m_SkyAtmosphereLookUpTablesCS == null) return;
            
            var skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            bool bHighQualityMultiScattering = SkyAtmosphereUtils.CVarSkyAtmosphereMultiScatteringLUTHighQuality > 0.0f;
            bool bSecondAtmosphereLightEnabled = renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional;
            bool bSeparatedAtmosphereMieRayLeigh = false; // todo: Volumetric Clouds need this.
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere LookUpTables")))
            {
                // Transmittance LUT
                if (SkyAtmosphereUtils.CVarSkyAtmosphereTransmittanceLUT > 0)
                {
                    int threadGroupX = UniversalUtils.DivRoundUp(TransmittanceLut.rt.width, GroupSize);
                    int threadGroupY = UniversalUtils.DivRoundUp(TransmittanceLut.rt.height, GroupSize);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, EnvironmentConstants.TransmittanceLutUAV, TransmittanceLut);
                    cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, threadGroupX, threadGroupY, 1);
                    cmd.SetGlobalTexture(EnvironmentConstants.TransmittanceLutTexture, TransmittanceLut);
                }
                
                // Multi-Scattering LUT
                {
                    int threadGroupX = UniversalUtils.DivRoundUp(MultiScatteredLuminanceLut.rt.width, GroupSize);
                    int threadGroupY = UniversalUtils.DivRoundUp(MultiScatteredLuminanceLut.rt.height, GroupSize);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED", bHighQualityMultiScattering);
                    m_SkyAtmosphereLookUpTablesCS.SetTexture(m_MultiScatteredLuminanceLutKernel,EnvironmentConstants.TransmittanceLutTexture, TransmittanceLut);
                    cmd.SetComputeBufferParam(m_SkyAtmosphereLookUpTablesCS, m_MultiScatteredLuminanceLutKernel, EnvironmentConstants.UniformSphereSamplesBuffer, UniformSphereSamplesBuffer);
                    cmd.SetComputeIntParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants.UniformSphereSamplesBufferSampleCount, GroupSize);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_MultiScatteredLuminanceLutKernel, EnvironmentConstants.MultiScatteredLuminanceLutUAV, MultiScatteredLuminanceLut);
                    cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_MultiScatteredLuminanceLutKernel, threadGroupX, threadGroupY, 1);
                    cmd.SetGlobalTexture(EnvironmentConstants.MultiScatteredLuminanceLutTexture, MultiScatteredLuminanceLut);
                }
                
                // Distant Sky Light LUT
                if (SkyAtmosphereUtils.CVarSkyAtmosphereDistantSkyLightLUT > 0)
                {
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", bSecondAtmosphereLightEnabled);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_DisatantSkyLightLutKernel, EnvironmentConstants.TransmittanceLutTexture, TransmittanceLut);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_DisatantSkyLightLutKernel, EnvironmentConstants.MultiScatteredLuminanceLutTexture, MultiScatteredLuminanceLut);
                    cmd.SetComputeBufferParam(m_SkyAtmosphereLookUpTablesCS, m_DisatantSkyLightLutKernel, EnvironmentConstants.UniformSphereSamplesBuffer, UniformSphereSamplesBuffer);
                    cmd.SetComputeBufferParam(m_SkyAtmosphereLookUpTablesCS, m_DisatantSkyLightLutKernel, EnvironmentConstants.DistantSkyLightLutBufferUAV, DistantSkyLightLutBuffer);
                    cmd.SetComputeFloatParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants.DistantSkyLightSampleAltitude, SkyAtmosphereUtils.CVarSkyAtmosphereDistantSkyLightLUTAltitude);
                    cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_DisatantSkyLightLutKernel, 1, 1, 1);
                    cmd.SetGlobalBuffer(EnvironmentConstants.DistantSkyLightLutBufferSRV, DistantSkyLightLutBuffer);
                }

                bool bLightDiskEnabled = renderingData.cameraData.cameraType != CameraType.Reflection;
                float AerialPerspectiveStartDepthInM = SkyAtmosphereUtils.GetValidAerialPerspectiveStartDepthInM(skyAtmosphere, renderingData.cameraData.camera);
                
                // Sky View LUT todo: Cloud part
                {
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_CLOUD_SKYAO", false);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", bSecondAtmosphereLightEnabled);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_OPAQUE_SHADOW", true);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_CLOUD_SHADOW", false);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_SkyViewLutKernel, EnvironmentConstants.TransmittanceLutTexture, TransmittanceLut);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_SkyViewLutKernel, EnvironmentConstants.MultiScatteredLuminanceLutTexture,
                        MultiScatteredLuminanceLut);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_SkyViewLutKernel, EnvironmentConstants.SkyViewLutUAV, SkyAtmosphereViewLutTexture);
                    cmd.SetComputeFloatParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants.SourceDiskEnabled, bLightDiskEnabled ? 1 : 0);
                    int threadGroupX = UniversalUtils.DivRoundUp(SkyAtmosphereViewLutTexture.rt.width, GroupSize);
                    int threadGroupY = UniversalUtils.DivRoundUp(SkyAtmosphereViewLutTexture.rt.height, GroupSize);
                    cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_SkyViewLutKernel, threadGroupX, threadGroupY, 1);
                    cmd.SetGlobalTexture(EnvironmentConstants.SkyViewLutTexture, SkyAtmosphereViewLutTexture);
                }
                
                // Camera Atmosphere Volume
                {
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_CLOUD_SKYAO", false);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", bSecondAtmosphereLightEnabled);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_OPAQUE_SHADOW", false);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SAMPLE_CLOUD_SHADOW", false);
                    CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "SEPARATE_MIE_RAYLEIGH_SCATTERING", bSeparatedAtmosphereMieRayLeigh);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, EnvironmentConstants.TransmittanceLutTexture, TransmittanceLut);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, EnvironmentConstants.MultiScatteredLuminanceLutTexture, MultiScatteredLuminanceLut);
                    cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, EnvironmentConstants.CameraAerialPerspectiveVolumeUAV, SkyAtmosphereCameraAerialPerspectiveVolume);
                    // cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, EnvironmentConstants.CameraAerialPerspectiveVolumeMieOnlyUAV, SkyAtmosphereCameraAerialPerspectiveVolumeMieOnly);
                    // cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, EnvironmentConstants.CameraAerialPerspectiveVolumeRayOnlyUAV, SkyAtmosphereCameraAerialPerspectiveVolumeRayOnly);
                    
                    cmd.SetComputeFloatParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants.AerialPerspectiveStartDepthKm, AerialPerspectiveStartDepthInM * SkyAtmosphereUtils.M_TO_KM);
                    cmd.SetComputeFloatParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants.RealTimeReflection360Mode, 0.0f);
                    
                    int threadGroupX = UniversalUtils.DivRoundUp(SkyAtmosphereCameraAerialPerspectiveVolume.rt.width, 4);
                    int threadGroupY = UniversalUtils.DivRoundUp(SkyAtmosphereCameraAerialPerspectiveVolume.rt.height, 4);
                    int threadGroupZ = UniversalUtils.DivRoundUp(SkyAtmosphereCameraAerialPerspectiveVolume.rt.volumeDepth, 4);
                    
                    cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_CameraAerialPerspectiveVolumeKernel, threadGroupX, threadGroupY, threadGroupZ);
                    cmd.SetGlobalTexture(EnvironmentConstants.CameraAerialPerspectiveVolumeTexture, SkyAtmosphereCameraAerialPerspectiveVolume);
                }
            }
        }
        
        public void Dispose()
        {
            TransmittanceLut?.Release();
            MultiScatteredLuminanceLut?.Release();
            CoreUtils.SafeRelease(UniformSphereSamplesBuffer);
            CoreUtils.SafeRelease(DistantSkyLightLutBuffer);
            SkyAtmosphereViewLutTexture?.Release();
            SkyAtmosphereCameraAerialPerspectiveVolume?.Release();
            SkyAtmosphereCameraAerialPerspectiveVolumeMieOnly?.Release();
            SkyAtmosphereCameraAerialPerspectiveVolumeRayOnly?.Release();
        }
    }
}