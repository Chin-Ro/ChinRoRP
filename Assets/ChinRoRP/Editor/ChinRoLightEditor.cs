using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(ChinRoRenderPipelineAsset))]
public class ChinRoLightEditor  : LightEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();
        }
    }
}
