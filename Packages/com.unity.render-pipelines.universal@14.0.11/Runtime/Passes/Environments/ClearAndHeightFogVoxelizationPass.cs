//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体素化Buffer Pass
//--------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

//using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public class ClearAndHeightFogVoxelizationPass : ScriptableRenderPass
    {
        class HeightFogVoxelizationPassData
        {
            public ComputeShader voxelizationCS;
            public int voxelizationKernel;

            public Vector4 resolution;
            public int viewCount;

            public ShaderVariablesVolumetric volumetricCB;

            public RTHandle densityBuffer;
            public ComputeBuffer volumetricAmbientProbeBuffer;
        }

        private HeightFogVoxelizationPassData passData;
        private VBufferParameters[] vBufferParams;
        
        public ClearAndHeightFogVoxelizationPass(EnvironmentsData data)
        {
            passData = new HeightFogVoxelizationPassData()
            {
                voxelizationCS = data.volumeVoxelizationCS
            };
        }
        
        internal void Setup(RenderPassEvent passEvent, VBufferParameters[] m_VBufferParameters, ref RTHandle volumetricDensityBuffer)
        {
            renderPassEvent = passEvent;
            passData.densityBuffer = volumetricDensityBuffer;
            vBufferParams = m_VBufferParameters;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsVolumetricFogEnabled(renderingData.cameraData)) return;
            
            int frameIndex = EnvironmentsRenderFeature.frameCount;
            var currIdx = (frameIndex + 0) & 1;
            var currParams = vBufferParams[currIdx];

            passData.viewCount = 1;
            
            passData.voxelizationKernel = 0;

            var cvp = currParams.viewportSize;
            passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Clear And Height Fog Voxelization")))
            {
                
            }
        }
        
        public void Dispose()
        {
            
        }
    }
}