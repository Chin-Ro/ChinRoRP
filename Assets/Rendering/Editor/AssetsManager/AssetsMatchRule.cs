using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AssetsMatchRule
    {
        public string name;
        public string extension;
        public bool usePathMatch;
        public string pathMatchStr;
        public string pathIgnoreStr;
        public List<string> matchedAssets = new List<string>();
        public BaseSetting setting;

        public AssetsMatchRule(BaseSetting baseSetting)
        {
            setting = baseSetting;
        }

        public void CollectAssetsByMatchRule(List<string> folderAssets)
        {
            matchedAssets.Clear();
            if(extension == null)
                return;
            foreach (var asset in folderAssets)
            {
                var fileName = Path.GetFileName(asset);
                if (Regex.IsMatch(fileName, extension))
                { 
                    if (usePathMatch)
                    {
                        if (!string.IsNullOrEmpty(pathMatchStr) && !Regex.IsMatch(fileName, pathMatchStr))
                        {
                            continue;
                        }
                        
                        if (!string.IsNullOrEmpty(pathIgnoreStr) && Regex.IsMatch(fileName, pathIgnoreStr))
                        {
                            continue;
                        }
                        matchedAssets.Add(asset);
                    }
                    else
                    {
                        matchedAssets.Add(asset);
                    }
                }
            }

            foreach (var matchedAsset in matchedAssets)
            {
                folderAssets.Remove(matchedAsset);
            }
        }

        public void ReImportRule()
        {
            for(int i = 0; i < matchedAssets.Count; i++)
            {
                var assetPath = matchedAssets[i];
                var importer = AssetImporter.GetAtPath(assetPath);
                EditorUtility.DisplayProgressBar($"正在重新导入资源({i + 1}/{matchedAssets.Count})",
                    assetPath,
                    (i + 1f) / matchedAssets.Count);
                setting.ImportAsset(importer, true);
            }
            EditorUtility.ClearProgressBar();
        }
    }
}