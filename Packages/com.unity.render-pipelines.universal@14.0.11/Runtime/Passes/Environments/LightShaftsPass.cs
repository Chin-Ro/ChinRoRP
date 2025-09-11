//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  LightShaftsPass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class LightShaftsPass : ScriptableRenderPass
    {
        public LightShaftsPass(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
        }
        
        public void Setup()
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}