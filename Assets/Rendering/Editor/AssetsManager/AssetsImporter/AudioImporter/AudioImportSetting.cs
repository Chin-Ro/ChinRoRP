using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class AudioImportSetting : BaseSetting
    {
        public bool forceToMono = true;
        public bool ambisonic = false;
        public bool loadInBackground = false;
        public bool[] platformSettings = {false, false, false, true};
        
        public bool useAssetCheck = false;
        
        public string startCheckStrings;
        public List<string> checkStrings = new List<string>();
        public bool checkMatchFolder;

        public AudioClipLoadType[] loadType =
        {
            AudioClipLoadType.DecompressOnLoad, AudioClipLoadType.DecompressOnLoad, AudioClipLoadType.DecompressOnLoad,
            AudioClipLoadType.DecompressOnLoad
        };
        public bool[] preloadAudioData = {false, false, false, false};
        
        public AudioCompressionFormat[] compressionFormat =
        {
            AudioCompressionFormat.Vorbis, AudioCompressionFormat.Vorbis, AudioCompressionFormat.Vorbis,
            AudioCompressionFormat.AAC
        };
        public float[] quality = {1f, 1f, 1f, 1f};

        public AudioSampleRateSetting[] sampleRateSetting =
        {
            AudioSampleRateSetting.PreserveSampleRate, AudioSampleRateSetting.PreserveSampleRate,
            AudioSampleRateSetting.PreserveSampleRate, AudioSampleRateSetting.PreserveSampleRate
        };
        public int[] sampleRateOverride = {22050, 22050, 22050, 22050};
        
        public enum AudioPreset
        {
            背景音乐bgm,
            环境音频,
            声效se
        }
        
        public string[] platform = {"Standalone", "iOS", "Android", "WebGL"};
        
        public override void ImportAsset(AssetImporter importer, bool reimport = false)
        {
            var audioImporter = (UnityEditor.AudioImporter)importer;
            if(audioImporter == null) return;
            
            if (useAssetCheck)
            {
                if (AssetCheck(audioImporter, out string message))
                {
                    EditorUtility.DisplayDialog("错误", $"{audioImporter.assetPath}\n\n不符合检查规则，已被删除\n\n原因：{message}", "确定");
                    AssetDatabase.DeleteAsset(audioImporter.assetPath);
                    return;
                }
            }
            
            audioImporter.forceToMono = forceToMono;
            audioImporter.ambisonic = ambisonic;
            audioImporter.loadInBackground = loadInBackground;

            for (int i = 0; i < platformSettings.Length; i++)
            {
                if (platformSettings[i])
                {
                    AudioPlatformSetting(audioImporter, platform[i], i);
                }
            }

            if (reimport)
            {
                EditorUtility.SetDirty(audioImporter);
                audioImporter.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private bool AssetCheck(UnityEditor.AudioImporter importer, out string displayMessage)
        {
            bool checkFolderState = true;
            bool checkStartStringState = true;
            bool checkStringState = true;
            displayMessage = null;
            
            var assetPath = importer.assetPath;
            string assetName = assetPath.Substring(assetPath.LastIndexOf("/", StringComparison.Ordinal) + 1);
            string assetFolder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            
            if (checkMatchFolder)
            {
                if (assetFolder != null)
                {
                    var folders = assetFolder.Split("/");
                
                    foreach (var folder in folders)
                    {
                        var folderName = folder;
                        if (folderName.ToLower() == "Common".ToLower())
                        {
                            folderName = "com";
                        }
                        
                        if(!assetName.ToLower().Contains(folderName.ToLower()))
                        {
                            checkFolderState = false;
                        }
                        else
                        {
                            checkFolderState = true;
                            break;
                        }
                    }
                }
            }

            if (!String.IsNullOrEmpty(startCheckStrings))
            {
                checkStartStringState = assetName.StartsWith(startCheckStrings);
            }

            if (checkStrings.Count != 0)
            {
                var field = assetName.Split("_");
                
                if (checkStrings.Count > field.Length)
                {
                    checkStringState = false;
                }
                else
                {
                    for (int i = 0; i < checkStrings.Count; i++)
                    {
                        var strings = checkStrings[i].Split("|");
                        foreach (var str in strings)
                        {
                            if(!field[i + 1].Contains(str))
                            {
                                checkStringState = false;
                            }
                            else
                            {
                                checkStringState = true;
                                break;
                            }
                        }

                        if (checkStringState)
                        {
                            break;
                        }
                    }
                }
            }
            
            if (!checkFolderState || !checkStartStringState || !checkStringState)
            {
                if (!checkFolderState)
                {
                    displayMessage += "不在指定文件夹内\n";
                }

                if (!checkStartStringState)
                {
                    displayMessage += $"不符合起始字符串{startCheckStrings}\n";
                }
                
                if (!checkStringState)
                {
                    displayMessage += "不包含指定字符串\n";
                }
                
                return true;
            }
            return false;
        }

        private void AudioPlatformSetting(UnityEditor.AudioImporter audioImporter, string platformName, int i)
        {
            AudioImporterSampleSettings settings = audioImporter.GetOverrideSampleSettings(platformName);
            
            settings.loadType = loadType[i];
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.preloadAudioData = preloadAudioData[i];
            }
            
            settings.compressionFormat = compressionFormat[i];
            if (platformName == "WebGL")
            {
                settings.compressionFormat = AudioCompressionFormat.AAC;
            }
            
            settings.quality = quality[i];

            if (platformName != "WebGL")
            {
                settings.sampleRateSetting = sampleRateSetting[i];
           
                if(settings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
                {
                    settings.sampleRateOverride = (uint)sampleRateOverride[i];
                }
            }
            
            audioImporter.SetOverrideSampleSettings(platformName, settings);
        }

        public void ApplyAudioPreset(AudioPreset audioPreset)
        {
            switch (audioPreset)
            {
                case AudioPreset.背景音乐bgm:
                    forceToMono = true;
                    loadInBackground = true;
                    ambisonic = false;
                    for (int i = 0; i < platformSettings.Length; i++)
                    {
                        if (platformSettings[i])
                        {
                            loadType[i] = AudioClipLoadType.Streaming;
                            preloadAudioData[i] = false;
                            compressionFormat[i] = AudioCompressionFormat.Vorbis;
                            quality[i] = 0.7f;
                            sampleRateSetting[i] = AudioSampleRateSetting.OverrideSampleRate;
                            sampleRateOverride[i] = 22050;
                        }
                    }
                    break;
                case AudioPreset.环境音频:
                    forceToMono = false;
                    loadInBackground = true;
                    ambisonic = true;
                    for (int i = 0; i < platformSettings.Length; i++)
                    {
                        if (platformSettings[i])
                        {
                            loadType[i] = AudioClipLoadType.CompressedInMemory;
                            preloadAudioData[i] = false;
                            compressionFormat[i] = AudioCompressionFormat.Vorbis;
                            quality[i] = 0.7f;
                            sampleRateSetting[i] = AudioSampleRateSetting.OverrideSampleRate;
                            sampleRateOverride[i] = 22050;
                        }
                    }
                    break;
                case AudioPreset.声效se:
                    forceToMono = true;
                    loadInBackground = false;
                    ambisonic = false;
                    for (int i = 0; i < platformSettings.Length; i++)
                    {
                        if (platformSettings[i])
                        {
                            loadType[i] = AudioClipLoadType.DecompressOnLoad;
                            preloadAudioData[i] = false;
                            compressionFormat[i] = AudioCompressionFormat.ADPCM;
                            quality[i] = 0.7f;
                            sampleRateSetting[i] = AudioSampleRateSetting.OverrideSampleRate;
                            sampleRateOverride[i] = 22050;
                        }
                    }
                    break;
            }
        }
    }
}