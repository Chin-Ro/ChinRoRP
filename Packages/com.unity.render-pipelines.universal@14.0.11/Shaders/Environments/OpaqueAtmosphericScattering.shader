Shader "Hidden/Environments/OpaqueAtmosphericScattering"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma multi_compile_fragment _ DEBUG_DISPLAY

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/SkyUtils.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/AtmosphericScattering.hlsl"

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }
        
        void AtmosphericScatteringCompute(Varyings input, float3 V, float depth, out float3 color, out float3 opacity)
        {
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            // if (depth == UNITY_RAW_FAR_CLIP_VALUE)
            // {
            //     // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
            //     // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
            //     // And recompute the position on the sphere with the current camera direction.
            //     posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;
            //
            //     // Warning: we do not modify depth values. Use them with care!
            // }

            EvaluateAtmosphericScattering(posInput, V, color, opacity); // Premultiplied alpha
        }
        
        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS = input.positionCS.xy;
            float3 V = GetSkyViewDirWS(positionSS);
            float depth = SampleSceneDepth(input.uv);

            float3 volColor, volOpacity;
            AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);

            return float4(volColor, 1.0 - volOpacity.x);
        }
        
    ENDHLSL

    SubShader
    {
         Tags{ "RenderPipeline" = "UniversalPipeline" }
         // 0: NO MSAA
         Pass
         {
             Name "NoMSAA"
             
             Cull Off    ZWrite Off
             Blend One SrcAlpha, Zero One // Premultiplied alpha for RGB, preserve alpha for the alpha channel
             ZTest Less  // Required for XR occlusion mesh optimization
             
             HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
         }
    }
}