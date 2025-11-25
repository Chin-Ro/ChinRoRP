//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  阴影渲染特性：ContactShadows, MicroShadows
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature("Shadowing")]
    public class ShadowingRendererFeature : ScriptableRendererFeature
    {
        public ShadowingData shadowingData;
        public bool contactShadows = true;
        [Range(4, 64)] public int sampleCount = 16;
        public bool microShadows = true;
        
        private ContactShadowsPass m_ContactShadowsPass;
        
        public override void Create()
        {
            //m_ContactShadowsPass = new ContactShadowsPass(shadowingData);
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool contactShadowsEnabled = ContactShadows.IsContactShadowsEnabled();
            if (contactShadowsEnabled)
            {
                //renderer.EnqueuePass(m_ContactShadowsPass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            bool isRendererDeferred = renderer is UniversalRenderer { renderingModeRequested: RenderingMode.Deferred };
            RenderPassEvent shadowPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;
            if (isRendererDeferred) shadowPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            
            bool contactShadowsEnabled = ContactShadows.IsContactShadowsEnabled();

            if (contactShadowsEnabled)
            {
                //m_ContactShadowsPass.Setup(shadowPassEvent, sampleCount);
            }
        }

        protected override void Dispose(bool disposing)
        {
            //m_ContactShadowsPass.Dispose();
        }
    }
    
    internal static class ShadowingConstants
    {
        public static readonly int _ContactShadowTexture = Shader.PropertyToID("_ContactShadowTexture");
        public static readonly int _ContactShadowTextureUAV = Shader.PropertyToID("_ContactShadowTextureUAV");
        public static readonly int _ContactShadowParamsParameters = Shader.PropertyToID("_ContactShadowParamsParameters");
        public static readonly int _ContactShadowParamsParameters2 = Shader.PropertyToID("_ContactShadowParamsParameters2");
        public static readonly int _ContactShadowParamsParameters3 = Shader.PropertyToID("_ContactShadowParamsParameters3");
        public static readonly int _ContactShadowParamsParameters4 = Shader.PropertyToID("_ContactShadowParamsParameters4");
    }
}