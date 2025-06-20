using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using File = System.IO.File;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AssetsManagerSettings
    {
        public List<BaseManager> managers = new List<BaseManager>();
        public static readonly string SettingsPath = "Assets/Rendering/Editor/AssetsManager/AssetsManagerSettings.json";

        private static AssetsManagerSettings _settings;
        public static AssetsManagerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = LoadSettings();
                }
                
                return _settings;
            }
        }

        public static AssetsManagerSettings LoadSettings()
        {
            AssetsManagerSettings assetsManagerSettings = new AssetsManagerSettings()
            {
                managers = new List<BaseManager>()
            };
            
            if (File.Exists(SettingsPath))
            {
                var bytes = File.ReadAllBytes(SettingsPath);
                assetsManagerSettings.managers = (List<BaseManager>)DeserializeObject(bytes);
            }

            if (assetsManagerSettings.managers.Count == 0)
            {
                assetsManagerSettings.managers.Add(new AudioImporter());
                assetsManagerSettings.managers.Add(new FbxImporter());
                assetsManagerSettings.managers.Add(new TextureImporter());
                assetsManagerSettings.managers.Add(new MaterialMonitor());
            }
            
            return assetsManagerSettings;
        }
        
        public void SaveSettings()
        {
            var bytes = SerializeObject(managers);
            File.WriteAllBytes(SettingsPath, bytes);
        }
        
        /// <summary>
        /// 内存流存储，十分好用！！！
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeObject(object obj)
        {
            if (obj == null) return null;

            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            byte[] bytes = ms.GetBuffer();
            ms.Close();
            return bytes;
        }
        
        public static object DeserializeObject(byte[] bytes)
        {
            if (bytes == null) return null;

            MemoryStream ms = new MemoryStream(bytes);
            BinaryFormatter formatter = new BinaryFormatter();
            object obj = formatter.Deserialize(ms);
            ms.Close();
            return obj;
        }
    }
}