
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
        
        private RenderTextureDescriptor descriptor;

        private ComputeShader m_SkyAtmosphereLookUpTablesCS;
        private int m_TransmittanceLutKernel;
        private int m_RenderMultiScatteredLuminanceLutKernel;

        private ComputeBuffer UniformSphereSamplesBuffer = new ComputeBuffer(GroupSize * GroupSize, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        private Vector4[] Dest = new Vector4[GroupSize * GroupSize];
        
        public SkyAtmosphereLookUpTablesPass(EnvironmentsData data)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
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
            }
            UniformSphereSamplesBuffer.SetData(Dest);
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
                int threadGroupX = UniversalUtils.DivRoundUp(TransmittanceLut.rt.width, GroupSize);
                int threadGroupY = UniversalUtils.DivRoundUp(TransmittanceLut.rt.height, GroupSize);
                
                cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, EnvironmentConstants._TransmittanceLutUAV, TransmittanceLut);
                cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_TransmittanceLutKernel, threadGroupX, threadGroupY, 1);
                
                // Multi-Scattering LUT
                CoreUtils.SetKeyword(m_SkyAtmosphereLookUpTablesCS, "HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED", bHighQualityMultiScattering);
                cmd.SetComputeBufferParam(m_SkyAtmosphereLookUpTablesCS, m_RenderMultiScatteredLuminanceLutKernel, EnvironmentConstants._UniformSphereSamplesBuffer, UniformSphereSamplesBuffer);
                cmd.SetComputeIntParam(m_SkyAtmosphereLookUpTablesCS, EnvironmentConstants._UniformSphereSamplesBufferSampleCount, GroupSize);
                cmd.SetComputeTextureParam(m_SkyAtmosphereLookUpTablesCS, m_RenderMultiScatteredLuminanceLutKernel, EnvironmentConstants._MultiScatteredLuminanceLutUAV, MultiScatteredLuminanceLut);
                cmd.SetGlobalTexture(EnvironmentConstants._TransmittanceLutTexture, TransmittanceLut);
                cmd.DispatchCompute(m_SkyAtmosphereLookUpTablesCS, m_RenderMultiScatteredLuminanceLutKernel, threadGroupX, threadGroupY, 1);
                
                // Distant Sky Light LUT
            }
        }
        
        public void Dispose()
        {
            TransmittanceLut?.Release();
            MultiScatteredLuminanceLut?.Release();
            CoreUtils.SafeRelease(UniformSphereSamplesBuffer);
        }
    }
}