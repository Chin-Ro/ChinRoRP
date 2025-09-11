//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体素光照计算Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class VolumetricLightingPass : ScriptableRenderPass
    {
        public VolumetricLightingPass()
        {
            
        }
        
        public void Setup(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }
        
        public void Dispose()
        {
            
        }
    }
}