//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  雾效组件GUI
//--------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(Fog))]
    public class FogEditor : VolumeComponentEditor
    {
        private SerializedDataParameter enabled;
        
        private SerializedDataParameter enableAtmosphereFog;
        private SerializedDataParameter baseHeight;
        private SerializedDataParameter maximumHeight;
        private SerializedDataParameter meanFreePath;
        private SerializedDataParameter mipFogMaxMip;
        private SerializedDataParameter mipFogNear;
        private SerializedDataParameter mipFogFar;
        
        private SerializedDataParameter fogMaxOpacity;
        private SerializedDataParameter fogFirstDensity;
        private SerializedDataParameter fogFirstHeightFalloff;
        private SerializedDataParameter fogSecondDensity;
        private SerializedDataParameter fogSecondHeightFalloff;
        private SerializedDataParameter fogSecondHeight;
        private SerializedDataParameter fogInscatteringColor;
        private SerializedDataParameter skyContributeFactor;
        private SerializedDataParameter fogStartDistance;
        private SerializedDataParameter fogEndDistance;
        private SerializedDataParameter fogCutoffDistance;
        private SerializedDataParameter inScatterExponent;
        private SerializedDataParameter inScatteringStartDistance;
        private SerializedDataParameter inScatterColor;
        
        private SerializedDataParameter enableVolumetricFog;
        private SerializedDataParameter albedo;
        private SerializedDataParameter extinctionScale;
        private SerializedDataParameter depthExtent;
        private SerializedDataParameter anisotropy;
        private SerializedDataParameter directionalLightsOnly;
        
        private SerializedDataParameter enableLightShafts;
        private SerializedDataParameter occlusionMaskDarkness;
        private SerializedDataParameter occlusionDepthRange;
        private SerializedDataParameter lightShaftBloom;
        private SerializedDataParameter bloomScale;
        private SerializedDataParameter bloomThreshold;
        private SerializedDataParameter bloomMaxBrightness;
        private SerializedDataParameter bloomTint;
        
        private bool _SecondFogDataExpand = true;
        
        private struct Style
        {
            public static GUIContent enableAtmosphereFog = new GUIContent("Atmosphere Fog");
            public static GUIContent meanFreePath = new GUIContent("Fog Attenuation Distance");
            public static GUIContent fogFirstDensity = new GUIContent("Fog Density");
            public static GUIContent fogFirstHeightFalloff = new GUIContent("Fog Height Falloff");
            public static GUIContent secondFogExpand = new GUIContent("Second Fog Data");
            public static GUIContent fogStartDistance = new GUIContent("Start Distance");
            public static GUIContent fogEndDistance = new GUIContent("End Distance");
            public static GUIContent inScatterExponent = new GUIContent("Inscattering Exponent");
            public static GUIContent inScatteringStartDistance = new GUIContent("Inscattering Start Distance");
            public static GUIContent inScatterColor = new GUIContent("Inscattering Color");
            public static GUIContent enableVolumetricFog = new GUIContent("Volumetric Fog");
            public static GUIContent depthExtent = new GUIContent("View Distance");
            public static GUIContent enableLightShafts = new GUIContent("Light Shafts");
        }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Fog>(serializedObject);
            
            enabled = Unpack(o.Find(x => x.enabled));
            
            enableAtmosphereFog = Unpack(o.Find(x => x.enableAtmosphereFog));
            baseHeight = Unpack(o.Find(x => x.baseHeight));
            maximumHeight = Unpack(o.Find(x => x.maximumHeight));
            meanFreePath = Unpack(o.Find(x => x.meanFreePath));
            mipFogMaxMip = Unpack(o.Find(x => x.mipFogMaxMip));
            mipFogNear = Unpack(o.Find(x => x.mipFogNear));
            mipFogFar = Unpack(o.Find(x => x.mipFogFar));
            
            fogMaxOpacity = Unpack(o.Find(x => x.fogMaxOpacity));
            fogFirstDensity = Unpack(o.Find(x => x.fogFirstDensity));
            fogFirstHeightFalloff = Unpack(o.Find(x => x.fogFirstHeightFalloff));
            fogSecondDensity = Unpack(o.Find(x => x.fogSecondDensity));
            fogSecondHeightFalloff = Unpack(o.Find(x => x.fogSecondHeightFalloff));
            fogSecondHeight = Unpack(o.Find(x => x.fogSecondHeight));
            fogInscatteringColor = Unpack(o.Find(x => x.fogInscatteringColor));
            fogStartDistance = Unpack(o.Find(x => x.fogStartDistance));
            fogEndDistance = Unpack(o.Find(x => x.fogEndDistance));
            fogCutoffDistance = Unpack(o.Find(x => x.fogCutoffDistance));
            inScatterExponent = Unpack(o.Find(x => x.inScatterExponent));
            inScatteringStartDistance = Unpack(o.Find(x => x.inScatteringStartDistance));
            inScatterColor = Unpack(o.Find(x => x.inScatterColor));
            
            enableVolumetricFog = Unpack(o.Find(x => x.enableVolumetricFog));
            albedo = Unpack(o.Find(x => x.albedo));
            extinctionScale = Unpack(o.Find(x => x.extinctionScale));
            skyContributeFactor = Unpack(o.Find(x => x.skyContributeFactor));
            depthExtent = Unpack(o.Find(x => x.depthExtent));
            anisotropy = Unpack(o.Find(x => x.anisotropy));
            directionalLightsOnly = Unpack(o.Find(x => x.directionalLightsOnly));
            
            enableLightShafts = Unpack(o.Find(x => x.enableLightShafts));
            occlusionMaskDarkness = Unpack(o.Find(x => x.occlusionMaskDarkness));
            occlusionDepthRange = Unpack(o.Find(x => x.occlusionDepthRange));
            lightShaftBloom = Unpack(o.Find(x => x.lightShaftBloom));
            bloomScale = Unpack(o.Find(x => x.bloomScale));
            bloomThreshold = Unpack(o.Find(x => x.bloomThreshold));
            bloomMaxBrightness = Unpack(o.Find(x => x.bloomMaxBrightness));
            bloomTint = Unpack(o.Find(x => x.bloomTint));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(enabled);

            using (new EditorGUILayout.VerticalScope("frameBox"))
            {
                PropertyField(enableAtmosphereFog, Style.enableAtmosphereFog);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropertyField(meanFreePath, Style.meanFreePath);
                    PropertyField(baseHeight);
                    PropertyField(maximumHeight);
                    PropertyField(mipFogMaxMip);
                    PropertyField(mipFogNear);
                    PropertyField(mipFogFar);
                }
            }
            
            using (new EditorGUILayout.VerticalScope("frameBox"))
            {
                EditorGUILayout.LabelField("Height Fog");
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropertyField(fogFirstDensity, Style.fogFirstDensity);
                    PropertyField(fogFirstHeightFalloff,  Style.fogFirstHeightFalloff);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUI.indentLevel++;
                        _SecondFogDataExpand = EditorGUILayout.Foldout(_SecondFogDataExpand, Style.secondFogExpand, true);
                        if (_SecondFogDataExpand)
                        {
                            EditorGUI.indentLevel--;
                            PropertyField(fogSecondDensity);
                            PropertyField(fogSecondHeightFalloff);
                            PropertyField(fogSecondHeight);
                            EditorGUI.indentLevel++;
                        }
                        EditorGUI.indentLevel--;
                    }
                    PropertyField(fogInscatteringColor);
                    PropertyField(fogMaxOpacity);
                    PropertyField(skyContributeFactor);
                    PropertyField(fogStartDistance, Style.fogStartDistance);
                    PropertyField(fogEndDistance, Style.fogEndDistance);
                    PropertyField(fogCutoffDistance);
                }
                EditorGUILayout.LabelField("Directional Inscattering");
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropertyField(inScatterExponent, Style.inScatterExponent);
                    PropertyField(inScatteringStartDistance, Style.inScatteringStartDistance);
                    PropertyField(inScatterColor, Style.inScatterColor);
                }
                
                EditorGUILayout.LabelField("Volumetric Fog");
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropertyField(enableVolumetricFog, Style.enableVolumetricFog);
                    PropertyField(albedo);
                    PropertyField(extinctionScale);
                    PropertyField(depthExtent, Style.depthExtent);
                    PropertyField(anisotropy);
                    PropertyField(directionalLightsOnly);
                }
            }
            
            using (new EditorGUILayout.VerticalScope("frameBox"))
            {
                PropertyField(enableLightShafts, Style.enableLightShafts);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropertyField(occlusionMaskDarkness);
                    PropertyField(occlusionDepthRange);
                    PropertyField(lightShaftBloom);
                    PropertyField(bloomScale);
                    PropertyField(bloomThreshold);
                    PropertyField(bloomMaxBrightness);
                    PropertyField(bloomTint);
                }
            }
        }
    }
}