using System;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class FbxImporter : AssetsImporter
    {
        public FbxImporter()
        {
            name = "Fbx Importer";
            extension = ".fbx|.max";
            _inst = this;
        }

        private static FbxImporter _inst;

        public static FbxImporter Inst
        {
            get
            {
                if (_inst == null)
                {
                    AssetsManagerSettings.LoadSettings();
                    _inst = AssetsManagerSettings.Settings.managers[1] as FbxImporter;
                }

                return _inst;
            }
        }
        
        [CustomDraw]
        public static void DrawCustom(BaseManager manager)
        {
            FbxImporterDraw.Draw((FbxImporter)manager);
        }
        
        [OnTypeEnable]
        public static void OnEnable()
        {
            if(Inst == null || Inst.matchFolders == null) return;
            Inst.RefreshAllAssetsByExtension();
        }
    }
}