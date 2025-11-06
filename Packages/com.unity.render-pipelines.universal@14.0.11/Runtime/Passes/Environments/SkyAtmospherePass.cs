namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmospherePass : ScriptableRenderPass
    {
        private Material skyAtmosphereMaterial;
        private MaterialPropertyBlock _materialPropertyBlock = new MaterialPropertyBlock();
        private FSkyAtmosphereRenderContext SkyRC = new FSkyAtmosphereRenderContext();
        
        public SkyAtmospherePass(EnvironmentsData data)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
            skyAtmosphereMaterial = Load(data.skyAtmosphereShader);
        }
        
        public void Setup()
        {
            
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (skyAtmosphereMaterial == null || SkyAtmosphere.IsSkyAtmosphereEnabled()) return;
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere")))
            {
                bool bSecondAtmosphereLightEnabled = renderingData.lightData is { additionalLightsCount: > 0 } && renderingData.lightData.visibleLights[1].lightType == LightType.Directional;
                
                SkyRC.bFastSky = SkyAtmosphereUtils.CVarSkyAtmosphereFastSkyLUT > 0;
                SkyRC.bFastAerialPerspective = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveApplyOnOpaque > 0;
                SkyRC.bFastAerialPerspectiveDepthTest = SkyAtmosphereUtils.CVarSkyAtmosphereAerialPerspectiveDepthTest > 0;
                SkyRC.bSecondAtmosphereLightEnabled = bSecondAtmosphereLightEnabled;
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

        struct FSkyAtmosphereRenderContext
        {
            public bool bFastSky;
            public bool bFastAerialPerspective;
            public bool bFastAerialPerspectiveDepthTest;
            public bool bSecondAtmosphereLightEnabled;
        }
    }
}