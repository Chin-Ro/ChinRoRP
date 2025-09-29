//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  LightShaftsPass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class LightShaftsPass : ScriptableRenderPass
    {
        private RTHandle m_LightShaftsTexture;
        private RTHandle m_LightShaftsTemp;
        private RenderTextureDescriptor m_LightShaftsDesc;
        private Material m_LightShaftsMaterial;
        private int m_LightShaftBlurNumSamples;
        private float m_LightShaftFirstPassDistance;
        public LightShaftsPass(RenderPassEvent passEvent, EnvironmentsData data)
        {
            renderPassEvent = passEvent;
            m_LightShaftsMaterial = Load(data.lightShaftsShader);
        }
        
        public void Setup(RenderTextureDescriptor descriptor, int lightShaftBlurNumSamples, float lightShaftFirstPassDistance)
        {
            m_LightShaftsDesc = descriptor;
            m_LightShaftBlurNumSamples = lightShaftBlurNumSamples;
            m_LightShaftFirstPassDistance = lightShaftFirstPassDistance;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsLightShaftsEnabled(renderingData.cameraData)) return;
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Light Shafts")))
            {
                Camera camera = renderingData.cameraData.camera;
                VisibleLight mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
                Vector3 lightDir = -mainLight.localToWorldMatrix.GetColumn(2);
                Vector3 lightPos = camera.transform.position + lightDir * camera.farClipPlane;
                Vector3 lightPosScreen = camera.WorldToScreenPoint(lightPos);
                
                // 判断方向光是否在相机背面
                float directionalLightInScreen = 1;
                if (lightPosScreen.z < 0)
                {
                    directionalLightInScreen = 0;
                }
                
                RenderingUtils.ReAllocateIfNeeded(ref m_LightShaftsTexture, m_LightShaftsDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LightShafts");
                RenderingUtils.ReAllocateIfNeeded(ref m_LightShaftsTemp, m_LightShaftsDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LightShaftsTemp");
                
                cmd.SetGlobalVector(EnvironmentConstants._DirectionalLightScreenPos, new Vector4(lightPosScreen.x, lightPosScreen.y, 0, 1));
                cmd.SetGlobalVector(EnvironmentConstants._LightShaftParameters, new Vector4(directionalLightInScreen, fog.bloomScale.value, 1.0f / fog.occlusionDepthRange.value, fog.occlusionMaskDarkness.value));
                Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_LightShaftsTexture, m_LightShaftsMaterial, 0);

                for (int i = 0; i < 3; i++)
                {
                    var source = i % 2 == 0 ? m_LightShaftsTexture : m_LightShaftsTemp;
                    var target = i % 2 == 0 ? m_LightShaftsTemp : m_LightShaftsTexture;
                    cmd.SetGlobalVector(EnvironmentConstants._RadialBlurParameters,
                        new Vector4(m_LightShaftBlurNumSamples, m_LightShaftFirstPassDistance, i));
                    Blitter.BlitCameraTexture(cmd, source, target, m_LightShaftsMaterial, 2);
                }
                
                // 融合
                Blitter.BlitCameraTexture(cmd, m_LightShaftsTemp, m_LightShaftsTexture, m_LightShaftsMaterial, 3);
                cmd.SetGlobalTexture(EnvironmentConstants._LightShaftTexture, m_LightShaftsTexture);
            }
        }

        public void Dispose()
        {
            m_LightShaftsTexture?.Release();
            m_LightShaftsTemp?.Release();
            CoreUtils.Destroy(m_LightShaftsMaterial);
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