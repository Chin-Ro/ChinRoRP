//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Local Volume Fog体素化Pass
//--------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public class FogVolumeVoxelizationPass : ScriptableRenderPass
    {
        class LocalVolumetricFogMaterialVoxelizationPassData
        {
            public Fog fog;
            public int maxSliceCount;
            public Vector3Int viewportSize;

            public List<LocalVolumetricFog> volumetricFogs;
            public RTHandle densityBuffer;
            public Material defaultVolumetricMaterial;
            public List<LocalVolumetricFogEngineData> visibleVolumeData;
            public List<OrientedBBox> visibleVolumeBounds;

            public int computeRenderingParametersKernel;
            public ComputeShader volumetricMaterialCS;
            public ComputeBufferHandle indirectArgumentBuffer;
            public ComputeBuffer visibleVolumeBoundsBuffer;
            public GraphicsBuffer materialDataBuffer;
            public GraphicsBuffer triangleFanIndexBuffer;
            public NativeArray<uint> fogVolumeSortKeys;

            public bool fogOverdrawDebugEnabled;
            public TextureHandle fogOverdrawOutput;
        }

        private LocalVolumetricFogMaterialVoxelizationPassData passData;
        
        public FogVolumeVoxelizationPass(EnvironmentsData data)
        {
            passData = new LocalVolumetricFogMaterialVoxelizationPassData()
            {
                volumetricMaterialCS = data.volumetricMaterialCS
            };
        }
        
        public void Setup(ref RTHandle volumetricDensityBuffer, RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
            passData.densityBuffer = volumetricDensityBuffer;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }
        
        public void Dispose()
        {
            
        }
    }
}