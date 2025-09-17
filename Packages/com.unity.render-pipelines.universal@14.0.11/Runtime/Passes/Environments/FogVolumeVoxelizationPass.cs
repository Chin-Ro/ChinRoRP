//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Local Volume Fog体素化Pass
//--------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Unity.Collections;

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
            public ComputeBuffer visibleVolumeBoundsBuffer;
            public GraphicsBuffer materialDataBuffer;
            public GraphicsBuffer triangleFanIndexBuffer;
            public NativeArray<uint> fogVolumeSortKeys;
        }

        private LocalVolumetricFogMaterialVoxelizationPassData passData;
        private VBufferParameters[] vBufferParams;
        private int maxLocalVolumetricFogOnScreen;
        
        const int k_VolumetricMaterialIndirectArgumentCount = 5;
        const int k_VolumetricMaterialIndirectArgumentByteSize = k_VolumetricMaterialIndirectArgumentCount * sizeof(uint);
        
        ComputeBuffer indirectArgumentBuffer;

        private MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        
        public FogVolumeVoxelizationPass(EnvironmentsData data)
        {
            passData = new LocalVolumetricFogMaterialVoxelizationPassData()
            {
                volumetricMaterialCS = data.volumetricMaterialCS
            };
        }
        
        internal void Setup(ref RTHandle volumetricDensityBuffer, RenderPassEvent passEvent, in List<LocalVolumetricFog> visibleLocalVolumetricFogs, 
            int m_MaxLocalVolumetricFogOnScreen, VBufferParameters[] m_VBufferParameters, ComputeBuffer visibleVolumeBoundsBuffer,
            GraphicsBuffer m_VolumetricMaterialDataBuffer, Material m_DefaultVolumetricFogMaterial, List<LocalVolumetricFogEngineData> m_VisibleVolumeData, 
            List<OrientedBBox> m_VisibleVolumeBounds, GraphicsBuffer m_VolumetricMaterialIndexBuffer, NativeArray<uint> m_VolumetricFogSortKeys)
        {
            renderPassEvent = passEvent;
            passData.densityBuffer = volumetricDensityBuffer;
            passData.volumetricFogs = visibleLocalVolumetricFogs;
            maxLocalVolumetricFogOnScreen = m_MaxLocalVolumetricFogOnScreen;
            vBufferParams = m_VBufferParameters;
            passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
            passData.materialDataBuffer = m_VolumetricMaterialDataBuffer;
            passData.defaultVolumetricMaterial = m_DefaultVolumetricFogMaterial;
            passData.visibleVolumeData = m_VisibleVolumeData;
            passData.visibleVolumeBounds = m_VisibleVolumeBounds;
            passData.triangleFanIndexBuffer = m_VolumetricMaterialIndexBuffer;
            passData.fogVolumeSortKeys = m_VolumetricFogSortKeys;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passData.volumetricFogs.Count == 0) return;
            if (indirectArgumentBuffer == null)
            {
                indirectArgumentBuffer = new ComputeBuffer(k_VolumetricMaterialIndirectArgumentCount * maxLocalVolumetricFogOnScreen, sizeof(uint), ComputeBufferType.IndirectArguments)
                {
                    name = "FogVolumeIndirectArguments"
                };
            }
            
            int frameIndex = EnvironmentsRenderFeature.frameIndex;
            var currIdx = (frameIndex + 0) & 1;
            var currParams = vBufferParams[currIdx];
            
            passData.fog = VolumeManager.instance.stack.GetComponent<Fog>();
            passData.computeRenderingParametersKernel = passData.volumetricMaterialCS.FindKernel("ComputeVolumetricMaterialRenderingParameters");
            passData.viewportSize = currParams.viewportSize;
            
            ComputeVolumetricFogSliceCountAndScreenFraction(passData.fog, out passData.maxSliceCount, out _);

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Fog Volume Voxelization")))
            {
                var data = passData;
                int volumeCount = data.volumetricFogs.Count;
                
                // Compute the indirect arguments to render volumetric materials
                cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, EnvironmentConstants._VolumeBounds, data.visibleVolumeBoundsBuffer);
                cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, EnvironmentConstants._VolumetricIndirectBufferArguments, indirectArgumentBuffer);
                cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, EnvironmentConstants._VolumetricMaterialData, data.materialDataBuffer);
                cmd.SetComputeIntParam(data.volumetricMaterialCS, EnvironmentConstants._VolumeCount, volumeCount);
                cmd.SetComputeIntParam(data.volumetricMaterialCS, EnvironmentConstants._MaxSliceCount, data.maxSliceCount);
                cmd.SetComputeIntParam(data.volumetricMaterialCS, EnvironmentConstants._VolumetricViewCount, 1);
                
                int dispatchXCount = Mathf.Max(1, Mathf.CeilToInt((float)(volumeCount * 1) / 32.0f));
                cmd.DispatchCompute(data.volumetricMaterialCS, data.computeRenderingParametersKernel, dispatchXCount, 1, 1);
                int xrViewArgumentOffset = volumeCount * k_VolumetricMaterialIndirectArgumentByteSize;
                var props = _propertyBlock;
                
                // Setup common properties for all fog volumes
                bool obliqueMatrix = UniversalUtils.IsProjectionMatrixOblique(renderingData.cameraData.camera.projectionMatrix);
                if (obliqueMatrix)
                {
                    // Convert the non oblique projection matrix to its  GPU version
                    var gpuProjNonOblique = GL.GetGPUProjectionMatrix(UniversalUtils.CalculateProjectionMatrix(renderingData.cameraData.camera), true);
                    // Build the non oblique view projection matrix
                    var vpNonOblique = gpuProjNonOblique * renderingData.cameraData.GetViewMatrix();
                    props.SetMatrix(EnvironmentConstants._CameraInverseViewProjection_NO, vpNonOblique.inverse);
                }
                props.SetInt(EnvironmentConstants._IsObliqueProjectionMatrix, obliqueMatrix ? 1 : 0);
                props.SetVector(EnvironmentConstants._CameraRight, renderingData.cameraData.camera.transform.right);
                props.SetBuffer(EnvironmentConstants._VolumetricMaterialData, data.materialDataBuffer);
                
                CoreUtils.SetRenderTarget(cmd, data.densityBuffer);
                cmd.SetViewport(new Rect(0, 0, data.viewportSize.x, data.viewportSize.y));

                for (int i = 0; i < volumeCount; i++)
                {
                    int volumeIndex = EnvironmentsRenderFeature.UnpackFogVolumeIndex(data.fogVolumeSortKeys[i]);

                    if (volumeIndex >= data.volumetricFogs.Count)
                        continue;
                    
                    var volume = data.volumetricFogs[volumeIndex];
                                Material material = volume.parameters.materialMask;

                    // Setup parameters for the default volumetric fog ShaderGraph
                    if (volume.parameters.maskMode == LocalVolumetricFogMaskMode.Texture)
                    {
                        material = data.defaultVolumetricMaterial;
                        bool alphaTexture = false;

                        if (volume.parameters.volumeMask != null)
                        {
                            props.SetTexture(EnvironmentConstants._VolumetricMask, volume.parameters.volumeMask);
                            cmd.EnableShaderKeyword("_ENABLE_VOLUMETRIC_FOG_MASK");
                            if (volume.parameters.volumeMask is Texture3D t3d)
                                alphaTexture = t3d.format == TextureFormat.Alpha8;
                        }
                        else
                        {
                            cmd.DisableShaderKeyword("_ENABLE_VOLUMETRIC_FOG_MASK");
                        }

                        props.SetVector(EnvironmentConstants._VolumetricScrollSpeed, volume.parameters.textureScrollingSpeed);
                        props.SetVector(EnvironmentConstants._VolumetricTiling, volume.parameters.textureTiling);
                        props.SetFloat(EnvironmentConstants._AlphaOnlyTexture, alphaTexture ? 1 : 0);
                    }

                    if (material == null)
                        continue;

                    // We can't setup render state on the command buffer so need to have a different pass for each blend mode
                    int passIndex = FogVolumeAPI.GetPassIndexFromBlendingMode(volume.parameters.blendingMode);
                    
#if UNITY_EDITOR
                    // In the editor after modifying a shader, it's possible that a pass is still compiling when rendering
                    if (!UnityEditor.ShaderUtil.IsPassCompiled(material, passIndex))
                    {
                        if (!UnityEditor.ShaderUtil.ShaderHasError(material.shader))
                            UnityEditor.ShaderUtil.CompilePass(material, passIndex, true);
                        continue;
                    }
#endif
                    
                    // Using a constant buffer updated per draw call allows to greatly increase the performance of the shader
                    // compared to using a structured buffer (structured buffer access are very slow even with unifrom indices).
                    var engineData = data.visibleVolumeData[volumeIndex];
                    var bounds = data.visibleVolumeBounds[volumeIndex];
                    var materialCB = new VolumetricMaterialDataCBuffer();
                    materialCB._VolumetricMaterialObbRight = bounds.right;
                    materialCB._VolumetricMaterialObbUp = bounds.up;
                    materialCB._VolumetricMaterialObbExtents = new Vector3(bounds.extentX, bounds.extentY, bounds.extentZ);
                    materialCB._VolumetricMaterialObbCenter = bounds.center;
                    materialCB._VolumetricMaterialAlbedo = engineData.albedo;

                    materialCB._VolumetricMaterialExtinction = VolumetricLightingUtils.ExtinctionFromMeanFreePath(volume.parameters.meanFreePath);
                    materialCB._VolumetricMaterialRcpPosFaceFade = engineData.rcpPosFaceFade;
                    materialCB._VolumetricMaterialRcpNegFaceFade = engineData.rcpNegFaceFade;
                    materialCB._VolumetricMaterialInvertFade = engineData.invertFade;

                    materialCB._VolumetricMaterialRcpDistFadeLen = engineData.rcpDistFadeLen;
                    materialCB._VolumetricMaterialEndTimesRcpDistFadeLen = engineData.endTimesRcpDistFadeLen;
                    materialCB._VolumetricMaterialFalloffMode = (int)engineData.falloffMode;

                    ConstantBuffer.PushGlobal(cmd, materialCB, EnvironmentConstants._VolumetricMaterialDataCBuffer);
                    
                    // We need to issue a draw call per eye because the number of instances to dispatch can be different per eye
                    for (int viewIndex = 0; viewIndex < 1; viewIndex++)
                    {
                        // Upload the volume index to fetch the volume data from the compute shader
                        props.SetInt(EnvironmentConstants._VolumeMaterialDataIndex, volumeIndex + viewIndex * volumeCount);

                        int viewOffset = xrViewArgumentOffset * viewIndex;
                        props.SetInt(EnvironmentConstants._VolumetricViewIndex, viewIndex);
                        cmd.DrawProceduralIndirect(data.triangleFanIndexBuffer, volume.transform.localToWorldMatrix,
                            material, passIndex, MeshTopology.Triangles, indirectArgumentBuffer,
                            k_VolumetricMaterialIndirectArgumentByteSize * volumeIndex + viewOffset, props
                        );
                    }
                }
            }
        }
        
        static void ComputeVolumetricFogSliceCountAndScreenFraction(Fog fog, out int sliceCount, out float screenFraction)
        {
            if (fog.fogControlMode == Fog.FogControl.Balance)
            {
                // Evaluate the ssFraction and sliceCount based on the control parameters
                float maxScreenSpaceFraction = (1.0f - fog.resolutionDepthRatio.value) * (Fog.maxFogScreenResolutionPercentage - Fog.minFogScreenResolutionPercentage) + Fog.minFogScreenResolutionPercentage;
                screenFraction = Mathf.Lerp(Fog.minFogScreenResolutionPercentage, maxScreenSpaceFraction, fog.volumetricFogBudget.value) * 0.01f;
                float maxSliceCount = Mathf.Max(1.0f, fog.resolutionDepthRatio.value * Fog.maxFogSliceCount);
                sliceCount = (int)Mathf.Lerp(1.0f, maxSliceCount, fog.volumetricFogBudget.value);
            }
            else
            {
                screenFraction = fog.screenResolutionPercentage.value * 0.01f;
                sliceCount = fog.volumeSliceCount.value;
            }
        }
        
        public void Dispose()
        {
            CoreUtils.SafeRelease(indirectArgumentBuffer);
            _propertyBlock = null;
            passData = null;
        }
    }
}