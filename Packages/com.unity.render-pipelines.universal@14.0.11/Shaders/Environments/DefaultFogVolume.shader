Shader "Hidden/DefaultLocalVolumeFog"
{
    Properties
    {
        [HideInInspector]_EmissionColor("Color", Color) = (1, 1, 1, 1)
        [HideInInspector]_RenderQueueType("Float", Float) = 1
        [HideInInspector][ToggleUI]_AddPrecomputedVelocity("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_ConservativeDepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentWritingMotionVec("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_AlphaCutoffEnable("Boolean", Float) = 0
        [HideInInspector]_TransparentSortPriority("_TransparentSortPriority", Float) = 0
        [HideInInspector][ToggleUI]_UseShadowThreshold("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DoubleSidedEnable("Boolean", Float) = 0
        [HideInInspector][Enum(Flip, 0, Mirror, 1, None, 2)]_DoubleSidedNormalMode("Float", Float) = 2
        [HideInInspector]_DoubleSidedConstants("Vector4", Vector) = (1, 1, -1, 0)
        [HideInInspector][Enum(Auto, 0, On, 1, Off, 2)]_DoubleSidedGIMode("Float", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPrepassEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPostpassEnable("Boolean", Float) = 0
        [HideInInspector]_SurfaceType("Float", Float) = 0
        [HideInInspector]_BlendMode("Float", Float) = 0
        [HideInInspector]_SrcBlend("Float", Float) = 1
        [HideInInspector]_DstBlend("Float", Float) = 0
        [HideInInspector]_AlphaSrcBlend("Float", Float) = 1
        [HideInInspector]_AlphaDstBlend("Float", Float) = 0
        [HideInInspector][ToggleUI]_ZWrite("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_TransparentZWrite("Boolean", Float) = 0
        [HideInInspector]_CullMode("Float", Float) = 2
        [HideInInspector][ToggleUI]_EnableFogOnTransparent("Boolean", Float) = 1
        [HideInInspector]_CullModeForward("Float", Float) = 2
        [HideInInspector][Enum(Front, 1, Back, 2)]_TransparentCullMode("Float", Float) = 2
        [HideInInspector][Enum(UnityEngine.Rendering.CullMode)]_OpaqueCullMode("Float", Float) = 2
        [HideInInspector]_ZTestDepthEqualForOpaque("Float", Int) = 3
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_ZTestTransparent("Float", Float) = 4
        [HideInInspector][ToggleUI]_TransparentBackfaceEnable("Boolean", Float) = 0
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="FogVolumeShader"
            "Queue"="Geometry+0"
            "DisableBatching"="False"
        }

        Pass
        {
            Name "FogVolumeVoxelize"
            Tags
            {
                "LightMode" = "FogVolumeVoxelize"
            }
            
            // Render State
            Cull Off
            Blend One Zero, One Zero
            BlendOp Add, Add
            ZTest Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma multi_compile_instancing

            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _ _DOUBLESIDED_ON
            #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
            #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
            #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/ShaderPassVoxelize.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FogVolumeVoxelize"
            Tags
            {
                "LightMode" = "FogVolumeVoxelize"
            }
            
            // Render State
            Cull Off
            Blend One One, One One
            BlendOp Add, Add
            ZTest Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma multi_compile_instancing

            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _ _DOUBLESIDED_ON
            #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
            #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
            #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/ShaderPassVoxelize.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FogVolumeVoxelize"
            Tags
            {
                "LightMode" = "FogVolumeVoxelize"
            }
            
            // Render State
            Cull Off
            Blend DstColor Zero, DstAlpha Zero
            BlendOp Add, Add
            ZTest Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma multi_compile_instancing

            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _ _DOUBLESIDED_ON
            #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
            #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
            #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/ShaderPassVoxelize.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FogVolumeVoxelize"
            Tags
            {
                "LightMode" = "FogVolumeVoxelize"
            }
            
            // Render State
            Cull Off
            Blend One One, One One
            BlendOp Min, Min
            ZTest Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma multi_compile_instancing

            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _ _DOUBLESIDED_ON
            #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
            #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
            #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/ShaderPassVoxelize.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FogVolumeVoxelize"
            Tags
            {
                "LightMode" = "FogVolumeVoxelize"
            }
            
            // Render State
            Cull Off
            Blend One One, One One
            BlendOp Max, Max
            ZTest Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma multi_compile_instancing

            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _ _DOUBLESIDED_ON
            #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
            #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
            #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/ShaderPassVoxelize.hlsl"
            ENDHLSL
        }
    }
}
