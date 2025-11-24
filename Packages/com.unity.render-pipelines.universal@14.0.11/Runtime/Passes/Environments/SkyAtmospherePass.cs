namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmospherePass : ScriptableRenderPass
    {
        private Material skyAtmosphereMaterial;
        
        public SkyAtmospherePass(EnvironmentsData data, RenderPassEvent evt)
        {
            renderPassEvent = evt;
            skyAtmosphereMaterial = Load(data.skyAtmosphereShader);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (skyAtmosphereMaterial == null || !SkyAtmosphere.IsSkyAtmosphereEnabled()) return;
            SkyAtmosphere skyAtmosphere = VolumeManager.instance.stack.GetComponent<SkyAtmosphere>();
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere")))
            {
                bool bSecondAtmosphereLightEnabled = renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional;
                // bool bFastSky = SkyAtmosphereUtils.CVarSkyAtmosphereFastSkyLUT > 0;
                // bool bFastAerialPerspective = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveApplyOnOpaque > 0;
                // bool bRenderSkyPixel = true;

                float AerialPerspectiveStartDepthInM =
                    SkyAtmosphereUtils.GetValidAerialPerspectiveStartDepthInM(skyAtmosphere, renderingData.cameraData.camera);
                cmd.SetGlobalFloat(EnvironmentConstants.AerialPerspectiveStartDepthKm, AerialPerspectiveStartDepthInM * SkyAtmosphereUtils.M_TO_KM);
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
        
        public void Dispose()
        {
            if (RenderSettings.skybox == skyAtmosphereMaterial)
            {
                RenderSettings.skybox = null;
            }

            if (skyAtmosphereMaterial)
            {
                CoreUtils.Destroy(skyAtmosphereMaterial);
            }
        }
        
        private Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. SkyAtmosphere render passes will not execute. Check for missing reference in the renderer resources.");
                return null;
            }

            return !shader.isSupported ? null : CoreUtils.CreateEngineMaterial(shader);
        }
    }
}