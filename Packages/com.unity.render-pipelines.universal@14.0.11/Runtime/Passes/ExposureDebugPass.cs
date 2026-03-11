using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal class ExposureDebugPass : ScriptableRenderPass
    {
        class DebugExposureData
        {
            public DebugDisplaySettingsLighting lightingDebugSettings;
            public CameraData cameraData;
            public Material debugExposureMaterial;

            public Vector4 proceduralMeteringParams1;
            public Vector4 proceduralMeteringParams2;
            public RTHandle colorBuffer;
            public RTHandle debugFullScreenTexture;
            public RTHandle output;
            public RTHandle currentExposure;
            public RTHandle previousExposure;
            public RTHandle debugExposureData;
            public HableCurve customToneMapCurve;
            public int lutSize;
            public ComputeBuffer histogramBuffer;
        }

        private DebugExposureData passData;
        private RTHandle m_DebugFullScreenTexture;
        private RenderTextureDescriptor m_Descriptor;

        private Exposure m_Exposure;
        
        internal ExposureDebugPass(Material mat)
        {
            profilingSampler = new ProfilingSampler(nameof(ExposureDebugPass));
            passData = new DebugExposureData()
            {
                debugExposureMaterial = mat
            };
            renderPassEvent = RenderPassEvent.AfterRendering + 3;
            m_Descriptor = new RenderTextureDescriptor();
        }

        internal void Setup(DebugDisplaySettingsLighting lightingDebugSettings)
        {
            passData.lightingDebugSettings = lightingDebugSettings;
        }
        
        void ComputeProceduralMeteringParams(CameraData camera, out Vector4 proceduralParams1, out Vector4 proceduralParams2)
        {
            Vector2 proceduralCenter = m_Exposure.proceduralCenter.value;

            proceduralCenter.x = Mathf.Clamp01(proceduralCenter.x);
            proceduralCenter.y = Mathf.Clamp01(proceduralCenter.y);

            proceduralCenter.x *= camera.scaledWidth;
            proceduralCenter.y *= camera.scaledHeight;

            proceduralParams1 = new Vector4(proceduralCenter.x, proceduralCenter.y,
                m_Exposure.proceduralRadii.value.x * camera.scaledWidth,
                m_Exposure.proceduralRadii.value.y * camera.scaledHeight);

            proceduralParams2 = new Vector4(1.0f / m_Exposure.proceduralSoftness.value, LightUtils.ConvertEvToLuminance(m_Exposure.maskMinIntensity.value), LightUtils.ConvertEvToLuminance(m_Exposure.maskMaxIntensity.value), 0.0f);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            CameraData cameraData = renderingData.cameraData;
            m_Exposure = VolumeManager.instance.stack.GetComponent<Exposure>();
            using (new ProfilingScope(cmd, new ProfilingSampler("Exposure Debug Pass")))
            {
                ComputeProceduralMeteringParams(cameraData, out passData.proceduralMeteringParams1, out passData.proceduralMeteringParams2);
                passData.cameraData = cameraData;
                passData.colorBuffer = renderingData.cameraData.renderer.cameraColorTargetHandle;
                passData.debugFullScreenTexture = m_DebugFullScreenTexture;

                m_Descriptor.msaaSamples = 1;
                m_Descriptor.width = cameraData.cameraTargetDescriptor.width;
                m_Descriptor.height = cameraData.cameraTargetDescriptor.height;
                m_Descriptor.dimension = TextureDimension.Tex2D;
                m_Descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                
                RenderingUtils.ReAllocateIfNeeded(ref passData.output, m_Descriptor, name: "ExposureDebug");
            }
        }

        internal void Dispose()
        {
            
        }
    }
}