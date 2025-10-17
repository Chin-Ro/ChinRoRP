//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  环境渲染特性GUI
//--------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(EnvironmentsRendererFeature))]
    internal class EnvironmentsRendererFeatureEditor : Editor
    {
        private SerializedProperty lightShafts;
        private SerializedProperty lightShaftsDownsample;
        private SerializedProperty lightShaftBlurNumSamples;
        private SerializedProperty lightShaftFirstPassDistance;
        private SerializedProperty volumetricLighting;
        private SerializedProperty maxLocalVolumetricFogOnScreen;
        private SerializedProperty denoisingMode;
        private SerializedProperty sliceDistributionUniformity;
        private SerializedProperty fogControl;
        private SerializedProperty screenResolutionPercentage;
        private SerializedProperty volumeSliceCount;
        private SerializedProperty volumetricFogBudget;
        private SerializedProperty resolutionDepthRatio;
        
        private bool m_IsInitialized = false;
        
        private struct Styles
        {
            public static GUIContent lightShafts = new GUIContent("LightShafts");
            public static GUIContent lightShaftsDownsample = new GUIContent("Downsample");
            public static GUIContent lightShaftBlurNumSamples = new GUIContent("Blur NumSamples");
            public static GUIContent lightShaftFirstPassDistance = new GUIContent("Blur Distance");
            public static GUIContent volumetricLighting = new GUIContent("Volumetric Lighting");
            public static GUIContent fogControl = new GUIContent("Fog Control Mode");
        }

        private void Init()
        {
            if (m_IsInitialized)
                return;
            
            lightShafts = serializedObject.FindProperty("lightShafts");
            lightShaftsDownsample = serializedObject.FindProperty("lightShaftsDownsample");
            lightShaftBlurNumSamples = serializedObject.FindProperty("lightShaftBlurNumSamples");
            lightShaftFirstPassDistance = serializedObject.FindProperty("lightShaftFirstPassDistance");
            volumetricLighting = serializedObject.FindProperty("volumetricLighting");
            maxLocalVolumetricFogOnScreen = serializedObject.FindProperty("maxLocalVolumetricFogOnScreen");
            denoisingMode = serializedObject.FindProperty("denoisingMode");
            sliceDistributionUniformity = serializedObject.FindProperty("sliceDistributionUniformity");
            fogControl = serializedObject.FindProperty("fogControl");
            screenResolutionPercentage = serializedObject.FindProperty("screenResolutionPercentage");
            volumeSliceCount = serializedObject.FindProperty("volumeSliceCount");
            volumetricFogBudget = serializedObject.FindProperty("volumetricFogBudget");
            resolutionDepthRatio = serializedObject.FindProperty("resolutionDepthRatio");
            m_IsInitialized = true;
        }
        
        public override void OnInspectorGUI()
        {
            Init();

            ValidateGraphicsApis();

            using (new EditorGUILayout.VerticalScope("frameBox"))
            {
                EditorGUILayout.PropertyField(lightShafts, Styles.lightShafts);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUI.DisabledGroupScope(!lightShafts.boolValue))
                    {
                        EditorGUILayout.PropertyField(lightShaftsDownsample, Styles.lightShaftsDownsample);
                        EditorGUILayout.PropertyField(lightShaftBlurNumSamples, Styles.lightShaftBlurNumSamples);
                        EditorGUILayout.PropertyField(lightShaftFirstPassDistance, Styles.lightShaftFirstPassDistance);
                    }
                }
            }
            using (new EditorGUILayout.VerticalScope("frameBox"))
            {
                EditorGUILayout.PropertyField(volumetricLighting, Styles.volumetricLighting);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUI.DisabledGroupScope(!volumetricLighting.boolValue))
                    {
                        EditorGUILayout.PropertyField(denoisingMode);
                        EditorGUILayout.PropertyField(maxLocalVolumetricFogOnScreen);
                        EditorGUILayout.PropertyField(sliceDistributionUniformity);
                        EditorGUILayout.PropertyField(fogControl, Styles.fogControl);
                        EditorGUI.indentLevel++;
                        if (fogControl.enumValueFlag == (int)FogControl.Manual)
                        {
                            EditorGUILayout.PropertyField(screenResolutionPercentage);
                            EditorGUILayout.PropertyField(volumeSliceCount);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(volumetricFogBudget);
                            EditorGUILayout.PropertyField(resolutionDepthRatio);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void ValidateGraphicsApis()
        {
            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;

            if (platform == BuildTarget.Android || platform == BuildTarget.iOS)
            {
                EditorGUILayout.HelpBox("VolumetricLighting are not supported on mobile device.", MessageType.Warning);
            }
        }
    }
}