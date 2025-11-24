Shader "Hidden/SkyAtmosphereAerialPerspective"
{
    Properties
    {
        [Toggle(SOURCE_DISK_ENABLED)] SourceDiskEnabled ("Source Disk Enabled", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        
        Pass
        {
        	Name "SkyAtmosphereAerialPerspective"
        	
        	ZWrite Off
        	
        	Blend One SrcAlpha, One Zero // Premultiplied alpha for RGB, preserve alpha for the alpha channel
        	
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SOURCE_DISK_ENABLED
            #pragma multi_compile _ SECOND_ATMOSPHERE_LIGHT_ENABLED
            
            // #define PER_PIXEL_NOISE 1
            #define MULTISCATTERING_APPROX_SAMPLING_ENABLED 1
            #define RENDERSKY_ENABLED 1
            // #define FASTSKY_ENABLED 1
            #define FASTAERIALPERSPECTIVE_ENABLED 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
	        #include "Packages/com.unity.render-pipelines.universal/Shaders/Environments/SkyAtmosphereLookUpTables.compute"

            struct Attributes
	        {
	            uint vertexID : SV_VertexID;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
	        };
	        
	        struct Varyings
	        {
	            float4 positionCS : SV_POSITION;
	            float4 vertex : TEXCOORD0;
	            UNITY_VERTEX_OUTPUT_STEREO
	        };

            // struct Attributes
            // {
            //     float4 vertex : POSITION;
            //     UNITY_VERTEX_INPUT_INSTANCE_ID
            // };

            

            Varyings Vert(Attributes input)
		    {
		        Varyings output;
		        UNITY_SETUP_INSTANCE_ID(input);
		        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
		        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
		        //output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
		        return output;
		    }

            // Varyings Vert(Attributes input)
            // {
            //     Varyings output;
            //     UNITY_SETUP_INSTANCE_ID(input);
            //     UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            //     output.positionCS = TransformObjectToHClip(input.vertex.xyz);
            //     output.vertex = input.vertex;
            //     return output;
            // }
            
            float4 Frag(Varyings input) : SV_Target
            {
                float4 OutLuminance = float4(0,0,0,1);
            	
            	float3 WorldDir = SafeNormalize(GetSkyViewDirWS(input.positionCS.xy));
                //float3 WorldDir = SafeNormalize(SafeNormalize(input.vertex.xyz) * 1000000.0f - GetCurrentViewPosition());
            	
            	float3 WorldPos = GetTranslatedCameraPlanetPos();
            	
            	float2 normalizedScreenUV = input.positionCS.xy * _ScreenSize.zw;

                float3 PreExposedL = 0;
            	float3 LuminanceScale = 1.0f;

            #if SAMPLE_ATMOSPHERE_ON_CLOUDS
				// We could read cloud color and skip if transmittance<0.999. Could do that if it would be a compute shader.

				const float4 CloudLuminanceTransmittance = InputCloudLuminanceTransmittanceTexture.Load(int3(PixPos, 0));
				const float CloudCoverage = 1.0f - CloudLuminanceTransmittance.a;
				if (CloudLuminanceTransmittance.a > 0.999)
				{
					OutLuminance = float4(0.0f, 0.0f, 0.0f, 1.0f);
					// return;
				}

				const float CloudDepthKm = VolumetricCloudDepthTexture.Load(int3(PixPos, 0)).r;
				float DeviceZ = CloudDepthKm; // Warning: for simplicity, we use DeviceZ as world distance in kilometer when SAMPLE_ATMOSPHERE_ON_CLOUDS. See special case in IntegrateSingleScatteredLuminance.

			#else // SAMPLE_ATMOSPHERE_ON_CLOUDS

			#if  MSAA_SAMPLE_COUNT > 1
					float DeviceZ = DepthReadDisabled ? FarDepthValue : MSAADepthTexture.Load(int2(PixPos), SampleIndex).x;
			#else
					//float DeviceZ = DepthReadDisabled ? FarDepthValue : SampleSceneDepth(normalizedScreenUV);
					float DeviceZ = SampleSceneDepth(normalizedScreenUV);
			#endif

				if (DeviceZ == FarDepthValue)
				{
					return OutLuminance;
					// Get the light disk luminance to draw 
					LuminanceScale = SkyLuminanceFactor;
				#if SOURCE_DISK_ENABLED
					if (SourceDiskEnabled > 0)
					{
						PreExposedL += GetLightDiskLuminance(WorldPos, WorldDir, 0);
					#if SECOND_ATMOSPHERE_LIGHT_ENABLED
						PreExposedL += GetLightDiskLuminance(WorldPos, WorldDir, 1);
					#endif
					}
				#endif

				#if RENDERSKY_ENABLED==0
					// We should not render the sky and the current pixels are at far depth, so simply early exit.
					// We enable depth bound when supported to not have to even process those pixels.
					OutLuminance = PrepareOutput(float3(0.0f, 0.0f, 0.0f), float3(1.0f, 1.0f, 1.0f));

					//Now the sky pass can ignore the pixel with depth == far but it will need to alpha clip because not all RHI backend support depthbound tests.
					// And the depthtest is already setup to avoid writing all the pixel closer than to the camera than the start distance (very good optimisation).
					// Since this shader does not write to depth or stencil it should still benefit from EArlyZ even with the clip (See AMD depth-in-depth documentation)
					clip(-1.0f);
					return OutLuminance;
				#endif
				}
				else if (FogShowFlagFactor <= 0.0f)
				{ 
					OutLuminance = PrepareOutput(float3(0.0f, 0.0f, 0.0f), float3(1.0f, 1.0f, 1.0f));
					clip(-1.0f);
					return OutLuminance;
				}
			#endif // SAMPLE_ATMOSPHERE_ON_CLOUDS

            	// float ViewHeight = length(WorldPos);
     //        #if FASTSKY_ENABLED && RENDERSKY_ENABLED
     //        	if (ViewHeight < (TopRadiusKm * PLANET_RADIUS_RATIO_SAFE_EDGE) && DeviceZ == FarDepthValue)
     //        	{
     //        		float2 UV;
     //        		
     //        		// The referencial used to build the Sky View lut
					// float3x3 LocalReferencial = GetSkyViewLutReferential(SkyViewLutReferential);
     //        		// Input vectors expressed in this referencial: Up is always Z. Also note that ViewHeight is unchanged in this referencial.
					// float3 WorldPosLocal = float3(0.0, 0.0, ViewHeight);
					// float3 UpVectorLocal = float3(0.0, 0.0, 1.0);
					// float3 WorldDirLocal = mul(LocalReferencial, WorldDir);
					// float ViewZenithCosAngle = dot(WorldDirLocal, UpVectorLocal);
     //
     //        		// Now evaluate inputs in the referential
					// bool IntersectGround = RaySphereIntersectNearest(WorldPosLocal, WorldDirLocal, float3(0, 0, 0), BottomRadiusKm) >= 0.0f;
     //        		SkyViewLutParamsToUv(IntersectGround, ViewZenithCosAngle, WorldDirLocal, ViewHeight, BottomRadiusKm, SkyViewLutSizeAndInvSize, UV);
     //        		float4 SkyLuminanceTransmittance = SkyViewLutTexture.SampleLevel(sampler_LinearClamp, UV, 0);
					// float3 SkyLuminance = SkyLuminanceTransmittance.rgb;
     //
     //        		float3 SkyGreyTransmittance = 1.0f;
					// UNITY_FLATTEN
					// if(bPropagateAlphaNonReflection > 0)
					// {
					// 	SkyGreyTransmittance = SkyLuminanceTransmittance.aaa;
					// }
     //
					// PreExposedL += SkyLuminance * LuminanceScale * (ViewOneOverPreExposure * OutputPreExposure);
     //
					// OutLuminance = PrepareOutput(PreExposedL, SkyGreyTransmittance);
					// UpdateVisibleSkyAlpha(DeviceZ, OutLuminance);
					// return 0;
     //        	}
     //        #endif

            #if FASTAERIALPERSPECTIVE_ENABLED
            	#if COLORED_TRANSMITTANCE_ENABLED
				#error The FASTAERIALPERSPECTIVE_ENABLED path does not support COLORED_TRANSMITTANCE_ENABLED.
				#else

            		float2 ndc = (input.positionCS.xy) * _ScreenSize.zw;
					float3 DepthBufferTranslatedWorldPos = ComputeWorldSpacePosition(ndc, DeviceZ, UNITY_MATRIX_I_VP).xyz;
					float4 NDCPosition = mul(DepthBufferTranslatedWorldPos, UNITY_MATRIX_VP);

            		PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, DeviceZ, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    
					const float NearFadeOutRangeInvDepthKm = 1.0 / 0.001f; // 1 meter fade region
					float4 AP = GetAerialPerspectiveLuminanceTransmittance(
						false, float4(0,0,1,1),
						posInput.positionNDC, (DepthBufferTranslatedWorldPos - GetCurrentViewPosition()) * M_TO_SKY_UNIT,
						CameraAerialPerspectiveVolumeTexture, sampler_LinearClamp,
						CameraAerialPerspectiveVolumeDepthResolutionInv,
						CameraAerialPerspectiveVolumeDepthResolution,
						AerialPerspectiveStartDepthKm,
						CameraAerialPerspectiveVolumeDepthSliceLengthKm,
						CameraAerialPerspectiveVolumeDepthSliceLengthKmInv,
						ViewOneOverPreExposure * OutputPreExposure,
						NearFadeOutRangeInvDepthKm);
    
					PreExposedL += AP.rgb * LuminanceScale;
					float Transmittance = AP.a;
    
					OutLuminance = PrepareOutput(PreExposedL, float3(Transmittance, Transmittance, Transmittance));
					UpdateVisibleSkyAlpha(DeviceZ, OutLuminance);
            	
					return OutLuminance;
				#endif
            #else
            	
            	// Move to top atmosphere as the starting point for ray marching.
				// This is critical to be after the above to not disrupt above atmosphere tests and voxel selection.
				if (!MoveToTopAtmosphere(WorldPos, WorldDir, TopRadiusKm))
				{
					// Ray is not intersecting the atmosphere
					OutLuminance = PrepareOutput(PreExposedL);
					return OutLuminance;
				}
    
            	// Apply the start depth offset after moving to the top of atmosphere for consistency (and to avoid wrong out-of-atmosphere test resulting in black pixels).
				WorldPos += WorldDir * AerialPerspectiveStartDepthKm;
    
				SamplingSetup Sampling = (SamplingSetup)0;
				{
					Sampling.VariableSampleCount = true;
					Sampling.MinSampleCount = SampleCountMin;
					Sampling.MaxSampleCount = SampleCountMax;
					Sampling.DistanceToSampleCountMaxInv = DistanceToSampleCountMaxInv;
				}
				const bool Ground = false;
				const bool MieRayPhase = true;
				SingleScatteringResult ss = IntegrateSingleScatteredLuminance(
					input.positionCS, WorldPos, WorldDir,
					Ground, Sampling, DeviceZ, MieRayPhase,
					_MainLightPosition.xyz, _AdditionalLightsPosition[0].xyz, 
					AtmosphereLightColor * SkyAndAerialPerspectiveLuminanceFactor, 
					SecondAtmosphereLightColor.rgb * SkyAndAerialPerspectiveLuminanceFactor,
					AerialPespectiveViewDistanceScale);
    
				PreExposedL += ss.L * LuminanceScale;
			 
				// if (View.RenderingReflectionCaptureMask == 0.0f && !IsSkyAtmosphereRenderedInMain(View.EnvironmentComponentsFlags))
				// {
				// 	PreExposedL = 0.0f;
				// }
    
            	#if SAMPLE_ATMOSPHERE_ON_CLOUDS
				// We use gray scale transmittance to match the rendering when applying the AerialPerspective texture
				const float GreyScaleAtmosphereTransmittance = dot(ss.Transmittance, float3(1.0 / 3.0f, 1.0 / 3.0f, 1.0 / 3.0f));
				// Reduce cloud luminance according to the atmosphere transmittance and add the atmosphere in scattred luminance according to the cloud coverage.
				PreExposedL = CloudLuminanceTransmittance.rgb * GreyScaleAtmosphereTransmittance + CloudCoverage * PreExposedL;
				// Coverage of the cloud layer itself does not change.
				ss.Transmittance = CloudLuminanceTransmittance.a;
				#endif
    
            	OutLuminance = PrepareOutput(PreExposedL, ss.Transmittance);
				UpdateVisibleSkyAlpha(DeviceZ, OutLuminance);
            			
		        return OutLuminance;
            #endif
		    }
		    ENDHLSL
		}
	}
}
