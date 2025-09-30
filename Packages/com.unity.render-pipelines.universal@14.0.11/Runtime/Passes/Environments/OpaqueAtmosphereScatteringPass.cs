//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  不透明大气散射Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class OpaqueAtmosphereScatteringPass : ScriptableRenderPass
    {
        private Material m_Material;
        public OpaqueAtmosphereScatteringPass(RenderPassEvent passEvent, EnvironmentsData data)
        {
            renderPassEvent = passEvent;
            m_Material = Load(data.opaqueAtmosphericScatteringShader); 
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsFogEnabled(renderingData.cameraData))
                return;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Opaque Atmospheric Scattering")))
            {
                CoreUtils.DrawFullScreen(cmd, m_Material);
            }
        }
        
        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
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