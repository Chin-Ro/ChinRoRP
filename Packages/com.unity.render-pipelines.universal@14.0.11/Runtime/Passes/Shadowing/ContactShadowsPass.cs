//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Contact Shadows Render Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class ContactShadowsPass : ScriptableRenderPass
    {

        public void Setup(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }
    }
}