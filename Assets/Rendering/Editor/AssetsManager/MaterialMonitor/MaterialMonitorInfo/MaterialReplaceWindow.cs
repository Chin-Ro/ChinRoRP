using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class MaterialReplaceWindow : EditorWindow
    {
        private List<Material> _materials;
        private Shader _replaceShader;
        public void Init(List<Material> materials)
        {
            _materials = materials;
        }

        public static void DrawWindow(List<Material> materials)
        {
            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有材质可供替换", "确定");
                return;
            }
            
            MaterialReplaceWindow window = GetWindow<MaterialReplaceWindow>();
            window.Init(materials);
            window.ShowAuxWindow();
        }

        void OnGUI()
        {
            _replaceShader = (Shader)EditorGUILayout.ObjectField("选择替换的Shader", _replaceShader, typeof(Shader), false);
            if (GUILayout.Button("替换"))
            {
                if (_replaceShader == null)
                {
                    EditorUtility.DisplayDialog("提示", "未选择Shader", "确定");
                    return;
                }
                foreach (var mat in _materials)
                {
                    if (mat.shader != _replaceShader)
                    {
                        mat.shader = _replaceShader;
                        if (mat.HasProperty("_MainTex") && mat.HasProperty("_BaseMap"))
                        {
                            mat.SetTexture("_BaseMap", mat.GetTexture("_MainTex")); 
                        }
                    }
                }
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "替换完成", "确定");
            }
        }
    }
}