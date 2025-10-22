namespace UnityEngine.Rendering.Universal
{
    public class SkyAtmosphereLookUpTablesPass : ScriptableRenderPass
    {
        private bool m_HighQualityMultiScattering;
        private bool m_SecondAtmosphereLightEnabled;
        private bool m_SeparatedAtmosphereMieRayLeigh;
        
        public SkyAtmosphereLookUpTablesPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
        }
        
        public void Setup(bool highQualityMultiScattering, bool secondAtmosphereLightEnabled)
        {
            m_HighQualityMultiScattering = highQualityMultiScattering;
            m_SecondAtmosphereLightEnabled = secondAtmosphereLightEnabled;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("SkyAtmosphere LookUpTables")))
            {
                
            }
        }
        
        public void Dispose()
        {
            
        }
    }
}