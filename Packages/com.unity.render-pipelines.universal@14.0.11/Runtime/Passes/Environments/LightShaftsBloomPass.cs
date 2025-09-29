//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  LightShaftsBloomPass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class LightShaftsBloomPass : ScriptableRenderPass
    {
        private Material m_LightShaftsBloomMaterial;
        private RTHandle m_LightShaftsBloomTexture;
        private RTHandle m_LightShaftsBloomTemp;
        private RenderTextureDescriptor m_LightShaftsBloomDesc;
        private int m_LightShaftBlurNumSamples;
        private float m_LightShaftFirstPassDistance;
        public LightShaftsBloomPass(RenderPassEvent passEvent, EnvironmentsData data)
        {
            renderPassEvent = passEvent;
            m_LightShaftsBloomMaterial = Load(data.lightShaftsShader);
        }
        
        public void Setup(RenderTextureDescriptor descriptor, int lightShaftBlurNumSamples, float lightShaftFirstPassDistance)
        {
            m_LightShaftsBloomDesc = descriptor;
            m_LightShaftBlurNumSamples = lightShaftBlurNumSamples;
            m_LightShaftFirstPassDistance = lightShaftFirstPassDistance;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsLightShaftsBloomEnabled(renderingData.cameraData)) return;
            Fog fog = VolumeManager.instance.stack.GetComponent<Fog>();
            
            var cmd = renderingData.commandBuffer;
            using(new ProfilingScope(cmd, new ProfilingSampler("Light Shafts(Bloom)")))
            {
                cmd.SetGlobalFloat(EnvironmentConstants._BloomMaxBrightness, fog.bloomMaxBrightness.value);
                cmd.SetGlobalVector(EnvironmentConstants._BloomTintAndThreshold,
                    new Vector4(fog.bloomTint.value.r, fog.bloomTint.value.g, fog.bloomTint.value.b, fog.bloomThreshold.value));
                
                RenderingUtils.ReAllocateIfNeeded(ref m_LightShaftsBloomTexture, m_LightShaftsBloomDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_LightShaftsBloomTexture");
                RenderingUtils.ReAllocateIfNeeded(ref m_LightShaftsBloomTemp, m_LightShaftsBloomDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_LightShaftsBloomTemp");
                
                Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_LightShaftsBloomTexture, m_LightShaftsBloomMaterial, 1);

                for (int i = 0; i < 3; i++)
                {
                    var source = i % 2 == 0 ? m_LightShaftsBloomTexture : m_LightShaftsBloomTemp;
                    var target = i % 2 == 0 ? m_LightShaftsBloomTemp : m_LightShaftsBloomTexture;
                    cmd.SetGlobalVector(EnvironmentConstants._RadialBlurParameters,
                        new Vector4(m_LightShaftBlurNumSamples, m_LightShaftFirstPassDistance, i));
                    Blitter.BlitCameraTexture(cmd, source, target, m_LightShaftsBloomMaterial, 2);
                }
                
                // 融合
                Blitter.BlitCameraTexture(cmd, m_LightShaftsBloomTemp, renderingData.cameraData.renderer.cameraColorTargetHandle, m_LightShaftsBloomMaterial, 4);
            }
        }
        
        public void Dispose()
        {
            CoreUtils.Destroy(m_LightShaftsBloomMaterial);
            m_LightShaftsBloomTexture?.Release();
            m_LightShaftsBloomTemp?.Release();
        }
        
        private Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                return null;
            }

            return !shader.isSupported ? null : CoreUtils.CreateEngineMaterial(shader);
        }
    }
}