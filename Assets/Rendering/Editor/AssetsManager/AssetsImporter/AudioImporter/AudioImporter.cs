using System;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AudioImporter : AssetsImporter
    {
        public AudioImporter()
        {
            name = "Audio Importer";
            extension = ".mp3|.flac|.wav";
            _inst = this;
        }

        private static AudioImporter _inst;

        public static AudioImporter Inst
        {
            get
            {
                if (_inst == null)
                {
                    AssetsManagerSettings.LoadSettings();
                    _inst = AssetsManagerSettings.Settings.managers[0] as AudioImporter;
                }

                return _inst;
            }
        }

        [CustomDraw]
        public static void DrawCustom(BaseManager manager)
        {
            AudioImporterDraw.Draw((AudioImporter)manager);
        }
        
        [OnTypeEnable]
        public static void OnEnable()
        {
            if(Inst == null || Inst.matchFolders == null) return;
            Inst.RefreshAllAssetsByExtension();
        }
    }
}