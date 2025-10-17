//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  Contact Shadows Volume组件
//--------------------------------------------------------------------------------------------------------

using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Contact Shadows", typeof(UniversalRenderPipeline))]
    public class ContactShadows : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Contact Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);
        /// <summary>
        /// Controls the length of the rays HDRP uses to calculate Contact Shadows. It is in meters, but it gets scaled by a factor depending on Distance Scale Factor
        /// and the depth of the point from where the contact shadow ray is traced.
        /// </summary>
        public ClampedFloatParameter length = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);
        /// <summary>
        /// Controls the opacity of the contact shadows.
        /// </summary>
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>
        /// Scales the length of the contact shadow ray based on the linear depth value at the origin of the ray.
        /// </summary>
        public ClampedFloatParameter distanceScaleFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>
        /// The distance from the camera, in meters, at which HDRP begins to fade out Contact Shadows.
        /// </summary>
        public MinFloatParameter maxDistance = new MinFloatParameter(50.0f, 0.0f);
        /// <summary>
        /// The distance from the camera, in meters, at which HDRP begins to fade in Contact Shadows.
        /// </summary>
        public MinFloatParameter minDistance = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>
        /// The distance, in meters, over which HDRP fades Contact Shadows out when past the Max Distance.
        /// </summary>
        public MinFloatParameter fadeDistance = new MinFloatParameter(5.0f, 0.0f);
        /// <summary>
        /// The distance, in meters, over which HDRP fades Contact Shadows in when past the Min Distance.
        /// </summary>
        public MinFloatParameter fadeInDistance = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>
        /// Controls the bias applied to the screen space ray cast to get contact shadows.
        /// </summary>
        public ClampedFloatParameter rayBias = new ClampedFloatParameter(0.2f, 0.0f, 1.0f);
        /// <summary>
        /// Controls the thickness of the objects found along the ray, essentially thickening the contact shadows.
        /// </summary>
        public ClampedFloatParameter thicknessScale = new ClampedFloatParameter(0.15f, 0.02f, 1.0f);

        internal static bool IsContactShadowsEnabled()
        {
            var contactShadows = VolumeManager.instance.stack.GetComponent<ContactShadows>();
            bool a = contactShadows.enable.value;
            bool b = contactShadows.length.value > 0.0f;
            
            return a && b;
        }
    }
}