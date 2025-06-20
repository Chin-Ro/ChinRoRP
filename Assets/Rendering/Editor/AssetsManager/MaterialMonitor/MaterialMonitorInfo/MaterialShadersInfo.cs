using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class MaterialShadersInfo : MaterialBaseInfo
    {
        private static Dictionary<Shader, bool> _shaderFoldout = new Dictionary<Shader, bool>();
        static Dictionary<Shader, List<MaterialInfo>> _shaderDict = new Dictionary<Shader, List<MaterialInfo>>();
        public MaterialShadersInfo()
        {
            name = "Material Shaders";
        }

        [OnTypeEnable]
        public static void OnEnable(List<string> matchedAssets)
        {
            _shaderDict.Clear();
            _shaderFoldout.Clear();
            foreach (var path in matchedAssets)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material == null) continue;
                Shader shader = material.shader;
                if (!_shaderDict.ContainsKey(shader))
                {
                    _shaderDict[shader] = new List<MaterialInfo> {};
                    _shaderFoldout[shader] = false;
                }

                _shaderDict[shader].Add(new MaterialInfo(material));
            }
        }

        [CustomDraw]
        public static void DrawCustom()
        {
            foreach (var shader in _shaderDict.Keys)
            {
                GUILayout.BeginHorizontal();
                _shaderFoldout[shader] = EditorGUILayout.Foldout(_shaderFoldout[shader], shader.name + $" ({_shaderDict[shader].Count})", EditorStyles.foldoutHeader);
                EditorGUILayout.ObjectField(shader, typeof(Shader), false);
                if (GUILayout.Button(MaterialMonitorDraw.GetGUIContent("替换", "Replace"), GUILayout.Width(55f)))
                {
                    List<Material> materials = new List<Material>();
                    foreach (var materialInfo in _shaderDict[shader])
                    {
                        materials.Add(materialInfo.mat);
                    }
                    MaterialReplaceWindow.DrawWindow(materials);
                }
                GUILayout.EndHorizontal();
                if (_shaderFoldout[shader])
                {
                    GUILayout.Space(5f);
                    foreach (var materialInfo in _shaderDict[shader])
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(materialInfo.mat, typeof(Material), false, GUILayout.Width(300f));
                        GUILayout.Space(5f);

                        for (int i = 0; i < materialInfo.KeywordsDict.Keys.Count; i++)
                        {
                            var keyword = materialInfo.KeywordsDict.Keys.ElementAt(i);
                            if (GUILayout.Button(" - ", "ToolbarButton", GUILayout.ExpandWidth(false)))
                            {
                                materialInfo.mat.DisableKeyword(keyword);
                                materialInfo.KeywordsDict.Remove(keyword);
                                AssetDatabase.Refresh();
                                continue;
                            }
                            if (!materialInfo.KeywordsDict[keyword])
                            {
                                EditorGUILayout.HelpBox(MaterialMonitorDraw.GetGUIContent($"此关键字不属于 {shader}", $"The Keyword is not owned by {shader.name}"));
                            }
                            GUILayout.Label(keyword, GUILayout.Width(200f));
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(1f);
                    }
                }
            }
            GUILayout.Space(5f);
        }
    }
}