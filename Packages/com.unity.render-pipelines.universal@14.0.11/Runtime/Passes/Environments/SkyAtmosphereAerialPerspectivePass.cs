namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmosphereAerialPerspectivePass : ScriptableRenderPass
    {
        private Material skyAtmosphereAerialPerspectiveMaterial;
        
        public SkyAtmosphereAerialPerspectivePass(EnvironmentsData data, RenderPassEvent evt)
        {
            renderPassEvent = evt;
            skyAtmosphereAerialPerspectiveMaterial = Load(data.skyAtmosphereAerialPerspectiveShader);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (skyAtmosphereAerialPerspectiveMaterial == null || !SkyAtmosphere.IsSkyAtmosphereEnabled()) return;
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere Areial Perspective")))
            {
                bool bSecondAtmosphereLightEnabled = renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional;
                skyAtmosphereAerialPerspectiveMaterial.SetFloat("DepthReadDisabled", 0.0f);
                CoreUtils.SetKeyword(skyAtmosphereAerialPerspectiveMaterial, "SECOND_ATMOSPHERE_LIGHT_ENABLED", bSecondAtmosphereLightEnabled);
                CoreUtils.DrawFullScreen(cmd, skyAtmosphereAerialPerspectiveMaterial);
            }
        }
        
        public void Dispose()
        {
            CoreUtils.Destroy(skyAtmosphereAerialPerspectiveMaterial);
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