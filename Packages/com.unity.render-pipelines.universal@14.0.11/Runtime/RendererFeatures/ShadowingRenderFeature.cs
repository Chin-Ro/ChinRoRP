//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  阴影渲染特性：ContactShadows, MicroShadows
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature("Shadowing")]
    public class ShadowingRenderFeature : ScriptableRendererFeature
    {
        public bool contactShadows = true;
        [Range(4, 64)] public int sampleCount = 16;
        public bool microShadows = true;
        
        private ContactShadowsPass m_ContactShadowsPass;
        
        public override void Create()
        {
            m_ContactShadowsPass = new ContactShadowsPass();
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ContactShadowsPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            m_ContactShadowsPass.Setup(RenderPassEvent.AfterRenderingGbuffer);
        }

        protected override void Dispose(bool disposing)
        {
            
        }
    }
}