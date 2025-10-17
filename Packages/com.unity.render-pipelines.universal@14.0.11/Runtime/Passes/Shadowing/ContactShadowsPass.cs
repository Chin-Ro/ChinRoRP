//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Contact Shadows Render Pass
//--------------------------------------------------------------------------------------------------------

using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class ContactShadowsPass : ScriptableRenderPass
    {
        private ComputeShader contactShadowsCS;
        private int sampleCount;
        private int kernel;

        private Vector4 params1;
        private Vector4 params2;
        private Vector4 params3;
        private Vector4 params4;
        
        private RTHandle contactShadowsTexture;

        private int cameraFrameCount;
        
        private RenderTextureDescriptor descriptor = new RenderTextureDescriptor();

        public ContactShadowsPass(ShadowingData data)
        {
            contactShadowsCS = data.contactShadowsCS;
        }
        
        public void Setup(RenderPassEvent passEvent, int m_SampleCount)
        {
            renderPassEvent = passEvent;
            sampleCount = m_SampleCount;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (contactShadowsCS == null) return;
            kernel = contactShadowsCS.FindKernel("DeferredContactShadow");
            contactShadowsCS.shaderKeywords = null;

            var m_ContactShadows = VolumeManager.instance.stack.GetComponent<ContactShadows>();
            float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance.value, 0.0f, m_ContactShadows.maxDistance.value);
            float contactShadowFadeEnd = m_ContactShadows.maxDistance.value;
            float contactShadowOneOverFadeRange = 1.0f / Math.Max(1e-6f, contactShadowRange);
            
            float contactShadowMinDist = Mathf.Min(m_ContactShadows.minDistance.value, contactShadowFadeEnd);
            float contactShadowFadeIn = Mathf.Clamp(m_ContactShadows.fadeInDistance.value, 1e-6f, contactShadowFadeEnd);

            int deferredShadowTileSize = 8; // Must match ContactShadows.compute
            int numTilesX = (renderingData.cameraData.scaledWidth + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
            int numTilesY = (renderingData.cameraData.scaledHeight + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
            
            params1 = new Vector4(m_ContactShadows.length.value, m_ContactShadows.distanceScaleFactor.value, contactShadowFadeEnd, contactShadowOneOverFadeRange);
            params2 = new Vector4(0, contactShadowMinDist, contactShadowFadeIn, m_ContactShadows.rayBias.value * 0.01f);
            params3 = new Vector4(sampleCount, m_ContactShadows.thicknessScale.value * 10.0f, 0.0f, 0.0f);
            
            int taaEnabled = renderingData.cameraData.IsTemporalAAEnabled() ? 1 : 0;
            if (taaEnabled == 1) cameraFrameCount++; 
            params4 = new Vector4(taaEnabled, cameraFrameCount, 0, 0);
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Contact Shadows")))
            {
                descriptor.dimension = TextureDimension.Tex2D;
                descriptor.width = renderingData.cameraData.scaledWidth;
                descriptor.height = renderingData.cameraData.scaledHeight;
                descriptor.graphicsFormat = GraphicsFormat.R32_UInt;
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref contactShadowsTexture, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "ContactShadowsBuffer");
                
                cmd.SetComputeVectorParam(contactShadowsCS, ShadowingConstants._ContactShadowParamsParameters, params1);
                cmd.SetComputeVectorParam(contactShadowsCS, ShadowingConstants._ContactShadowParamsParameters2, params2);
                cmd.SetComputeVectorParam(contactShadowsCS, ShadowingConstants._ContactShadowParamsParameters3, params3);
                cmd.SetComputeVectorParam(contactShadowsCS, ShadowingConstants._ContactShadowParamsParameters4, params4);
                
                cmd.SetComputeTextureParam(contactShadowsCS, kernel, ShadowingConstants._ContactShadowTextureUAV, contactShadowsTexture);
                cmd.DispatchCompute(contactShadowsCS, kernel, numTilesX, numTilesY, 1);
                
                cmd.SetGlobalTexture(ShadowingConstants._ContactShadowTexture, contactShadowsTexture);
            }
        }

        public void Dispose()
        {
            contactShadowsTexture?.Release();
        }
    }
}