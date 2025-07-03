using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(Bloom))]
    sealed class BloomEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_BloomMode;
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Scatter;
        SerializedDataParameter m_Clamp;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Downsample;
        SerializedDataParameter m_MaxIterations;
        SerializedDataParameter m_BloomSizeScale;
        SerializedDataParameter m_Bloom1Size;
        SerializedDataParameter m_Bloom2Size;
        SerializedDataParameter m_Bloom3Size;
        SerializedDataParameter m_Bloom4Size;
        SerializedDataParameter m_Bloom5Size;
        SerializedDataParameter m_Bloom6Size;
        SerializedDataParameter m_Bloom1Tint;
        SerializedDataParameter m_Bloom2Tint;
        SerializedDataParameter m_Bloom3Tint;
        SerializedDataParameter m_Bloom4Tint;
        SerializedDataParameter m_Bloom5Tint;
        SerializedDataParameter m_Bloom6Tint;
        SerializedDataParameter m_DirtTexture;
        SerializedDataParameter m_DirtIntensity;
        
        private bool _UEBloomFolderExpanded = false;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Bloom>(serializedObject);

            m_BloomMode = Unpack(o.Find(x => x.bloomMode));
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Clamp = Unpack(o.Find(x => x.clamp));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_HighQualityFiltering = Unpack(o.Find(x => x.highQualityFiltering));
            m_Downsample = Unpack(o.Find(x => x.downscale));
            m_MaxIterations = Unpack(o.Find(x => x.maxIterations));
            m_BloomSizeScale = Unpack(o.Find(x => x.bloomSizeScale));
            m_Bloom1Size = Unpack(o.Find(x => x.bloom1Size));
            m_Bloom2Size = Unpack(o.Find(x => x.bloom2Size));
            m_Bloom3Size = Unpack(o.Find(x => x.bloom3Size));
            m_Bloom4Size = Unpack(o.Find(x => x.bloom4Size));
            m_Bloom5Size = Unpack(o.Find(x => x.bloom5Size));
            m_Bloom6Size = Unpack(o.Find(x => x.bloom6Size));
            m_Bloom1Tint = Unpack(o.Find(x => x.bloom1Tint));
            m_Bloom2Tint = Unpack(o.Find(x => x.bloom2Tint));
            m_Bloom3Tint = Unpack(o.Find(x => x.bloom3Tint));
            m_Bloom4Tint = Unpack(o.Find(x => x.bloom4Tint));
            m_Bloom5Tint = Unpack(o.Find(x => x.bloom5Tint));
            m_Bloom6Tint = Unpack(o.Find(x => x.bloom6Tint));
            m_DirtTexture = Unpack(o.Find(x => x.dirtTexture));
            m_DirtIntensity = Unpack(o.Find(x => x.dirtIntensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_BloomMode);
            PropertyField(m_Threshold);
            PropertyField(m_Intensity);
            
            if (m_BloomMode.value.enumValueIndex == (int)BloomMode.Unity)
            {
                if (m_Intensity.value.floatValue > 0f && m_Threshold.value.floatValue <= 0f)
                    EditorGUILayout.HelpBox("Threshold must be greater than 0 when intensity is greater than 0.", MessageType.Warning);
                PropertyField(m_Scatter);
                PropertyField(m_Tint);
                PropertyField(m_Clamp);
                PropertyField(m_HighQualityFiltering);

                if (m_HighQualityFiltering.overrideState.boolValue && m_HighQualityFiltering.value.boolValue && CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
                    EditorGUILayout.HelpBox("High Quality Bloom isn't supported on GLES2 platforms.", MessageType.Warning);
                
                PropertyField(m_Downsample);
                PropertyField(m_MaxIterations);
            }
            else if (m_BloomMode.value.enumValueIndex == (int)BloomMode.UE)
            {
                EditorGUI.indentLevel++;
                _UEBloomFolderExpanded = EditorGUILayout.Foldout(_UEBloomFolderExpanded, new GUIContent("UE Bloom Settings", "Settings for UE-style bloom effect."), true);
                if (_UEBloomFolderExpanded)
                {
                    PropertyField(m_BloomSizeScale);
                    PropertyField(m_Bloom1Size);
                    PropertyField(m_Bloom2Size);
                    PropertyField(m_Bloom3Size);
                    PropertyField(m_Bloom4Size);
                    PropertyField(m_Bloom5Size);
                    PropertyField(m_Bloom6Size);
                    PropertyField(m_Bloom1Tint);
                    PropertyField(m_Bloom2Tint);
                    PropertyField(m_Bloom3Tint);
                    PropertyField(m_Bloom4Tint);
                    PropertyField(m_Bloom5Tint);
                    PropertyField(m_Bloom6Tint);
                }
                
                EditorGUI.indentLevel--;
            }
            
            PropertyField(m_DirtTexture);
            PropertyField(m_DirtIntensity);
        }
    }
}
