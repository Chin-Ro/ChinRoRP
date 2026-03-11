
#ifndef UNIVERSAL_DEBUGGING_COMMON_INCLUDED
#define UNIVERSAL_DEBUGGING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebugViewEnums.cs.hlsl"

#if defined(DEBUG_DISPLAY)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

// Material settings...
int _DebugMaterialMode;
int _DebugVertexAttributeMode;
int _DebugMaterialValidationMode;

// Rendering settings...
int _DebugFullScreenMode;
int _DebugSceneOverrideMode;
int _DebugMipInfoMode;
int _DebugValidationMode;

// Lighting settings...
int _DebugLightingMode;
int _DebugLightingFeatureFlags;

half _DebugValidateAlbedoMinLuminance = 0.01;
half _DebugValidateAlbedoMaxLuminance = 0.90;
half _DebugValidateAlbedoSaturationTolerance = 0.214;
half _DebugValidateAlbedoHueTolerance = 0.104;
half3 _DebugValidateAlbedoCompareColor = half3(0.5, 0.5, 0.5);

half _DebugValidateMetallicMinValue = 0;
half _DebugValidateMetallicMaxValue = 0.9;

float4 _DebugColor;
float4 _DebugColorInvalidMode;
float4 _DebugValidateBelowMinThresholdColor;
float4 _DebugValidateAboveMaxThresholdColor;
float4 _MousePixelCoord;

TEXTURE2D(_DebugFont); // Debug font to write string in shader
sampler s_point_clamp_sampler;

half3 GetDebugColor(uint index)
{
    uint clampedIndex = clamp(index, 0, DEBUG_COLORS_COUNT-1);
    return kDebugColorGradient[clampedIndex].rgb;
}

bool TryGetDebugColorInvalidMode(out half4 debugColor)
{
    // Depending upon how we want to deal with invalid modes, this code may need to change,
    // for now we'll simply make each pixel use "_DebugColorInvalidMode"...
    debugColor = _DebugColorInvalidMode;
    return true;
}

uint GetMipMapLevel(float2 nonNormalizedUVCoordinate)
{
    // The OpenGL Graphics System: A Specification 4.2
    //  - chapter 3.9.11, equation 3.21

    float2  dx_vtc = ddx(nonNormalizedUVCoordinate);
    float2  dy_vtc = ddy(nonNormalizedUVCoordinate);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));

    return (uint)(0.5 * log2(delta_max_sqr));
}

bool CalculateValidationAlbedo(half3 albedo, out half4 color)
{
    half luminance = Luminance(albedo);

    if (luminance < _DebugValidateAlbedoMinLuminance)
    {
        color = _DebugValidateBelowMinThresholdColor;
    }
    else if (luminance > _DebugValidateAlbedoMaxLuminance)
    {
        color = _DebugValidateAboveMaxThresholdColor;
    }
    else
    {
        half3 hsv = RgbToHsv(albedo);
        half hue = hsv.r;
        half sat = hsv.g;

        half3 compHSV = RgbToHsv(_DebugValidateAlbedoCompareColor.rgb);
        half compHue = compHSV.r;
        half compSat = compHSV.g;

        if ((compSat - _DebugValidateAlbedoSaturationTolerance > sat) || ((compHue - _DebugValidateAlbedoHueTolerance > hue) && (compHue - _DebugValidateAlbedoHueTolerance + 1.0 > hue)))
        {
            color = _DebugValidateBelowMinThresholdColor;
        }
        else if ((sat > compSat + _DebugValidateAlbedoSaturationTolerance) || ((hue > compHue + _DebugValidateAlbedoHueTolerance) && (hue > compHue + _DebugValidateAlbedoHueTolerance - 1.0)))
        {
            color = _DebugValidateAboveMaxThresholdColor;
        }
        else
        {
            color = half4(luminance, luminance, luminance, 1.0);
        }
    }
    return true;
}

bool CalculateColorForDebugSceneOverride(out half4 color)
{
    if (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE)
    {
        color = 0;
        return false;
    }
    else
    {
        color = _DebugColor;
        return true;
    }
}


// DebugFont code assume black and white font with texture size 256x128 with bloc of 16x16
#define DEBUG_FONT_TEXT_WIDTH   16
#define DEBUG_FONT_TEXT_HEIGHT  16
#define DEBUG_FONT_TEXT_COUNT_X 16
#define DEBUG_FONT_TEXT_COUNT_Y 8
#define DEBUG_FONT_TEXT_ASCII_START 32

#define DEBUG_FONT_TEXT_SCALE_WIDTH 10 // This control the spacing between characters (if a character fill the text block it will overlap).

