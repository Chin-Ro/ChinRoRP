using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(UniversalRenderPipelineAsset))]
    class UniversalRenderPipelineLightEditor : LightEditor
    {
        UniversalRenderPipelineSerializedLight serializedLight { get; set; }

        private UniversalAdditionalLightData[] m_AdditionalLightDatas;
        private UniversalAdditionalLightData targetAdditionalData => m_AdditionalLightDatas[ReferenceTargetIndex(this)];
        
        static Func<Editor, int> ReferenceTargetIndex;

        static UniversalRenderPipelineLightEditor()
        {
            var type = typeof(UnityEditor.Editor);
            var propertyInfo = type.GetProperty("referenceTargetIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var getterMethodInfo = propertyInfo.GetGetMethod(true);
            var instance = Expression.Parameter(typeof(Editor), "instance");
            var getterCall = Expression.Call(instance, getterMethodInfo);
            var lambda = Expression.Lambda<Func<Editor, int>>(getterCall, instance);
            ReferenceTargetIndex = lambda.Compile();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Get & automatically add additional HD data if not present
            m_AdditionalLightDatas = CoreEditorUtils.GetAdditionalData<UniversalAdditionalLightData>(targets, UniversalAdditionalLightData.InitDefaultHDAdditionalLightData);
            serializedLight = new UniversalRenderPipelineSerializedLight(m_AdditionalLightDatas, settings);
            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
            
            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += OnUndoRedo;
            
            UniversalRenderPipelineLightUI.RegisterEditor(this);
        }

        void OnUndoRedo()
        {
            // Serialized object is lossing references after an undo
            if (serializedLight.serializedObject.targetObject != null)
            {
                serializedLight.serializedObject.Update();
                settings.Update();
                serializedLight.lightGameObject.Update();
                
                serializedObject.ApplyModifiedProperties();
                settings.ApplyModifiedProperties();
            }
        }

        internal void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }

        protected void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed -= OnUndoRedo;
            UniversalRenderPipelineLightUI.UnregisterEditor(this);
        }

        // IsPreset is an internal API - lets reuse the usable part of this function
        // 93 is a "magic number" and does not represent a combination of other flags here
        internal static bool IsPresetEditor(UnityEditor.Editor editor)
        {
            return (int)((editor.target as Component).gameObject.hideFlags) == 93;
        }

        public override void OnInspectorGUI()
        {
            serializedLight.Update();
            // Add space before the first collapsible area
            EditorGUILayout.Space();
            
            ApplyAdditionalComponentsVisibility(true);
            
            EditorGUI.BeginChangeCheck();

            if (IsPresetEditor(this))
            {
                UniversalRenderPipelineLightUI.PresetInspector.Draw(serializedLight, this);
            }
            else
            {
                UniversalRenderPipelineLightUI.Inspector.Draw(serializedLight, this);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedLight.Apply();

                foreach (var universalLightData in m_AdditionalLightDatas)
                {
                    universalLightData.UpdateAllLightValues();
                }
            }
        }
        
        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            foreach (var t in serializedLight.serializedObject.targetObjects)
                if (((UniversalAdditionalLightData)t).hideFlags == HideFlags.HideInInspector)
                    ((UniversalAdditionalLightData)t).hideFlags = HideFlags.None;
        }

        protected override void OnSceneGUI()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                return;

            if (!(target is Light light) || light == null)
                return;

            switch (light.type)
            {
                case LightType.Spot:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawSpotLightGizmo(light);
                    }
                    break;

                case LightType.Point:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawPointLightGizmo(light);
                    }
                    break;

                case LightType.Rectangle:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawRectangleLightGizmo(light);
                    }
                    break;

                case LightType.Disc:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDiscLightGizmo(light);
                    }
                    break;

                case LightType.Directional:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDirectionalLightGizmo(light);
                    }
                    break;

                default:
                    base.OnSceneGUI();
                    break;
            }
        }
    }
}
