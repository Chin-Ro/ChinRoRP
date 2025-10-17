//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Max Z Mask生成Pass
//--------------------------------------------------------------------------------------------------------

using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class GenerateMaxZPass : ScriptableRenderPass
    {
        class GenerateMaxZMaskPassData
        {
            public RTHandle maxZ8xBuffer;
            public RTHandle maxZBuffer;
            public RTHandle dilatedMaxZBuffer;
            
            public ComputeShader generateMaxZCS;
            public int maxZKernel;
            public int maxZDownsampleKernel;
            public int dilateMaxZKernel;
            
            public Vector2Int intermediateMaskSize;
            public Vector2Int finalMaskSize;
            public float dilationWidth;
            public int viewCount;
        }
        
        private GenerateMaxZMaskPassData passData;
        private VBufferParameters[] vBufferParameters;
        
        public GenerateMaxZPass(EnvironmentsData data)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            passData = new GenerateMaxZMaskPassData
            {
                generateMaxZCS = data.generateMaxZCS,
            };
        }
        
        internal void Setup(ref RTHandle maxZMask, RenderPassEvent passEvent, bool isRendererDeferred, in VBufferParameters[] m_vBufferParameters)
        {
            renderPassEvent = passEvent;
            if (!isRendererDeferred) ConfigureInput(ScriptableRenderPassInput.Depth);
            vBufferParameters = m_vBufferParameters;
            passData.dilatedMaxZBuffer = maxZMask;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsVolumetricFogEnabled(renderingData.cameraData)) return;

            CameraData cameraData = renderingData.cameraData;
            passData.generateMaxZCS.shaderKeywords = null;
            bool planarReflection = cameraData.cameraType == CameraType.Reflection;
            CoreUtils.SetKeyword(passData.generateMaxZCS, "PLANAR_OBLIQUE_DEPTH", planarReflection);

            passData.maxZKernel = passData.generateMaxZCS.FindKernel("ComputeMaxZ");
            passData.maxZDownsampleKernel = passData.generateMaxZCS.FindKernel("ComputeFinalMask");
            passData.dilateMaxZKernel = passData.generateMaxZCS.FindKernel("DilateMask");
            
            passData.intermediateMaskSize.x = UniversalUtils.DivRoundUp(cameraData.scaledWidth, 8);
            passData.intermediateMaskSize.y = UniversalUtils.DivRoundUp(cameraData.scaledHeight, 8);

            passData.finalMaskSize.x = passData.intermediateMaskSize.x / 2;
            passData.finalMaskSize.y = passData.intermediateMaskSize.y / 2;

            int frameIndex = EnvironmentsRendererFeature.frameIndex;
            var currIdx = frameIndex & 1;
            
            if (vBufferParameters != null)
            {
                var currentParams = vBufferParameters[currIdx];
                float ratio = (float)currentParams.viewportSize.x / (float)cameraData.scaledWidth;
                passData.dilationWidth = ratio < 0.1f ? 2f:
                    ratio < 0.5f ? 1f : 0f;
            }
            else
            {
                passData.dilationWidth = 1f;
            }

            passData.viewCount = 1;

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(passData.intermediateMaskSize.x, passData.intermediateMaskSize.y)
            {
                graphicsFormat = GraphicsFormat.R32_SFloat,
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                msaaSamples = 1,
                depthBufferBits = 0,
            };
            
            RenderingUtils.ReAllocateIfNeeded(ref passData.maxZ8xBuffer, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "MaxZ 8x mask");
            RenderingUtils.ReAllocateIfNeeded(ref passData.maxZBuffer, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "MaxZ mask");
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Generate Max Z Mask for Volumetric")))
            {
                var data = passData;
                
                // Downsample 8x8 with max operator

                var cs = data.generateMaxZCS;
                var kernel = data.maxZKernel;

                int maskW = data.intermediateMaskSize.x;
                int maskH = data.intermediateMaskSize.y;

                int dispatchX = maskW;
                int dispatchY = maskH;

                cmd.SetComputeTextureParam(cs, kernel, EnvironmentConstants._OutputTexture, data.maxZ8xBuffer);

                cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);

                // --------------------------------------------------------------
                // Downsample to 16x16 and compute gradient if required

                kernel = data.maxZDownsampleKernel;

                cmd.SetComputeTextureParam(cs, kernel, EnvironmentConstants._InputTexture, data.maxZ8xBuffer);
                cmd.SetComputeTextureParam(cs, kernel, EnvironmentConstants._OutputTexture, data.maxZBuffer);

                Vector4 srcLimitAndDepthOffset = new Vector4(maskW, maskH, 0.0f, 0.0f);
                cmd.SetComputeVectorParam(cs, EnvironmentConstants._SrcOffsetAndLimit, srcLimitAndDepthOffset);
                cmd.SetComputeFloatParam(cs, EnvironmentConstants._DilationWidth, data.dilationWidth);

                int finalMaskW = Mathf.CeilToInt(maskW / 2.0f);
                int finalMaskH = Mathf.CeilToInt(maskH / 2.0f);

                dispatchX = UniversalUtils.DivRoundUp(finalMaskW, 8);
                dispatchY = UniversalUtils.DivRoundUp(finalMaskH, 8);

                cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);

                // --------------------------------------------------------------
                // Dilate max Z and gradient.
                kernel = data.dilateMaxZKernel;

                cmd.SetComputeTextureParam(cs, kernel, EnvironmentConstants._InputTexture, data.maxZBuffer);
                cmd.SetComputeTextureParam(cs, kernel, EnvironmentConstants._OutputTexture, data.dilatedMaxZBuffer);

                srcLimitAndDepthOffset.x = finalMaskW;
                srcLimitAndDepthOffset.y = finalMaskH;
                cmd.SetComputeVectorParam(cs, EnvironmentConstants._SrcOffsetAndLimit, srcLimitAndDepthOffset);

                cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);
            }
        }

        public void Dispose()
        {
            passData.maxZ8xBuffer?.Release();
            passData.maxZBuffer?.Release();
            passData = null;
            vBufferParameters = null;
        }
    }
}