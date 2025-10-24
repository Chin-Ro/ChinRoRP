
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmosphereLookUpTablesPass : ScriptableRenderPass
    {
        private FSkyAtmosphereInternalCommonParameters InternalCommonParameters = new FSkyAtmosphereInternalCommonParameters();

        private RTHandle TransmittanceLut;
        private RTHandle MultiScatteredLuminanceLut;
        
        private RenderTextureDescriptor descriptor = new RenderTextureDescriptor();

        private ComputeShader m_SkyAtmosphereLookUpTablesCS;
        private int m_TransmittanceLutKernel;
        private int m_RenderMultiScatteredLuminanceLutKernel;
        
        public SkyAtmosphereLookUpTablesPass(EnvironmentsData data)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
            m_SkyAtmosphereLookUpTablesCS = data.skyAtmosphereLookUpTablesCS;
        }
        
        public void Setup()
        {
            m_TransmittanceLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderTransmittanceLutCS");
            m_RenderMultiScatteredLuminanceLutKernel = m_SkyAtmosphereLookUpTablesCS.FindKernel("RenderMultiScatteredLuminanceLutCS");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Transmittance LUT
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
                int threadGroupX = UniversalUtils.DivRoundUp(TransmittanceLut.rt.width, 8);
                int threadGroupY = UniversalUtils.DivRoundUp(TransmittanceLut.rt.height, 8);
                cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, EnvironmentConstants._TransmittanceLutUAV, TransmittanceLut);
                cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, threadGroupX, threadGroupY, 1);
                
                // Multi-Scattering LUT
                CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED", bHighQualityMultiScattering);
                cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_RenderMultiScatteredLuminanceLutKernel, EnvironmentConstants._MultiScatteredLuminanceLutUAV, MultiScatteredLuminanceLut);
            }
        }
        
        public void Dispose()
        {
            TransmittanceLut?.Release();
            MultiScatteredLuminanceLut?.Release();
        }
    }
}