using System;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class MaterialMonitor : BaseManager
    {
        public MaterialMonitor()
        {
            name = "Material Monitor";
            extension = ".mat";
        }

        private static MaterialMonitor _inst;

        public static MaterialMonitor Inst
        {
            get
            {
                if (_inst == null)
                {
                    AssetsManagerSettings.LoadSettings();
                    _inst = AssetsManagerSettings.Settings.managers[3] as MaterialMonitor;
                }

                return _inst;
            }
        }
        
        [OnTypeEnable]
        public static void OnEnable()
        {
            if(Inst == null || Inst.matchFolders == null) return;
            Inst.RefreshAllAssetsByExtension();
        }
        
        [CustomDraw]
        public static void DrawCustom(BaseManager manager)
        {
            MaterialMonitorDraw.Draw((MaterialMonitor)manager);
        }
    }
}