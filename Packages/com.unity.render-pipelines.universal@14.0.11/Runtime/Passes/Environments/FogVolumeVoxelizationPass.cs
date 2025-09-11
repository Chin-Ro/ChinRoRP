//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体素化Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class FogVolumeVoxelizationPass : ScriptableRenderPass
    {
        public FogVolumeVoxelizationPass(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
        }
        
        public void Setup()
        {
            
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }
        
        public void Dispose()
        {
            
        }
    }
}