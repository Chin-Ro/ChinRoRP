using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>Light Layers.</summary>
    [Flags]
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    /// <summary>
    /// Contains extension methods for Light class.
    /// </summary>
    public static class LightExtensions
    {
        /// <summary>
        /// Universal Render Pipeline exposes additional light data in a separate component.
        /// This method returns the additional data component for the given light or create one if it doesn't exist yet.
        /// </summary>
        /// <param name="light"></param>
        /// <returns>The <c>UniversalAdditionalLightData</c> for this light.</returns>
        /// <see cref="UniversalAdditionalLightData"/>
        public static UniversalAdditionalLightData GetUniversalAdditionalLightData(this Light light)
        {
            var gameObject = light.gameObject;
            bool componentExists = gameObject.TryGetComponent<UniversalAdditionalLightData>(out var lightData);
            if (!componentExists)
                lightData = gameObject.AddComponent<UniversalAdditionalLightData>();

            return lightData;
        }
    }
    
    // This structure contains all the old values for every recordable fields from the HD light editor
    // so we can force timeline to record changes on other fields from the LateUpdate function (editor only)
    struct TimelineWorkaround
    {
        public float oldSpotAngle;
        public Color oldLightColor;
        public Vector3 oldLossyScale;
        public bool oldDisplayAreaLightEmissiveMesh;
        public float oldLightColorTemperature;
        public float oldIntensity;
        public bool lightEnabled;
    }

    /// <summary>
    /// Class containing various additional light data used by URP.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [URPHelpURL("universal-additional-light-data")]
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide in menu
    public partial class UniversalAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        // Version 0 means serialized data before the version field.
        [SerializeField] int m_Version = 3;
        internal int version
        {
            get => m_Version;
        }

        [Tooltip("Controls if light Shadow Bias parameters use pipeline settings.")]
        [SerializeField] bool m_UsePipelineSettings = true;

        /// <summary>
        /// Controls if light Shadow Bias parameters use pipeline settings or not.
        /// </summary>
        public bool usePipelineSettings
        {
            get { return m_UsePipelineSettings; }
            set { m_UsePipelineSettings = value; }
        }

        /// <summary>
        /// Value used to indicate custom shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierCustom = -1;

        /// <summary>
        /// Value used to indicate low shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierLow = 0;

        /// <summary>
        /// Value used to indicate medium shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierMedium = 1;

        /// <summary>
        /// Value used to indicate high shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierHigh = 2;

        /// <summary>
        /// The default shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowDefaultResolutionTier = AdditionalLightsShadowResolutionTierHigh;

        /// <summary>
        /// The default custom shadow resolution for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowDefaultCustomResolution = 128;
        
        /// <summary>
        /// The default intensity value for directional lights in Lux
        /// </summary>
        public const float k_DefaultDirectionalLightIntensity = Mathf.PI; // In lux
        /// <summary>
        /// The default intensity value for punctual lights in Lumen
        /// </summary>
        public const float k_DefaultPunctualLightIntensity = 600.0f;      // Light default to 600 lumen, i.e ~48 candela

        [NonSerialized] private Light m_Light;

        /// <summary>
        /// Returns the cached light component associated with the game object that owns this light data.
        /// </summary>
#if UNITY_EDITOR
        internal new Light light
#else
        internal Light light
