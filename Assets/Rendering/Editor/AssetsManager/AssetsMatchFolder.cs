using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AssetsMatchFolder
    {
        public string folder;
        private string _extension;
        public List<AssetsMatchRule> matchRules = new List<AssetsMatchRule>();

        public bool IsRefreshing { get; private set; } = false;
        
        public List<string> folderTotalAssets = new List<string>();
        public List<string> folderUnMatchedAssets = new List<string>();

        public void RefreshAssets(string extension)
        {
            if(IsRefreshing) return;
            IsRefreshing = true;
            _extension = extension;
            ThreadPool.QueueUserWorkItem(_RefreshAssets);
        }
        
        private void _RefreshAssets(object state)
        {
            try
            {
                var totalList = new List<string>();
                folderUnMatchedAssets.Clear();
                CollectAllAssetsByExtension(totalList);
                foreach (var rule in matchRules)
                {
                    rule.CollectAssetsByMatchRule(totalList);
                }

                folderUnMatchedAssets = totalList;
            }
            finally 
            {
                IsRefreshing = false;
            }
        }

        private void CollectAllAssetsByExtension(List<string> list)
        {
            folderTotalAssets.Clear();
            if(!Directory.Exists(folder)) return;
            var extensions = _extension.Split("|");
            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(folder, "*" + extension, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var filePath = file.Replace("\\", "/");
                    list.Add(filePath);
                    folderTotalAssets.Add(filePath);
                }
            }
        }
        
        public bool CheckCanUseImport(string assetPath)
        {
            bool isCanUse = false;

            do
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    break;
                }
                
                if(string.IsNullOrEmpty(folder))
                {
                    break;
                }

                if (!assetPath.StartsWith(folder))
                {
                    break;
                }
                
                isCanUse = CheckExtension(assetPath);
            }
            while(false);

            return isCanUse;
        }

        bool CheckExtension(string assetPath)
        {
            var ext = Path.GetExtension(assetPath).ToLower();
            foreach (var rule in matchRules)
            {
                var strings = rule.extension.Split("|");
                foreach (var str in strings)
                {
                    if(ext == str)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public void ReImportFolder()
        {
            foreach (var rule in matchRules)
            {
                rule.ReImportRule();
            }
        }
    }
}