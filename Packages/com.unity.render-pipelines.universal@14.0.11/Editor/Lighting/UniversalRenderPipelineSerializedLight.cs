using UnityEngine.Rendering.Universal;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineSerializedLight : ISerializedLight
    {
        /// <summary>The base settings of the light</summary>
        public LightEditor.Settings settings { get; }
        /// <summary>The light serialized</summary>
        public SerializedObject serializedObject { get; }
        /// <summary>The additional light data serialized</summary>
        public SerializedObject serializedAdditionalDataObject { get; private set; }
        
        public SerializedObject lightGameObject;

        public UniversalAdditionalLightData additionalLightData => lightsAdditionalData[0];
        public UniversalAdditionalLightData[] lightsAdditionalData { get; private set; }

        // Common SRP's Lights properties
        public SerializedProperty intensity { get; }
        
        // Specific URP Light properties
        public SerializedProperty lightUnit { get; }
        public SerializedProperty enableSpotReflector { get; }
        public SerializedProperty luxAtDistance { get; }

        // URP Light Properties
        public SerializedProperty useAdditionalDataProp { get; }                     // Does light use shadow bias settings defined in UniversalRP asset file?
        public SerializedProperty additionalLightsShadowResolutionTierProp { get; }  // Index of the AdditionalLights ShadowResolution Tier
        public SerializedProperty softShadowQualityProp { get; }                     // Per light soft shadow filtering quality.
        public SerializedProperty lightCookieSizeProp { get; }                       // Multi dimensional light cookie size replacing `cookieSize` in legacy light.
        public SerializedProperty lightCookieOffsetProp { get; }                     // Multi dimensional light cookie offset.
        public SerializedProperty useContactShadowsProp { get; }

        // Volumetric
        public SerializedProperty useVolumetricProp { get; }
        public SerializedProperty volumetricDimmerProp { get; }
        public SerializedProperty volumetricShadowDimmerProp { get; }

        // Light layers related
        public SerializedProperty renderingLayers { get; }
        public SerializedProperty customShadowLayers { get; }
        public SerializedProperty shadowRenderingLayers { get; }

        internal SerializedProperty pointLightUniversalType;

        public UniversalLightType type
        {
            get => haveMultipleTypeValue ? (UniversalLightType)(-1) : ((UniversalAdditionalLightData)serializedObject.targetObjects[0]).type;
            set
            {
                //Note: type is split in both component
                var undoObjects = serializedObject.targetObjects.SelectMany((Object x) => new Object[] { x, (x as UniversalAdditionalLightData).light }).ToArray();
                Undo.RecordObjects(undoObjects, "Change light type");
                var objects = serializedObject.targetObjects;
                for (int index = 0; index < objects.Length; ++index)
                    (objects[index] as UniversalAdditionalLightData).type = value;
                serializedObject.Update();
            }
        }

        private bool haveMultipleTypeValue
        {
            get
            {
                var objects = serializedObject.targetObjects;
                UniversalLightType value = (objects[0] as UniversalAdditionalLightData).type;
                for (int index = 1; index < objects.Length; ++index)
                    if (value != (objects[index] as UniversalAdditionalLightData).type)
                        return true;
                return false;
            }
        }

        /// <summary>Method that updates the <see cref="SerializedObject"/> of the Light and the Additional Light Data</summary>
        public void Update()
        {
            // Case 1182968
            // For some reasons, the is different cache is not updated while we actually have different
            // values for shadowResolution.level
            // So we force the update here as a workaround
            serializedObject.SetIsDifferentCacheDirty();
            
            serializedObject.Update();
            settings.Update();
        }

        /// <summary>Method that applies the modified properties the <see cref="SerializedObject"/> of the Light and the Light Camera Data</summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
        }

        /// <summary>Constructor</summary>
        /// <param name="serializedObject"><see cref="SerializedObject"/> with the light</param>
        /// <param name="settings"><see cref="LightEditor.Settings"/>with the settings</param>
        public UniversalRenderPipelineSerializedLight(UniversalAdditionalLightData[] lightDatas, LightEditor.Settings settings)
        {
            serializedObject = new SerializedObject(lightDatas);
            this.settings = settings;
            settings.OnEnable();
            using (var o = new PropertyFetcher<UniversalAdditionalLightData>(serializedObject))
            {
                intensity = o.Find("m_Intensity");
            
                lightUnit = o.Find("m_LightUnit");
                enableSpotReflector = o.Find("m_EnableSpotReflector");
                luxAtDistance = o.Find("m_LuxAtDistance");

                useAdditionalDataProp = o.Find("m_UsePipelineSettings");
                additionalLightsShadowResolutionTierProp = o.Find("m_AdditionalLightsShadowResolutionTier");
                softShadowQualityProp = o.Find("m_SoftShadowQuality");
                lightCookieSizeProp = o.Find("m_LightCookieSize");
                lightCookieOffsetProp = o.Find("m_LightCookieOffset");
                useContactShadowsProp = o.Find("m_UseContactShadow");

                renderingLayers = o.Find("m_RenderingLayers");
                customShadowLayers = o.Find("m_CustomShadowLayers");
                shadowRenderingLayers = o.Find("m_ShadowRenderingLayers");
            
                useVolumetricProp = o.Find("useVolumetric");
                volumetricDimmerProp = o.Find("m_VolumetricDimmer");
                volumetricShadowDimmerProp = o.Find("m_VolumetricShadowDimmer");
            }

            settings.ApplyModifiedProperties();
            
            lightGameObject = new SerializedObject(serializedObject.targetObjects.Select(ld => ((UniversalAdditionalLightData)ld).gameObject).ToArray());
        }
    }
}
