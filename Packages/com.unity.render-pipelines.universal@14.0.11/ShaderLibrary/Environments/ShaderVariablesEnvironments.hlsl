#ifndef SHADER_VARIABLES_ENVIRONMENTS_HLSL_INCLUDED
#define SHADER_VARIABLES_ENVIRONMENTS_HLSL_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

GLOBAL_CBUFFER_START(ShaderVariablesEnvironments, b8)
// x : FogFirstDensity 
// y : FogFirstHeightFalloff
// z : FogFirstHeight
// w : StartDistance
float4 ExponentialFogParameters;
// x : fogSecondDensity
// y : fogSecondHeightFalloff
// z : fogSecondHeight
// w : fogCutoffDistance
float4 ExponentialFogParameters2;
// xyz : inscatterColor
// w : cosine exponent
float4 DirectionalInscatteringColor;
// xy : 0
// z : EndDistance
// w : direactional inscattering start distance
float4 ExponentialFogParameters3;
// xyz : fog inscattering color
// w : min transparency
float4 ExponentialFogColorParameter;
float4 _MipFogParameters;
float4 _HeightFogBaseScattering;
// x : 1/H
// y : H
// z : heightFogBaseExtinction
// w : heightFogBaseHeight
float4 _HeightFogExponents;
// x : globalFogAnisotropy
// y : globalLightProbeDimmer
// z : extinctionScale
// w : fogEnabled
float4 _GlobalFogParam1;
// x : enableLightShafts
// y : enableVolumetricFog
// z : volumetricFilteringEnabled
// w : fogDirectionalOnly
float4 _GlobalFogParam2;

// VBuffer
float4 _VBufferViewportSize;
float4 _VBufferLightingViewportScale;
float4 _VBufferLightingViewportLimit;
float4 _VBufferDistanceEncodingParams;
float4 _VBufferDistanceDecodingParams;
uint _VBufferSliceCount;
float _VBufferRcpSliceCount;
float _VBufferRcpInstancedViewCount;
float _VBufferLastSliceDist;
CBUFFER_END

#define _HeightFogBaseExtinction _HeightFogExponents.z;
#define _HeightFogBaseHeight _HeightFogExponents.w
#define _GlobalFogAnisotropy _GlobalFogParam1.x;
#define _SkyContributeFactor _GlobalFogParam1.y
#define _ExtinctionScale _GlobalFogParam1.z
#define _FogEnabled _GlobalFogParam1.w
#define _EnableLightShafts _GlobalFogParam2.x
#define _EnableVolumetricFog _GlobalFogParam2.y
#define _VolumetricFilteringEnabled _GlobalFogParam2.z
#define _FogDirectionalOnly _GlobalFogParam2.w
#endif