//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Local Volume Fog体素化Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class FogVolumeVoxelizationPass : ScriptableRenderPass
    {
        public FogVolumeVoxelizationPass()
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