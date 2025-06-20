using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.FilePreview
{
    [CustomEditor(typeof(ShaderInclude))]
    public class ShaderPreview : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            string path = AssetDatabase.GetAssetPath(target);
            if (path.EndsWith(".cginc") || path.EndsWith(".hlsl"))
            {
                GUI.enabled = true;
                string shaderCode = File.ReadAllText(path);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(shaderCode);
                GUILayout.EndVertical();
            }
        }
    }
}