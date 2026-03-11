namespace UnityEngine.Rendering.Universal
{
    /// <summary>Emissive Intensity Unit</summary>
    public enum EmissiveIntensityUnit
    {
        /// <summary>Nits</summary>
        Nits,
        /// <summary>EV100</summary>
        EV100,
    }
    
    internal static class MaterialExtension
    {
        public static void UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(this Material material)
        {
            const string kEmissiveColorLDR = "_EmissiveColorLDR";
            const string kEmissiveColor = "_EmissionColor";
            const string kEmissiveIntensity = "_EmissiveIntensity";

            if (material.HasProperty(kEmissiveColorLDR) && material.HasProperty(kEmissiveIntensity) && material.HasProperty(kEmissiveColor))
            {
                // Important: The color picker for kEmissiveColorLDR is LDR and in sRGB color space but Unity don't perform any color space conversion in the color
                // picker BUT only when sending the color data to the shader... So as we are doing our own calculation here in C#, we must do the conversion ourselves.
                Color emissiveColorLDR = material.GetColor(kEmissiveColorLDR);
                Color emissiveColorLDRLinear = new Color(Mathf.GammaToLinearSpace(emissiveColorLDR.r), Mathf.GammaToLinearSpace(emissiveColorLDR.g), Mathf.GammaToLinearSpace(emissiveColorLDR.b));
                material.SetColor(kEmissiveColor, emissiveColorLDRLinear * material.GetFloat(kEmissiveIntensity));
            }
        }
    }

    public static class UniversalMaterial
    {
        // Emission
        internal const string kUseEmissiveIntensity = "_UseEmissiveIntensity";
        internal const string kEmissiveExposureWeight = "_EmissiveExposureWeight";
        internal const string kEmissiveIntensity = "_EmissiveIntensity";
        internal const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";
        internal const string kForceForwardEmissive = "_ForceForwardEmissive";
        internal const string kEmissiveColor = "_EmissionColor";
        internal const string kEmissiveColorLDR = "_EmissiveColorLDR";
        internal const string kEmissiveColorHDR = "_EmissiveColorHDR";
        internal const string kEmissiveColorMap = "_EmissiveColorMap";
        internal const string kUVEmissive = "_UVEmissive";
        
        /// <summary>Set the Emissive Color on Lit, Unlit and Decal shaders.</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="value">The emissive color. In LDR if the material uses a separate emissive intensity value, in HDR otherwise.</param>
        public static void SetEmissiveColor(Material material, Color value)
        {
            if (material.GetFloat(kUseEmissiveIntensity) > 0.0f)
            {
                material.SetColor(kEmissiveColorLDR, value);
                material.SetColor(kEmissiveColor, value.linear * material.GetFloat(kEmissiveIntensity));
            }
            else
            {
                if (material.HasProperty(kEmissiveColorHDR))
                    material.SetColor(kEmissiveColorHDR, value);
                material.SetColor(kEmissiveColor, value);
            }
        }

        /// <summary>Set to true to use a separate LDR color and intensity value for the emission color. Compatible with Lit, Unlit and Decal shaders.</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="value">True to use separate color and intensity values.</param>
        public static void SetUseEmissiveIntensity(Material material, bool value)
        {
            material.SetFloat(kUseEmissiveIntensity, value ? 1.0f : 0.0f);
            if (value)
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();
            else if (material.HasProperty(kEmissiveColorHDR))
                material.SetColor(kEmissiveColor, material.GetColor(kEmissiveColorHDR));
        }

        /// <summary>Compares a material's color and intensity values to determine if they are different. Works with Lit, Unlit and Decal shaders.</summary>
        /// <param name="material">The material to change.</param>
        /// <returns>True if the material uses different color and intensity values.</returns>
        public static bool GetUseEmissiveIntensity(Material material)
        {
            return material.GetFloat(kUseEmissiveIntensity) > 0.0f;
        }

        /// <summary>Set the Emissive Intensity on Lit, Unlit and Decal shaders. If the material doesn't use emissive intensity, this won't have any effect.</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="intensity">The emissive intensity.</param>
        /// <param name="unit">The unit of the intensity parameter.</param>
        public static void SetEmissiveIntensity(Material material, float intensity, EmissiveIntensityUnit unit)
        {
            if (unit == EmissiveIntensityUnit.EV100)
                intensity = LightUtils.ConvertEvToLuminance(intensity);
            material.SetFloat(kEmissiveIntensity, intensity);
            material.SetFloat(kEmissiveIntensityUnit, (float)unit);
            if (material.GetFloat(kUseEmissiveIntensity) > 0.0f)
                material.SetColor(kEmissiveColor, material.GetColor(kEmissiveColorLDR).linear * intensity);
        }
    }
}