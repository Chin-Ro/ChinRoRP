using System;
using UnityEditor;
using UnityEngine;
namespace Rendering.Editor.AssetsManager
{
    public class AudioImporterDraw
    {
        private static Vector2 _scrollView;
        private static bool _bExtension;
        public static void Draw(AudioImporter importer)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4f);
            if (GUILayout.Button(" + ", "ToolbarButton"))
            {
                importer.matchFolders.Add(new AssetsMatchFolder());
            }

            if (GUILayout.Button(" - ", EditorStyles.toolbarButton))
            {
                var count = importer.matchFolders.Count;
                if(count > 0) importer.matchFolders.RemoveAt(count - 1);
            }

            if (GUILayout.Button(importer.useChinese ? "Eng" : " 中 ", EditorStyles.toolbarButton))
            {
                importer.useChinese = !importer.useChinese;
            }
            
            if (GUILayout.Button(GetGUIContent("刷新", "Refresh"), EditorStyles.toolbarButton))
            {
                AudioImporter.Inst.RefreshAllAssetsByExtension();
            }

            if (GUILayout.Button(GetGUIContent("保存", "Save"), EditorStyles.toolbarButton))
            {
                AssetsManagerSettings.Settings.SaveSettings();
            }

            if (GUILayout.Button(GetGUIContent("重新导入所有资源", "Reimport All Assets"), EditorStyles.toolbarButton))
            {
                foreach (var folder in importer.matchFolders)
                {
                    folder.ReImportFolder();
                }
            }

            _bExtension = GUILayout.Toggle(_bExtension, GetGUIContent("配置文件夹索引后缀", "Config Extension"), EditorStyles.toolbarButton);
            if (_bExtension)
            {
                importer.extension = GUILayout.TextField(importer.extension);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            _scrollView = GUILayout.BeginScrollView(_scrollView);
            DrawFolderList(importer);
            GUILayout.EndScrollView();
        }

