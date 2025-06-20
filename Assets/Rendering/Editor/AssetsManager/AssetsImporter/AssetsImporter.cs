using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AssetsImporter : BaseManager
    {
        public void PreImportAsset(AssetImporter importer, string assetPath, AssetsMatchFolder folder)
        {
            var rule = GetMatchRule(folder, assetPath);
            if (rule is { setting: not null } && importer != null)
            {
                var setting = rule.setting;
                setting.ImportAsset(importer);
            }
        }

        public void PostImportAsset(GameObject g, string assetPath, AssetsMatchFolder folder)
        {
            var rule = GetMatchRule(folder, assetPath);
            if (rule is { setting: not null } && g != null)
            {
                var setting = rule.setting;
                setting.PostAsset(g, assetPath);
            }
        }

        AssetsMatchRule GetMatchRule(AssetsMatchFolder folder, string assetPath)
        {
            AssetsMatchRule tmpRule = null;
            foreach (var rule in folder.matchRules)
            {
                if(string.IsNullOrEmpty(rule.extension) || rule.extension == "*" || rule.extension == ".mat")
                {
                    continue;
                }
                
                if(Regex.IsMatch(assetPath, rule.extension))
                {
                    if (rule.usePathMatch)
                    {
                        if (!string.IsNullOrEmpty(rule.pathMatchStr) && !Regex.IsMatch(assetPath, rule.pathMatchStr))
                        {
                            continue;
                        }
                        
                        if (!string.IsNullOrEmpty(rule.pathIgnoreStr) && Regex.IsMatch(assetPath, rule.pathIgnoreStr))
                        {
                            continue;
                        }
                    }
                    tmpRule = rule;
                    break;
                }
            }
            
            return tmpRule;
        }
    }
}