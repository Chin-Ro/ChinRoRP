Shader "ChinRo RenderPipeline/Unlit"
{
    Properties
    {
        [HDR]_BaseMap ("Texture", 2D) = "white"{}
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 0.0, 1.0)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        //[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _Zwrite ("Z Write", Float) = 1
    }
    
    SubShader
    {
        
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            Zwrite [_Zwrite]
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            //#pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing
            
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags{"LightMode" = "ShadowCaster"}
            
            ColorMask 0
            
            HLSLPROGRAM

            #pragma target 3.5
            //#pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "ChinRoShaderGUI"
}