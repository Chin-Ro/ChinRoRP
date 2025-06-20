using System;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class BaseSetting
    {
        public virtual void ImportAsset(AssetImporter importer, bool reimport = false)
        {
            
        }

        public virtual void PostAsset(GameObject g, string assetPath)
        {
            
        }
    }
}