// Only support ASCII symbol from DEBUG_FONT_TEXT_ASCII_START to 126
// return black or white depends if we hit font character or not
// currentUnormCoord is current unormalized screen position
// fixedUnormCoord is the position where we want to draw something, this will be incremented by block font size in provided direction
// color is current screen color
// color of the font to use
// direction is 1 or -1 and indicate fixedUnormCoord block shift
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int direction, int fontTextScaleWidth)
{
    // Are we inside a font display block on the screen ?
    uint2 localCharCoord = currentUnormCoord - fixedUnormCoord;
    if (localCharCoord.x >= 0 && localCharCoord.x < DEBUG_FONT_TEXT_WIDTH && localCharCoord.y >= 0 && localCharCoord.y < DEBUG_FONT_TEXT_HEIGHT)
    {
        localCharCoord.y = DEBUG_FONT_TEXT_HEIGHT - localCharCoord.y;

        asciiValue -= DEBUG_FONT_TEXT_ASCII_START; // Our font start at ASCII table 32;
        uint2 asciiCoord = uint2(asciiValue % DEBUG_FONT_TEXT_COUNT_X, asciiValue / DEBUG_FONT_TEXT_COUNT_X);
        // Unorm coordinate inside the font texture
        uint2 unormTexCoord = asciiCoord * uint2(DEBUG_FONT_TEXT_WIDTH, DEBUG_FONT_TEXT_HEIGHT) + localCharCoord;
        // normalized coordinate
        float2 normTexCoord = float2(unormTexCoord) / float2(DEBUG_FONT_TEXT_WIDTH * DEBUG_FONT_TEXT_COUNT_X, DEBUG_FONT_TEXT_HEIGHT * DEBUG_FONT_TEXT_COUNT_Y);

#if UNITY_UV_STARTS_AT_TOP
        normTexCoord.y = 1.0 - normTexCoord.y;
#endif

        float charColor = SAMPLE_TEXTURE2D_LOD(_DebugFont, s_point_clamp_sampler, normTexCoord, 0).r;
        color = color * (1.0 - charColor) + charColor * fontColor;
    }

    fixedUnormCoord.x += fontTextScaleWidth * direction;
}

void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int direction)
{
    DrawCharacter(asciiValue, fontColor, currentUnormCoord, fixedUnormCoord, color, direction, DEBUG_FONT_TEXT_SCALE_WIDTH);
}

// Shortcut to not have to file direction
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color)
{
    DrawCharacter(asciiValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 1);
}

// Draw a signed integer
// Can't display more than 16 digit
// The two following parameter are for float representation
// leading0 is used when drawing frac part of a float to draw the leading 0 (call is in charge of it)
// forceNegativeSign is used to force to display a negative sign as -0 is not recognize
void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int leading0, bool forceNegativeSign)
{
    const uint maxStringSize = 16;

    uint absIntValue = abs(intValue);

    // 1. Get size of the number of display
    int numEntries = min((intValue == 0 ? 0 : log10(absIntValue)) + ((intValue < 0 || forceNegativeSign) ? 1 : 0) + leading0, maxStringSize);

    // 2. Shift curseur to last location as we will go reverse
    fixedUnormCoord.x += numEntries * DEBUG_FONT_TEXT_SCALE_WIDTH;

    // 3. Display the number
    bool drawCharacter = true; // bit weird, but it is to appease the compiler.
    for (uint j = 0; j < maxStringSize; ++j)
    {
        // Numeric value incurrent font start on the second row at 0
        if(drawCharacter)
            DrawCharacter((absIntValue % 10) + '0', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);

        if (absIntValue  < 10)
            drawCharacter = false;

        absIntValue /= 10;
    }

    // 4. Display leading 0
    if (leading0 > 0)
    {
        for (int i = 0; i < leading0; ++i)
        {
            DrawCharacter('0', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);
        }
    }

    // 5. Display sign
    if (intValue < 0 || forceNegativeSign)
    {
        DrawCharacter('-', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);
    }

    // 6. Reset cursor at end location
    fixedUnormCoord.x += (numEntries + 2) * DEBUG_FONT_TEXT_SCALE_WIDTH;
}

void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color)
{
    DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 0, false);
}

void DrawFloatExplicitPrecision(float floatValue, float3 fontColor, uint2 currentUnormCoord, uint digitCount, inout uint2 fixedUnormCoord, inout float3 color)
{
    if (IsNaN(floatValue))
    {
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, color);
        DrawCharacter('a', fontColor, currentUnormCoord, fixedUnormCoord, color);
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, color);
    }
    else
    {
        int intValue = int(floatValue);
        bool forceNegativeSign = floatValue >= 0.0f ? false : true;
        DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 0, forceNegativeSign);
        DrawCharacter('.', fontColor, currentUnormCoord, fixedUnormCoord, color);
        int fracValue = int(frac(abs(floatValue)) * pow(10, digitCount));
        int leading0 = digitCount - (int(log10(fracValue)) + 1); // Counting leading0 to add in front of the float
        DrawInteger(fracValue, fontColor, currentUnormCoord, fixedUnormCoord, color, leading0, false);
    }
}

#endif

bool IsAlphaDiscardEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE);
    #else
    return true;
    #endif
}

bool IsFogEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugMaterialMode == DEBUGMATERIALMODE_NONE) &&
           (_DebugVertexAttributeMode == DEBUGVERTEXATTRIBUTEMODE_NONE) &&
           (_DebugMaterialValidationMode == DEBUGMATERIALVALIDATIONMODE_NONE) &&
           (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE) &&
           (_DebugMipInfoMode == DEBUGMIPINFOMODE_NONE) &&
           (_DebugLightingMode == DEBUGLIGHTINGMODE_NONE) &&
           (_DebugLightingFeatureFlags == 0) &&
           (_DebugValidationMode == DEBUGVALIDATIONMODE_NONE);
    #else
    return true;
    #endif
}

bool IsLightingFeatureEnabled(uint bitMask)
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugLightingFeatureFlags == 0) || ((_DebugLightingFeatureFlags & bitMask) != 0);
    #else
    return true;
    #endif
}

bool IsOnlyAOLightingFeatureEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return _DebugLightingFeatureFlags == DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION;
    #else
    return false;
    #endif
}

#endif