#endif
        {
            get
            {
                if (!m_Light)
                    TryGetComponent(out m_Light);
                return m_Light;
            }
        }

        [SerializeField] float m_Intensity;
        
        /// <summary>
        /// Get/Set the intensity of the light using the current light unit.
        /// </summary>
        public float intensity
        {
            get => m_Intensity;
            set
            {
                if (m_Intensity == value)
                    return;

                m_Intensity = Mathf.Clamp(value, 0.0f, float.MaxValue);
                UpdateLightIntensity();
            }
        }
        
        internal bool useColorTemperature
        {
            get => light.useColorTemperature;
            set
            {
                if (light.useColorTemperature == value)
                    return;

                light.useColorTemperature = value;
            }
        }

        
        [SerializeField] float m_LuxAtDistance = 1.0f;
        /// <summary>
        /// Set/Get the distance for spot lights where the emission intensity is matches the value set in the intensity property.
        /// </summary>
        public float luxAtDistance
        {
            get => m_LuxAtDistance;
            set
            {
                if (m_LuxAtDistance == value)
                    return;

                m_LuxAtDistance = Mathf.Clamp(value, 0.0f, float.MaxValue);
                UpdateLightIntensity();
            }
        }

        // Only for pyramid projector
        [SerializeField] float m_AspectRatio = 1.0f;

        // Only for Spotlight, should be hide for other light
        [SerializeField] bool m_EnableSpotReflector = true;

        /// <summary>
        /// Get/Set the Spot Reflection option on spot lights.
        /// </summary>
        public bool enableSpotReflector
        {
            get => m_EnableSpotReflector;
            set
            {
                if (m_EnableSpotReflector == value)
                    return;

                m_EnableSpotReflector = value;
                UpdateLightIntensity();
            }
        }

        /// <summary>
        /// The minimum shadow resolution for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowMinimumResolution = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier = AdditionalLightsShadowDefaultResolutionTier;

        /// <summary>
        /// Returns the selected shadow resolution tier.
        /// </summary>
        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
        }

        // The layer(s) this light belongs too.
        [Obsolete("This is obsolete, please use m_RenderingLayerMask instead.", false)]
        [SerializeField] LightLayerEnum m_LightLayerMask = LightLayerEnum.LightLayerDefault;

        /// <summary>
        /// The layer(s) this light belongs to.
        /// </summary>
        [Obsolete("This is obsolete, please use renderingLayerMask instead.", false)]
        public LightLayerEnum lightLayerMask
        {
            get { return m_LightLayerMask; }
            set { m_LightLayerMask = value; }
        }

        [SerializeField] uint m_RenderingLayers = 1;

        /// <summary>
        /// Specifies which rendering layers this light will affect.
        /// </summary>
        public uint renderingLayers
        {
            get
            {
                return m_RenderingLayers;
            }
            set
            {
                if (m_RenderingLayers != value)
                {
                    m_RenderingLayers = value;
                    SyncLightAndShadowLayers();
                }
            }
        }

        [SerializeField] bool m_CustomShadowLayers = false;

        /// <summary>
        /// Indicates whether shadows need custom layers.
        /// If not, then it uses the same settings as lightLayerMask.
        /// </summary>
        public bool customShadowLayers
        {
            get
            {
                return m_CustomShadowLayers;
            }
            set
            {
                if (m_CustomShadowLayers != value)
                {
                    m_CustomShadowLayers = value;
                    SyncLightAndShadowLayers();
                }
            }
        }

        // The layer(s) used for shadow casting.
        [SerializeField] LightLayerEnum m_ShadowLayerMask = LightLayerEnum.LightLayerDefault;

        /// <summary>
        /// The layer(s) for shadow.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowRenderingLayerMask instead.", false)]
        public LightLayerEnum shadowLayerMask
        {
            get { return m_ShadowLayerMask; }
            set { m_ShadowLayerMask = value; }
        }

        [SerializeField] uint m_ShadowRenderingLayers = 1;
        /// <summary>
        /// Specifies which rendering layers this light shadows will affect.
        /// </summary>
        public uint shadowRenderingLayers
        {
            get
            {
                return m_ShadowRenderingLayers;
            }
            set
            {
                if (value != m_ShadowRenderingLayers)
                {
                    m_ShadowRenderingLayers = value;
                    SyncLightAndShadowLayers();
                }
            }
        }

        /// <summary>
        /// Controls the size of the cookie mask currently assigned to the light.
        /// </summary>
        [Tooltip("Controls the size of the cookie mask currently assigned to the light.")]
        public Vector2 lightCookieSize
        {
            get => m_LightCookieSize;
            set => m_LightCookieSize = value;
        }
        [SerializeField] Vector2 m_LightCookieSize = Vector2.one;

        /// <summary>
        /// Controls the offset of the cookie mask currently assigned to the light.
        /// </summary>
        [Tooltip("Controls the offset of the cookie mask currently assigned to the light.")]
        public Vector2 lightCookieOffset
        {
            get => m_LightCookieOffset;
            set => m_LightCookieOffset = value;
        }
        [SerializeField] Vector2 m_LightCookieOffset = Vector2.zero;

        /// <summary>
        /// Light soft shadow filtering quality.
        /// </summary>
        [Tooltip("Controls the filtering quality of soft shadows. Higher quality has lower performance.")]
        public SoftShadowQuality softShadowQuality
        {
            get => m_SoftShadowQuality;
            set => m_SoftShadowQuality = value;
        }
        [SerializeField] private SoftShadowQuality m_SoftShadowQuality = SoftShadowQuality.UsePipelineSettings;
        
        [SerializeField] public bool useVolumetric = true;
        
        [Range(0.0f, 16.0f), SerializeField]
        float m_VolumetricDimmer = 1.0f;
        /// <summary>
        /// Get/Set the light dimmer / multiplier on volumetric effects, between 0 and 16.
        /// </summary>
        public float volumetricDimmer
        {
            get => useVolumetric ? m_VolumetricDimmer : 0.0f;
            set
            {
                if (Mathf.Approximately(m_VolumetricDimmer, value))
                    return;

                m_VolumetricDimmer = Mathf.Clamp(value, 0.0f, 16.0f);
            }
        }
        
        [Range(0.0f, 1.0f)]
        [SerializeField]
        float m_VolumetricShadowDimmer = 1.0f;
        /// <summary>
        /// Get/Set the volumetric shadow dimmer value, between 0 and 1.
        /// </summary>
        public float volumetricShadowDimmer
        {
            get => useVolumetric ? m_VolumetricShadowDimmer : 0.0f;
            set
            {
                if (Mathf.Approximately(m_VolumetricShadowDimmer, value))
                    return;

                m_VolumetricShadowDimmer = Mathf.Clamp01(value);
            }
        }

        [SerializeField] 
        private bool m_UseContactShadow = true;

        public bool useContactShadow => m_UseContactShadow;
        
        [SerializeField, Range(0.0f, 1.0f)] LightUnit m_LightUnit = LightUnit.Lumen;
        /// <summary>
        /// Get/Set the light unit. When changing the light unit, the intensity will be converted to match the previous intensity in the new unit.
        /// </summary>
        public LightUnit lightUnit
        {
            get => m_LightUnit;
            set
            {
                if (m_LightUnit == value)
                    return;

                if (!IsValidLightUnitForType(type, value))
                {
                    var supportedTypes = String.Join(", ", GetSupportedLightUnits(type));
                    Debug.LogError($"Set Light Unit '{value}' to a {GetLightTypeName()} is not allowed, only {supportedTypes} are supported.");
                    return;
                }

                LightUtils.ConvertLightIntensity(m_LightUnit, value, this, light);

                m_LightUnit = value;
                UpdateLightIntensity();
            }
        }
        
        [NonSerialized]
        TimelineWorkaround timelineWorkaround = new TimelineWorkaround();
        /// <summary>
        /// Synchronize all the HD Additional Light values with the Light component.
        /// </summary>
        public void UpdateAllLightValues()
        {
            // Update light intensity
            UpdateLightIntensity();
        }

        void UpdateLightIntensity()
        {
            if (lightUnit == LightUnit.Lumen)
            {
                SetLightIntensityPunctual(intensity);
            }
            else if (lightUnit == LightUnit.Ev100)
            {
                light.intensity = LightUtils.ConvertEvToLuminance(m_Intensity);
            }
            else
            {
                UniversalLightType lightType = type;
                if ((lightType == UniversalLightType.Spot || lightType == UniversalLightType.Point) && lightUnit == LightUnit.Lux)
                {
                    light.intensity = LightUtils.ConvertLuxToCandela(m_Intensity, luxAtDistance);
                }
                else
                {
                    light.intensity = m_Intensity;
                }
            }
#if UNITY_EDITOR
            light.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
#endif
        }

        void SetLightIntensityPunctual(float intensity)
        {
            switch (type)
            {
                case UniversalLightType.Directional:
                    light.intensity = intensity;
                    break;
                case UniversalLightType.Point:
                    if (lightUnit == LightUnit.Candela)
                        light.intensity = intensity;
                    else
                        light.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                    break;
                case UniversalLightType.Spot:
                    if (lightUnit == LightUnit.Candela)
                    {
                        light.intensity = intensity;
                    }
                    else // lumen
                    {
                        if (enableSpotReflector)
                        {
                            light.intensity = LightUtils.ConvertSpotLightLumenToCandela(intensity, light.spotAngle * Mathf.Deg2Rad, true);
                        }
                        else
                        {
                            // No reflector, angle act as occlusion of point light.
                            light.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                        }
                    }

                    break;
            }
        }

        public static void InitDefaultHDAdditionalLightData(UniversalAdditionalLightData lightData)
        {
            // Special treatment for Unity built-in area light. Change it to our rectangle light
            var light = lightData.gameObject.GetComponent<Light>();
            
            // Set light intensity and unit using its type
            //note: requiring type convert Rectangle and Disc to Area and correctly set areaLight
            switch (lightData.type)
            {
                case UniversalLightType.Directional:
                    lightData.lightUnit = LightUnit.Lux;
                    lightData.intensity = k_DefaultDirectionalLightIntensity / Mathf.PI * 100000.0f; // Change back to just k_DefaultDirectionalLightIntensity on 11.0.0 (can't change constant as it's a breaking change)
                    break;
                case UniversalLightType.Point:
                case UniversalLightType.Spot:
                    lightData.lightUnit = LightUnit.Lumen;
                    lightData.intensity = k_DefaultPunctualLightIntensity;
                    break;
            }

            // We don't use the global settings of shadow mask by default
            light.lightShadowCasterMode = LightShadowCasterMode.Everything;
            
            // Enable filter/temperature mode by default for all light types
            lightData.useColorTemperature = true;
        }

        void LateUpdate()
        {
            // Check if the intensity have been changed by the inspector or an animator
            if (timelineWorkaround.oldLossyScale != transform.lossyScale
                || intensity != timelineWorkaround.oldIntensity
                || light.colorTemperature != timelineWorkaround.oldLightColorTemperature)
            {
                UpdateLightIntensity();
                timelineWorkaround.oldLossyScale = transform.lossyScale;
                timelineWorkaround.oldIntensity = intensity;
                timelineWorkaround.oldLightColorTemperature = light.colorTemperature;
            }
            
            // Same check for light angle to update intensity using spot angle
            if (type == UniversalLightType.Spot && (timelineWorkaround.oldSpotAngle != light.spotAngle))
            {
                UpdateLightIntensity();
                timelineWorkaround.oldSpotAngle = light.spotAngle;
            }
        }

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            if (m_Version < 2)
            {
#pragma warning disable 618 // Obsolete warning
                m_RenderingLayers = (uint)m_LightLayerMask;
                m_ShadowRenderingLayers = (uint)m_ShadowLayerMask;
#pragma warning restore 618 // Obsolete warning
                m_Version = 2;
            }

            if (m_Version < 3)
            {
                // SoftShadowQuality.UsePipelineSettings added at index 0. Bump existing serialized values by 1. e.g. Low(0) -> Low(1).
                m_SoftShadowQuality = (SoftShadowQuality)(Math.Clamp((int)m_SoftShadowQuality + 1, 0, (int)SoftShadowQuality.High));
                m_Version = 3;
            }
        }

        private void SyncLightAndShadowLayers()
        {
            if (light)
                light.renderingLayerMask = m_CustomShadowLayers ? (int)m_ShadowRenderingLayers : (int)m_RenderingLayers;
        }
    }
}
