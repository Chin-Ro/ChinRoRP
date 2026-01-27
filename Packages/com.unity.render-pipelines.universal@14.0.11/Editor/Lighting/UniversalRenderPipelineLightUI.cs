using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static partial class UniversalRenderPipelineLightUI
    {
        
        // This scope is here mainly to keep pointLightHDType isolated
        public struct LightTypeEditionScope : IDisposable
        {
            EditorGUI.PropertyScope lightTypeScope;

            public LightTypeEditionScope(Rect rect, GUIContent label, UniversalRenderPipelineSerializedLight serialized, bool isPreset)
            {
                // When editing a Light Preset, the HDAdditionalData, is not editable as is not shown on the inspector, therefore, all the properties
                // That come from the HDAdditionalData are not editable, if we use the PropertyScope for those, as they are not editable this will block
                // the edition of any property that came afterwards. So make sure that we do not use the PropertyScope if the editor is for a preset
                lightTypeScope = new EditorGUI.PropertyScope(rect, label, serialized.settings.lightType);
            }

            void IDisposable.Dispose()
            {
                lightTypeScope.Dispose();
            }
        }
        
        internal static LightUnit DrawLightIntensityUnitPopup(Rect rect, LightUnit value, UniversalLightType type)
        {
            switch (type)
            {
                case UniversalLightType.Directional:
                    return (LightUnit)EditorGUI.EnumPopup(rect, (DirectionalLightUnit)value);
                case UniversalLightType.Point:
                    return (LightUnit)EditorGUI.EnumPopup(rect, (PunctualLightUnit)value);
                case UniversalLightType.Spot:
                        return (LightUnit)EditorGUI.EnumPopup(rect, (PunctualLightUnit)value);
                default:
                    return (LightUnit)EditorGUI.EnumPopup(rect, (AreaLightUnit)value);
            }
        }
        
        static void DrawLightIntensityUnitPopup(Rect rect, UniversalRenderPipelineSerializedLight serialized, Editor owner)
        {
            LightUnit oldLigthUnit = serialized.lightUnit.GetEnumValue<LightUnit>();

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(rect, GUIContent.none, serialized.lightUnit);
            EditorGUI.showMixedValue = serialized.lightUnit.hasMultipleDifferentValues;
            var selectedLightUnit = DrawLightIntensityUnitPopup(rect, serialized.lightUnit.GetEnumValue<LightUnit>(), serialized.type);
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                ConvertLightIntensity(oldLigthUnit, selectedLightUnit, serialized, owner);
                serialized.lightUnit.SetEnumValue(selectedLightUnit);
            }
        }
        
        internal static void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, UniversalRenderPipelineSerializedLight serialized, Editor owner)
        {
            serialized.intensity.floatValue = ConvertLightIntensity(oldLightUnit, newLightUnit, serialized, owner, serialized.intensity.floatValue);
        }
        
        internal static float ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, UniversalRenderPipelineSerializedLight serialized, Editor owner, float intensity)
        {
            Light light = (Light)owner.target;

            // For punctual lights
            UniversalLightType lightType = serialized.type;
            switch (lightType)
            {
                case UniversalLightType.Directional:
                case UniversalLightType.Point:
                case UniversalLightType.Spot:
                    // Lumen ->
                    if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertPunctualLightLumenToCandela(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue);
                    else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertPunctualLightLumenToLux(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue,
                            serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertPunctualLightLumenToEv(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue);
                    // Candela ->
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightCandelaToLumen(lightType, intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle);
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertCandelaToLux(intensity, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertCandelaToEv(intensity);
                    // Lux ->
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightLuxToLumen(lightType, intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertLuxToCandela(intensity, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertLuxToEv(intensity, serialized.luxAtDistance.floatValue);
                    // EV100 ->
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightEvToLumen(lightType, intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle);
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertEvToCandela(intensity);
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertEvToLux(intensity, serialized.luxAtDistance.floatValue);
                    break;
                default:
                case (UniversalLightType)(-1): // multiple different values
                    break;  // do nothing
            }

            return intensity;
        }
        
        static void UpdateLightIntensityUnit(UniversalRenderPipelineSerializedLight serialized, Editor owner)
        {
            UniversalLightType lightType = serialized.type;
            // Box are local directional light
            if (lightType == UniversalLightType.Directional)
            {
                serialized.lightUnit.SetEnumValue((LightUnit)DirectionalLightUnit.Lux);
                // We need to reset luxAtDistance to neutral when changing to (local) directional light, otherwise first display value ins't correct
                serialized.luxAtDistance.floatValue = 1.0f;
            }
        }
        
        static void SetLightsDirty(Editor owner)
        {
            foreach (Light light in owner.targets)
                light.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
        }
    }
}