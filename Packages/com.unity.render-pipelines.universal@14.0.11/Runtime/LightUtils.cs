namespace UnityEngine.Rendering.Universal
{
    public class LightUtils
    {
        static float s_LuminanceToEvFactor => Mathf.Log(100f / ColorUtils.s_LightMeterCalibrationConstant, 2);
        static float s_EvToLuminanceFactor => -Mathf.Log(100f / ColorUtils.s_LightMeterCalibrationConstant, 2);

        // Physical light unit helper
        // All light unit are in lumen (Luminous power)
        // Punctual light (point, spot) are convert to candela (cd = lumens / steradian)

        // For our isotropic area lights which expect radiance(W / (sr* m^2)) in the shader:
        // power = Integral{area, Integral{hemisphere, radiance * <N, L>}},
        // power = area * Pi * radiance,
        // radiance = power / (area * Pi).
        // We use photometric unit, so radiance is luminance and power is luminous power

        // Ref: Moving Frostbite to PBR
        // Also good ref: https://www.radiance-online.org/community/workshops/2004-fribourg/presentations/Wandachowicz_paper.pdf

        /// <summary>
        /// Convert an intensity in Lumen to Candela for a point light
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public static float ConvertPointLightLumenToCandela(float intensity)
            => intensity / (4.0f * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a point light
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public static float ConvertPointLightCandelaToLumen(float intensity)
            => intensity * (4.0f * Mathf.PI);

        // angle is the full angle, not the half angle in radian
        // convert intensity (lumen) to candela
        /// <summary>
        /// Convert an intensity in Lumen to Candela for a cone spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angle">Full angle in radian</param>
        /// <param name="exact">Exact computation or an approximation</param>
        /// <returns></returns>
        public static float ConvertSpotLightLumenToCandela(float intensity, float angle, bool exact)
            => exact ? intensity / (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity / Mathf.PI;

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a cone pot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angle">Full angle in radian</param>
        /// <param name="exact">Exact computation or an approximation</param>
        /// <returns></returns>
        public static float ConvertSpotLightCandelaToLumen(float intensity, float angle, bool exact)
            => exact ? intensity * (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity * Mathf.PI;

        // angleA and angleB are the full opening angle, not half angle
        // convert intensity (lumen) to candela
        /// <summary>
        /// Convert an intensity in Lumen to Candela for a pyramid spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angleA">Full opening angle in radian</param>
        /// <param name="angleB">Full opening angle in radian</param>
        /// <returns></returns>
        public static float ConvertFrustrumLightLumenToCandela(float intensity, float angleA, float angleB)
            => intensity / (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a pyramid spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angleA">Full opening angle in radian</param>
        /// <param name="angleB">Full opening angle in radian</param>
        /// <returns></returns>
        public static float ConvertFrustrumLightCandelaToLumen(float intensity, float angleA, float angleB)
            => intensity * (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a sphere light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        public static float ConvertSphereLightLumenToLuminance(float intensity, float sphereRadius)
            => intensity / ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a sphere light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        public static float ConvertSphereLightLuminanceToLumen(float intensity, float sphereRadius)
            => intensity * ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a disc light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="discRadius"></param>
        /// <returns></returns>
        public static float ConvertDiscLightLumenToLuminance(float intensity, float discRadius)
            => intensity / ((discRadius * discRadius * Mathf.PI) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a disc light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="discRadius"></param>
        /// <returns></returns>
        public static float ConvertDiscLightLuminanceToLumen(float intensity, float discRadius)
            => intensity * ((discRadius * discRadius * Mathf.PI) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a rectangular light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertRectLightLumenToLuminance(float intensity, float width, float height)
            => intensity / ((width * height) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a rectangular light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertRectLightLuminanceToLumen(float intensity, float width, float height)
            => intensity * ((width * height) * Mathf.PI);

        // Helper for Lux, Candela, Luminance, Ev conversion
        /// <summary>
        /// Convert intensity in Lux at a certain distance in Candela.
        /// </summary>
        /// <param name="lux"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertLuxToCandela(float lux, float distance)
            => lux * distance * distance;

        /// <summary>
        /// Convert intensity in Candela at a certain distance in Lux.
        /// </summary>
        /// <param name="candela"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertCandelaToLux(float candela, float distance)
            => candela / (distance * distance);

        /// <summary>
        /// Convert EV100 to Luminance(nits)
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public static float ConvertEvToLuminance(float ev)
        {
            return Mathf.Pow(2, ev + s_EvToLuminanceFactor);
        }

        /// <summary>
        /// Convert EV100 to Candela
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public static float ConvertEvToCandela(float ev)
        // From punctual point of view candela and luminance is the same
            => ConvertEvToLuminance(ev);

        /// <summary>
        /// Convert EV100 to Lux at a certain distance
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertEvToLux(float ev, float distance)
        // From punctual point of view candela and luminance is the same
            => ConvertCandelaToLux(ConvertEvToLuminance(ev), distance);

        /// <summary>
        /// Convert Luminance(nits) to EV100
        /// </summary>
        /// <param name="luminance"></param>
        /// <returns></returns>
        public static float ConvertLuminanceToEv(float luminance)
        {
            return Mathf.Log(luminance, 2) + s_LuminanceToEvFactor;
        }

        /// <summary>
        /// Convert Candela to EV100
        /// </summary>
        /// <param name="candela"></param>
        /// <returns></returns>
        public static float ConvertCandelaToEv(float candela)
        // From punctual point of view candela and luminance is the same
            => ConvertLuminanceToEv(candela);

        /// <summary>
        /// Convert Lux at a certain distance to EV100
        /// </summary>
        /// <param name="lux"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertLuxToEv(float lux, float distance)
        // From punctual point of view candela and luminance is the same
            => ConvertLuminanceToEv(ConvertLuxToCandela(lux, distance));
        
        // Helper for punctual and area light unit conversion
        /// <summary>
        /// Convert a punctual light intensity in Lumen to Candela
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToCandela(UniversalLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
        {
            if (lightType == UniversalLightType.Spot && enableSpotReflector)
            {
                // We have already calculate the correct value, just assign it
                return initialIntensity;
            }
            return ConvertPointLightLumenToCandela(lumen);
        }
        
        /// <summary>
        /// Convert a punctual light intensity in Lumen to Lux
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToLux(UniversalLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector, float distance)
        {
            float candela = ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector);
            return ConvertCandelaToLux(candela, distance);
        }
        
        // This is not correct, we use candela instead of luminance but this is request from artists to support EV100 on punctual light
        /// <summary>
        /// Convert a punctual light intensity in Lumen to EV100.
        /// This is not physically correct but it's handy to have EV100 for punctual lights.
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToEv(UniversalLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
        {
            float candela = ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector);
            return ConvertCandelaToEv(candela);
        }
        
        /// <summary>
        /// Convert a punctual light intensity in Candela to Lumen
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="candela"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightCandelaToLumen(UniversalLightType lightType, float candela, bool enableSpotReflector, float spotAngle)
        {
            if (lightType == UniversalLightType.Spot && enableSpotReflector)
            {
                return ConvertSpotLightCandelaToLumen(candela, spotAngle * Mathf.Deg2Rad, true);
            }
            return ConvertPointLightCandelaToLumen(candela);
        }
        
        /// <summary>
        /// Convert a punctual light intensity in Lux to Lumen
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lux"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLuxToLumen(UniversalLightType lightType, float lux, bool enableSpotReflector, float spotAngle, float distance)
        {
            float candela = ConvertLuxToCandela(lux, distance);
            return ConvertPunctualLightCandelaToLumen(lightType, candela, enableSpotReflector, spotAngle);
        }
        
        // This is not correct, we use candela instead of luminance but this is request from artists to support EV100 on punctual light
        /// <summary>
        /// Convert a punctual light intensity in EV100 to Lumen.
        /// This is not physically correct but it's handy to have EV100 for punctual lights.
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="ev"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightEvToLumen(UniversalLightType lightType, float ev, bool enableSpotReflector, float spotAngle)
        {
            float candela = ConvertEvToCandela(ev);
            return ConvertPunctualLightCandelaToLumen(lightType, candela, enableSpotReflector, spotAngle);
        }
        
        // spotAngle in radian
        /// <summary>
        /// Calculate angles for the pyramid spot light to calculate it's intensity.
        /// </summary>
        /// <param name="aspectRatio"></param>
        /// <param name="spotAngle">angle in radian</param>
        /// <param name="angleA"></param>
        /// <param name="angleB"></param>
        public static void CalculateAnglesForPyramid(float aspectRatio, float spotAngle, out float angleA, out float angleB)
        {
            // Since the smallest angles is = to the fov, and we don't care of the angle order, simply make sure the aspect ratio is > 1
            if (aspectRatio < 1.0f)
                aspectRatio = 1.0f / aspectRatio;

            angleA = spotAngle;

            var halfAngle = angleA * 0.5f; // half of the smallest angle
            var length = Mathf.Tan(halfAngle); // half length of the smallest side of the rectangle
            length *= aspectRatio; // half length of the bigest side of the rectangle
            halfAngle = Mathf.Atan(length); // half of the bigest angle

            angleB = halfAngle * 2.0f;
        }
        
        internal static void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, UniversalAdditionalLightData universalLight, Light light)
        {
            float intensity = universalLight.intensity;
            float luxAtDistance = universalLight.luxAtDistance;
            UniversalLightType lightType = universalLight.ComputeLightType(light);
            
            // For punctual lights
            // Lumen ->
                if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertPunctualLightLumenToCandela(lightType, intensity, light.intensity, universalLight.enableSpotReflector);
                else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertPunctualLightLumenToLux(lightType, intensity, light.intensity, universalLight.enableSpotReflector, universalLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertPunctualLightLumenToEv(lightType, intensity, light.intensity, universalLight.enableSpotReflector);
                // Candela ->
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightCandelaToLumen(lightType, intensity, universalLight.enableSpotReflector, light.spotAngle);
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertCandelaToLux(intensity, universalLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertCandelaToEv(intensity);
                // Lux ->
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightLuxToLumen(lightType, intensity, universalLight.enableSpotReflector,
                        light.spotAngle, universalLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertLuxToCandela(intensity, universalLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertLuxToEv(intensity, universalLight.luxAtDistance);
                // EV100 ->
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightEvToLumen(lightType, intensity, universalLight.enableSpotReflector,
                        light.spotAngle);
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertEvToCandela(intensity);
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertEvToLux(intensity, universalLight.luxAtDistance);
        }
        
        internal static Color EvaluateLightColor(Light light, UniversalAdditionalLightData universalLight)
        {
            Color finalColor = light.color.linear * light.intensity;
            if (universalLight.useColorTemperature)
                finalColor *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
            return finalColor;
        }
    }
}