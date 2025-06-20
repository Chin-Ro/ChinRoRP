using System;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class TextureImporter : AssetsImporter
    {
        public TextureImporter()
        {
            name = "Texture Importer";
            extension = ".png|.jpg|.jpeg|.tga|.bmp|.psd|.gif|.hdr|.exr|.tif";
            _inst = this;
        }

        private static TextureImporter _inst;

        public static TextureImporter Inst
        {
            get
            {
                if (_inst == null)
                {
                    AssetsManagerSettings.LoadSettings();
                    _inst = AssetsManagerSettings.Settings.managers[2] as TextureImporter;
                }

                return _inst;
            }
        }
        
        [CustomDraw]
        public static void DrawCustom(BaseManager manager)
        {
            TextureImporterDraw.Draw((TextureImporter)manager);
        }
        
        [OnTypeEnable]
        public static void OnEnable()
        {
            if(Inst == null || Inst.matchFolders == null) return;
            Inst.RefreshAllAssetsByExtension();
        }
    }
}