//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  环境渲染资源索引配置
//--------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    public class EnvironmentsData : ScriptableObject
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateEnvironmentsDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<EnvironmentsData>();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;
            }
        }
        [MenuItem("Assets/Create/Rendering/Environments Data", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateAdditionalPostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateEnvironmentsDataAsset>(), "EnvironmentsData.asset", null, null);
        }
#endif

        public ComputeShader skyAtmosphereLookUpTablesCS;
        public ComputeShader generateMaxZCS;
        public ComputeShader volumeVoxelizationCS;
        public ComputeShader volumetricMaterialCS;
        public ComputeShader volumetricLightingCS;
        public ComputeShader volumetricLightingFilteringCS;
        public Shader opaqueAtmosphericScatteringShader;
        public Shader defaultFogVolumeShader;
        public Shader lightShaftsShader;
        public Shader skyAtmosphereShader;
        public Material skyAtmosphereMaterial;
        public Shader skyAtmosphereAerialPerspectiveShader;
    }
}