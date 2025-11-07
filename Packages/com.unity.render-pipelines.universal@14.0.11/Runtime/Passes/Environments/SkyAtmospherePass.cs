namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmospherePass : ScriptableRenderPass
    {
        private Material skyAtmosphereMaterial;
        
        public SkyAtmospherePass(EnvironmentsData data)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
            skyAtmosphereMaterial = Load(data.skyAtmosphereShader);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (skyAtmosphereMaterial == null || !SkyAtmosphere.IsSkyAtmosphereEnabled()) return;
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere")))
            {
                bool bSecondAtmosphereLightEnabled = renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional;
                // bool bFastSky = SkyAtmosphereUtils.CVarSkyAtmosphereFastSkyLUT > 0;
                // bool bFastAerialPerspective = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveApplyOnOpaque > 0;
                // bool bRenderSkyPixel = true;
                
                skyAtmosphereMaterial.SetFloat("DepthReadDisabled", 0.0f);
                CoreUtils.SetKeyword(skyAtmosphereMaterial, "SECOND_ATMOSPHERE_LIGHT_ENABLED", bSecondAtmosphereLightEnabled);
                if (RenderSettings.skybox != skyAtmosphereMaterial)
                {
                    RenderSettings.skybox = skyAtmosphereMaterial;
                }
                //CoreUtils.SetKeyword(skyAtmosphereMaterial, "FASTSKY_ENABLED", bFastSky);
                //CoreUtils.SetKeyword(skyAtmosphereMaterial, "FASTAERIALPERSPECTIVE_ENABLED", bFastAerialPerspective);
                //CoreUtils.SetKeyword(skyAtmosphereMaterial, "RENDERSKY_ENABLED", bRenderSkyPixel);
            }
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
        
        public void Dispose()
        {
            CoreUtils.Destroy(skyAtmosphereMaterial);
        }
    }
}