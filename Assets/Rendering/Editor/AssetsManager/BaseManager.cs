using System;
using System.Collections.Generic;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class BaseManager
    {
        public string name;
        public string extension;
        public bool useChinese;
        public List<AssetsMatchFolder> matchFolders = new List<AssetsMatchFolder>();
        
        public void RefreshAllAssetsByExtension()
        {
            foreach (var folder in matchFolders)
            {
                folder.RefreshAssets(extension);
            }
        }
    }
}