        static void DrawFolderList(AudioImporter importer)
        {
            GUILayout.BeginVertical();
            for (int i = 0; i < importer.matchFolders.Count; i++)
            {
                var folder = importer.matchFolders[i];
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("目录：", "Path:"), EditorStyles.largeLabel, GUILayout.Width(40f));
                folder.folder = GUILayout.TextField(folder.folder);
                if (!String.IsNullOrEmpty(folder.folder) && folder.folder.Contains("Assets"))
                {
                    folder.folder = folder.folder.Substring(folder.folder.IndexOf("Assets", StringComparison.Ordinal));
                    folder.folder = folder.folder.Replace("\\", "/");
                }
                if (GUILayout.Button(GetGUIContent("添加匹配规则", "Add Match Rule"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    folder.matchRules.Add(new AssetsMatchRule(new AudioImportSetting()));
                }

                if (GUILayout.Button(GetGUIContent("删除目录", "Remove Folder"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    importer.matchFolders.Remove(folder);
                }

                if (GUILayout.Button(GetGUIContent("重新导入目录", "Reimport Folder"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    folder.ReImportFolder();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("当前路径下被索引到的资源：", "Matched Assets Total: ") + folder.folderTotalAssets.Count.ToString());
                if(GUILayout.Button(GetGUIContent("查看", "Show"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    AssetsPreViewSubWindow.DrawWindow(folder.folderTotalAssets, folder.folder);
                }

                GUILayout.Space(25f);
                GUILayout.Label(GetGUIContent("当前路径下未使用匹配规则的资源：", "Unused Match Rule Assets: ") +
                                folder.folderUnMatchedAssets.Count.ToString());
                if(GUILayout.Button(GetGUIContent("查看", "Show"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    AssetsPreViewSubWindow.DrawWindow(folder.folderUnMatchedAssets, folder.folder);
                }

                if (folder.folderUnMatchedAssets.Count > 0)
                {
                    EditorGUILayout.HelpBox(GetGUIContent("当前文件夹下存在未使用匹配规则的资源", "Current path contains unused match rule assets"));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                DrawMatchRuleList(folder);
                
                GUILayout.EndVertical();
                GUILayout.Space(15f);
            }
            GUILayout.EndVertical();
        }
        
        static void DrawMatchRuleList(AssetsMatchFolder folder)
        {
            if(folder.matchRules.Count == 0) return;
            GUILayout.BeginVertical();
            for (int i = 0; i < folder.matchRules.Count; i++)
            {
                GUILayout.Space(5f);
                GUILayout.BeginVertical(Style.Box);
                var rule = folder.matchRules[i];
                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(rule.name, "ProfilerHeaderLabel");
                
                if (GUILayout.Button(GetGUIContent("删除匹配规则", "Remove Rule"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    folder.matchRules.Remove(rule);
                }
                
                if(GUILayout.Button(GetGUIContent("重新导入", "Reimport"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    rule.ReImportRule();
                }

                if (i > 0)
                {
                    if (GUILayout.Button("▲", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        (folder.matchRules[i], folder.matchRules[i - 1]) =
                            (folder.matchRules[i - 1], folder.matchRules[i]);
                    }
                }

                if (i < folder.matchRules.Count - 1)
                {
                    if (GUILayout.Button("▼", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        (folder.matchRules[i], folder.matchRules[i + 1]) =
                            (folder.matchRules[i + 1], folder.matchRules[i]);
                    }
                }
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();
                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(GetGUIContent("该规则命中资源：", "Matched Assets: ") + rule.matchedAssets.Count.ToString(), GUILayout.ExpandWidth(false));
                if(GUILayout.Button(GetGUIContent("查看", "Show"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    AssetsPreViewSubWindow.DrawWindow(rule.matchedAssets, rule.name);
                }
                GUILayout.Space(50f);
                
                GUILayout.Label(GetGUIContent("匹配规则描述：", "Rule Description:"), GUILayout.ExpandWidth(false));
                rule.name = GUILayout.TextField(rule.name, GUILayout.Width(85f));
                GUILayout.Space(25f);
                GUILayout.Label(GetGUIContent("当前匹配后缀：", "Matched Extension:", "使用 '|' 分隔后缀名称"), GUILayout.ExpandWidth(false));
                if (string.IsNullOrEmpty(rule.extension))
                {
                    rule.extension = ".mp3|.flac|.wav";
                }
                rule.extension = GUILayout.TextField(rule.extension, GUILayout.ExpandWidth(false));
                GUILayout.Space(25f);
                GUILayout.Label(GetGUIContent("使用路径匹配", "Apply Path Match"), GUILayout.ExpandWidth(false));
                rule.usePathMatch = GUILayout.Toggle(rule.usePathMatch, "", GUILayout.Width(25f));
                if (rule.usePathMatch)
                {
                    GUILayout.Space(25f);
                    GUILayout.Label(GetGUIContent("匹配包含：", "Included String:"),GUILayout.ExpandWidth(false));
                    rule.pathMatchStr = GUILayout.TextField(rule.pathMatchStr, GUILayout.Width(100f));
                    
                    GUILayout.Space(25f);
                    GUILayout.Label(GetGUIContent("匹配过滤：", "Ignored String:"),GUILayout.ExpandWidth(false));
                    rule.pathIgnoreStr = GUILayout.TextField(rule.pathIgnoreStr, GUILayout.Width(100f));
                }
                
                
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5f);
                
                GUILayout.BeginVertical(Style.Box);
                DrawSettings((AudioImportSetting)rule.setting);
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        static void DrawSettings(AudioImportSetting settings)
        {
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("预设：", "Preset:"), GUILayout.ExpandWidth(false));
            if(GUILayout.Button(GetGUIContent("背景音乐", "Background Music"), "ToolbarButtonLeft"))
            {
                settings.ApplyAudioPreset(AudioImportSetting.AudioPreset.背景音乐bgm);
            }
            if(GUILayout.Button(GetGUIContent("环境音频", "Environment Sound"), "ToolbarButton"))
            {
                settings.ApplyAudioPreset(AudioImportSetting.AudioPreset.环境音频);
            }
            if(GUILayout.Button(GetGUIContent("声效", "Sound Effect"), "ToolbarButtonRight"))
            {
                settings.ApplyAudioPreset(AudioImportSetting.AudioPreset.声效se);
            }
            
            GUILayout.Space(75f);
            GUILayout.Label(GetGUIContent("资源检查", "Assets Check"), GUILayout.ExpandWidth(false));
            settings.useAssetCheck = GUILayout.Toggle(settings.useAssetCheck, "", GUILayout.ExpandWidth(false));
            if (settings.useAssetCheck)
            {
                GUILayout.Space(50f);
                GUILayout.Label(GetGUIContent("检查文件夹路径", "Check Folder Path", "检查资源必须存放于同名文件夹下"));
                settings.checkMatchFolder = GUILayout.Toggle(settings.checkMatchFolder, "");
                GUILayout.Space(50f);
                GUILayout.Label(GetGUIContent("强制匹配起始字段：", "Force Match Start String:"));
                settings.startCheckStrings = GUILayout.TextField(settings.startCheckStrings, GUILayout.Width(85f));
                GUILayout.Space(50f);
                GUILayout.Label(GetGUIContent("匹配字符字段：", "Match Strings:"));
                if (GUILayout.Button("+", "ToolbarButton"))
                {
                    settings.checkStrings.Add("");
                }

                if (GUILayout.Button(" -", "ToolbarButton"))
                {
                    if (settings.checkStrings.Count > 0)
                    {
                        settings.checkStrings.RemoveAt(settings.checkStrings.Count - 1);
                    }
                }
                if (settings.checkStrings.Count > 0)
                {
                    for (int i = 0; i < settings.checkStrings.Count; i++)
                    {
                        settings.checkStrings[i] = GUILayout.TextField(settings.checkStrings[i], GUILayout.Width(85f));
                    }
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(200f));
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("单声道", "Force To Mono"), GUILayout.Width(140));
            settings.forceToMono = GUILayout.Toggle(settings.forceToMono, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("后台加载","Load In Background"), GUILayout.Width(140));
            settings.loadInBackground = GUILayout.Toggle(settings.loadInBackground, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("环绕声","Ambisonic"), GUILayout.Width(140));
            settings.ambisonic = GUILayout.Toggle(settings.ambisonic, "");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            GUILayout.BeginHorizontal();
            for (int i = 0; i < settings.platformSettings.Length; i++)
            {
                GUILayout.Space(75f);
                GUILayout.BeginVertical(GUILayout.Width(275f));
                
                settings.platformSettings[i] = GUILayout.Toggle(settings.platformSettings[i], settings.platform[i], "BoldToggle");
                
                if (settings.platformSettings[i])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("加载类型", "Load Type"), GUILayout.Width(120f));
                    settings.loadType[i] = (AudioClipLoadType)EditorGUILayout.EnumPopup(settings.loadType[i]);
                    GUILayout.EndHorizontal();
                    if (settings.loadType[i] != AudioClipLoadType.Streaming)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("预加载音频数据","Preload Audio Data"), GUILayout.Width(120f));
                        settings.preloadAudioData[i] = GUILayout.Toggle(settings.preloadAudioData[i], "");
                        GUILayout.EndHorizontal();
                    }
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("压缩格式", "Compression Format"), GUILayout.Width(120f));
                    settings.compressionFormat[i] = (AudioCompressionFormat)EditorGUILayout.EnumPopup(settings.compressionFormat[i]);
                    if(settings.platform[i] == "WebGL")
                    {
                        settings.compressionFormat[i] = AudioCompressionFormat.AAC;
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("质量：", "Quality:") + ((int)(settings.quality[i] * 100)).ToString(), GUILayout.Width(120f));
                    settings.quality[i] = GUILayout.HorizontalSlider(settings.quality[i], 0f, 1f);
                    GUILayout.EndHorizontal();
                    
                    if(settings.platform[i] != "WebGL")
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("采样率设置", "Sample Rate Setting"), GUILayout.Width(120f));
                        settings.sampleRateSetting[i] = (AudioSampleRateSetting)EditorGUILayout.EnumPopup(settings.sampleRateSetting[i]);
                        GUILayout.EndHorizontal();
                    
                        if(settings.sampleRateSetting[i] == AudioSampleRateSetting.OverrideSampleRate)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(GetGUIContent("采样率：", "Sample Rate:"), GUILayout.Width(120f));
                            settings.sampleRateOverride[i] = EditorGUILayout.IntField(settings.sampleRateOverride[i]);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static GUIContent GetGUIContent(string chinese, string english, string tooltip = null)
        {
            return new GUIContent(AudioImporter.Inst.useChinese ? chinese : english, tooltip);
        }

        static GUILayoutOption GetGUIWidth()
        {
            return GUILayout.Width(AudioImporter.Inst.useChinese ? 90f : 120f);
        }
    }
}