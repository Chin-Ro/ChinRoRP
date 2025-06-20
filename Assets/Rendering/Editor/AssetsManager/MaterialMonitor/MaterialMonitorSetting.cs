using System;
using System.Collections.Generic;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class MaterialMonitorSetting : BaseSetting
    {
        public List<MaterialBaseInfo> materialBaseInfos = new List<MaterialBaseInfo>();

        public MaterialMonitorSetting()
        {
            materialBaseInfos.Add(new MaterialShadersInfo());
            materialBaseInfos.Add(new MaterialsReferenceInfo());
        }
    }
}