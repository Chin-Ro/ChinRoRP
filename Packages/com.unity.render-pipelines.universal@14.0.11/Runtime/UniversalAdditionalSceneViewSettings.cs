
namespace UnityEngine.Rendering.Universal
{
#if UNITY_EDITOR
    using UnityEditor;
    
    [InitializeOnLoad]
    static class UniversalAdditionalSceneViewSettings
    {
        static class Styles
        {
            public static readonly GUIContent OverrideExposure = EditorGUIUtility.TrTextContent("Override Exposure", "When enabled, the scene exposure is overridden with the selected value.");
            public static readonly GUIContent OverriddenExposure = EditorGUIUtility.TrTextContent("Scene Exposure", "The value for the overridden exposure.");
        }
        
        // Helper class to manage editor preferences with local caching.
        // Only supports bools, floats and ints/enums, so we keep it local for now.
        class CachedEditorPref<T>
        {
            T m_Storage;
            string m_Key;

            public T value
            {
                // We update the Editor prefs only when writing. Reading goes through the cached local var to ensure that reads have no overhead.
                get => m_Storage;
                set
                {
                    m_Storage = value;
                    SetPref(value);
                }
            }

            // Creates a cached editor preference using the specified key and default value
            public CachedEditorPref(string key, T dafaultValue)
            {
                m_Key = key;
                m_Storage = GetOrCreatePref(dafaultValue);
            }

            T GetOrCreatePref(T defaultValue)
            {
                if (EditorPrefs.HasKey(m_Key))
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)EditorPrefs.GetBool(m_Key);
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        return (T)(object)EditorPrefs.GetFloat(m_Key);
                    }
                    return (T)(object)EditorPrefs.GetInt(m_Key);
                }
                else
                {
                    if (typeof(T) == typeof(bool))
                    {
                        EditorPrefs.SetBool(m_Key, (bool)(object)defaultValue);
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        EditorPrefs.SetFloat(m_Key, (float)(object)defaultValue);
                    }
                    else
                    {
                        EditorPrefs.SetInt(m_Key, (int)(object)defaultValue);
                    }
                    return defaultValue;
                }
            }

            void SetPref(T value)
            {
                if (typeof(T) == typeof(bool))
                    EditorPrefs.SetBool(m_Key, (bool)(object)value);
                else if (typeof(T) == typeof(float))
                    EditorPrefs.SetFloat(m_Key, (float)(object)value);
                else
                    EditorPrefs.SetInt(m_Key, (int)(object)value);
            }
        }
        
        static CachedEditorPref<bool> s_SceneExposureOverride = new CachedEditorPref<bool>("HDRP:SceneViewCamera:OverrideExposure", false);

        public static bool sceneExposureOverriden
        {
            get => s_SceneExposureOverride.value;
            set => s_SceneExposureOverride.value = value;
        }

        static CachedEditorPref<float> s_SceneExposure = new CachedEditorPref<float>("HDRP:SceneViewCamera:Exposure", 10.0f);

        public static float sceneExposure
        {
            get => s_SceneExposure.value;
            set => s_SceneExposure.value = value;
        }
            
        static UniversalAdditionalSceneViewSettings()
        {
            SceneViewCameraWindow.additionalSettingsGui += DoAdditionalSettings;
        }
            
        static void DoAdditionalSettings(SceneView sceneView)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Universal Render Pipeline", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sceneExposureOverriden = EditorGUILayout.Toggle(Styles.OverrideExposure, sceneExposureOverriden);
            if (sceneExposureOverriden)
                sceneExposure = EditorGUILayout.Slider(Styles.OverriddenExposure, sceneExposure, -11.0f, 16.0f);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }
    }
#endif
}