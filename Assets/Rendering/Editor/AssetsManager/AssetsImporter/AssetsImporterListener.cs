using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class AssetsImporterListener : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            ImportAsset();
        }

        private void OnPreprocessTexture()
        {
            ImportAsset();
        }

        private void OnPreprocessModel()
        {
            ImportAsset();
        }

        private void OnPostprocessModel(GameObject g)
        {
            PostAsset(g);
        }

        private void OnPreprocessMaterialDescription(MaterialDescription description, Material material,
            AnimationClip[] animations)
        {
            material.shader = Shader.Find("Aurogon/Lit");
        }

        void ImportAsset()
        {
            var setting = AssetsManagerSettings.Settings;

            foreach (var manager in setting.managers)
            {
                var info = manager as AssetsImporter;
                if(info == null) continue;
                if (info.matchFolders == null || info.matchFolders.Count == 0) continue;
                foreach (var folder in info.matchFolders)
                {
                    if (folder.CheckCanUseImport(assetPath))
                    {
                        info.PreImportAsset(assetImporter, assetPath, folder);
                    }
                }
            }
        }

        void PostAsset(GameObject g)
        {
            var setting = AssetsManagerSettings.Settings;
            foreach (var manager in setting.managers)
            {
                var info = manager as AssetsImporter;
                if(info == null) continue;
                if (info.GetType() == typeof(FbxImporter))
                {
                    if (info.matchFolders == null || info.matchFolders.Count == 0) continue;
                    foreach (var folder in info.matchFolders)
                    {
                        if (folder.CheckCanUseImport(assetPath))
                        {
                            info.PostImportAsset(g, assetPath, folder);
                        }
                    }
                    
                }
            }
        }
    }
}