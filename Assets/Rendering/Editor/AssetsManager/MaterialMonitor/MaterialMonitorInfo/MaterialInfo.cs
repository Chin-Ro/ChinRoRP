using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class MaterialInfo
    {
        public bool materialFoldout;
        public Dictionary<string, bool> KeywordsDict = new Dictionary<string, bool>();
        public List<string> keywords = new List<string>();
        public bool referenceFoldout;
        public List<GameObject> referenceObjects = new List<GameObject>();
        public Material mat;
        private MethodInfo _method;
        public MaterialInfo(Material material)
        {
            mat = material;
            var kws = material.shaderKeywords;
            var shaderKeywords = GetShaderLocalKeyWord(material.shader);
            foreach (var keyword in kws)
            {
                KeywordsDict.TryAdd(keyword, true);
                if (!shaderKeywords.Contains(keyword))
                {
                    KeywordsDict[keyword] = false;
                }
            }
        }
        
        public string[] GetShaderLocalKeyWord(Shader shader) {
            if (_method == null) {
                _method = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords",
                    BindingFlags.Static | BindingFlags.NonPublic);
            }

            if (_method != null)
            {
                var keywordList = _method.Invoke(null, new object[] { shader }) as string[];
                return keywordList;
            }

            return null;
        }
    }
}