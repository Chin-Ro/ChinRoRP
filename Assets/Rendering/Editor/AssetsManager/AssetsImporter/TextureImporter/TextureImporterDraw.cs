using System;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class TextureImporterDraw
    {
        private static Vector2 _scrollView;
        private static bool _bExtension;

        private static int[] _maxSize = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };

        public static void Draw(TextureImporter importer)
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
                TextureImporter.Inst.RefreshAllAssetsByExtension();
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
        
        static void DrawFolderList(TextureImporter importer)
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
                    folder.matchRules.Add(new AssetsMatchRule(new TextureImportSetting()));
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
                    rule.extension = ".jpg|.png|.tga";
                }
                rule.extension = GUILayout.TextField(rule.extension, GUILayout.ExpandWidth(false));
                GUILayout.Space(25f);
                GUILayout.Label(GetGUIContent("使用路径匹配", "Apply Path Match"), GUILayout.ExpandWidth(false));
                rule.usePathMatch = GUILayout.Toggle(rule.usePathMatch, "", GUILayout.Width(25f));
                if (rule.usePathMatch)
                {
                    GUILayout.Space(25f);
                    GUILayout.Label(GetGUIContent("匹配包含：", "Included String:"),GUILayout.ExpandWidth(false));
                    rule.pathMatchStr = GUILayout.TextField(rule.pathMatchStr, GUILayout.Width(200f));
                    
                    GUILayout.Space(25f);
                    GUILayout.Label(GetGUIContent("匹配过滤：", "Ignored String:"),GUILayout.ExpandWidth(false));
                    rule.pathIgnoreStr = GUILayout.TextField(rule.pathIgnoreStr, GUILayout.Width(200f));
                }
                
                
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5f);
                
                GUILayout.BeginVertical(Style.Box);
                DrawSettings((TextureImportSetting)rule.setting);
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
        
        static void DrawSettings(TextureImportSetting settings)
        {
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("预设：", "Preset:"), GUILayout.Width(150f));
            if(GUILayout.Button("Mask", "ToolbarButtonLeft"))
            {
                settings.ApplyTexturePreset(TextureImportSetting.TexturePreset.Mask);
            }
            if(GUILayout.Button("Normal", "ToolbarButton"))
            {
                settings.ApplyTexturePreset(TextureImportSetting.TexturePreset.Normal);
            }
            if(GUILayout.Button("Ramp", "ToolbarButton"))
            {
                settings.ApplyTexturePreset(TextureImportSetting.TexturePreset.Ramp);
            }
            if(GUILayout.Button("AnimMap", "ToolbarButtonRight"))
            {
                settings.ApplyTexturePreset(TextureImportSetting.TexturePreset.AnimMap);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            DrawTextureSettings(settings);
            
        }

        static void DrawTextureSettings(TextureImportSetting settings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("纹理类型", "Texture Type"), GUILayout.Width(160f));
            settings.textureType = (TextureImporterType)EditorGUILayout.EnumPopup(settings.textureType);
            GUILayout.EndHorizontal();

            if (settings.textureType == TextureImporterType.Default ||
                settings.textureType == TextureImporterType.NormalMap ||
                settings.textureType == TextureImporterType.SingleChannel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("纹理形状", "Texture Shape"), GUILayout.Width(160f));
                settings.textureShape = (TextureImporterShape)EditorGUILayout.EnumPopup(settings.textureShape);
                GUILayout.EndHorizontal();
                if (settings.textureShape == TextureImporterShape.TextureCube)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("CubeMap生成", "Generate Cubemap"), GUILayout.Width(160f));
                    settings.generateCubemap =
                        (TextureImporterGenerateCubemap)EditorGUILayout.EnumPopup(settings.generateCubemap);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                settings.textureShape = TextureImporterShape.Texture2D;
            }

            if (settings.textureType == TextureImporterType.Default || settings.textureType == TextureImporterType.Sprite)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("sRGB纹理", "sRGB Texture"), GUILayout.Width(160f));
                settings.sRGBTexture = GUILayout.Toggle(settings.sRGBTexture, "");
                GUILayout.EndHorizontal();
            }

            if (settings.textureType == TextureImporterType.Default ||
                settings.textureType == TextureImporterType.Cookie ||
                settings.textureType == TextureImporterType.SingleChannel ||
                settings.textureType == TextureImporterType.Sprite)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("Alpha来源", "Alpha Source"), GUILayout.Width(160f));
                settings.alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup(settings.alphaSource);
                GUILayout.EndHorizontal();

                if (settings.alphaSource != TextureImporterAlphaSource.None)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("Alpha作为透明度", "Alpha Is Transparency"), GUILayout.Width(160f));
                    settings.alphaIsTransparency = GUILayout.Toggle(settings.alphaIsTransparency, "");
                    GUILayout.EndHorizontal();
                }
            }

            if (settings.textureType == TextureImporterType.NormalMap)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("转换为法线贴图", "Convert To Normalmap"), GUILayout.Width(160f));
                settings.convertToNormalmap = GUILayout.Toggle(settings.convertToNormalmap, "");
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("翻转绿色通道", "Flip Green Channel"), GUILayout.Width(160f));
                settings.flipGreenChannel = GUILayout.Toggle(settings.flipGreenChannel, "");
                GUILayout.EndHorizontal();
            }
            
            if(settings.textureType == TextureImporterType.Sprite)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("精灵导入模式", "Sprite Import Mode"), GUILayout.Width(160f));
                settings.spriteImportMode = (SpriteImportMode)EditorGUILayout.EnumPopup(settings.spriteImportMode);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("精灵像素单位", "Sprite Pixels Per Unit"), GUILayout.Width(160f));
                settings.spritePixelsPerUnit = EditorGUILayout.FloatField(settings.spritePixelsPerUnit);
                GUILayout.EndHorizontal();
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("非二次幂贴图处理方式", "Non-Power of 2"), GUILayout.Width(160f));
            settings.npotScale = (TextureImporterNPOTScale)EditorGUILayout.EnumPopup(settings.npotScale);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("贴图可读写", "Read/Write"), GUILayout.Width(160f));
            settings.isReadable = GUILayout.Toggle(settings.isReadable, "");
            GUILayout.EndHorizontal();

            if (settings.textureType == TextureImporterType.Default)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("仅虚拟纹理", "Virtual Texture Only"), GUILayout.Width(160f));
                settings.vtOnly = GUILayout.Toggle(settings.vtOnly, "");
                GUILayout.EndHorizontal();
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("生成MipMap", "Generate MipMap"), GUILayout.Width(160f));
            settings.mipmapEnabled = GUILayout.Toggle(settings.mipmapEnabled, "");
            GUILayout.EndHorizontal();

            if (settings.mipmapEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("使用MipMap限制", "Use MipMap Limits"), GUILayout.Width(160f));
                settings.ignoreMipmapLimit = GUILayout.Toggle(!settings.ignoreMipmapLimit, "");
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("Mipmap流", "Mipmap Streaming"), GUILayout.Width(160f));
                settings.streamingMipmaps = GUILayout.Toggle(settings.streamingMipmaps, "");
                GUILayout.EndHorizontal();

                if (settings.streamingMipmaps)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("Mipmap流优先级", "Mipmaps Priority"), GUILayout.Width(160f));
                    settings.streamingMipmapsPriority = EditorGUILayout.IntField(settings.streamingMipmapsPriority);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("Mipmap过滤", "Mipmap Filter"), GUILayout.Width(160f));
                settings.mipmapFilter = (TextureImporterMipFilter)EditorGUILayout.EnumPopup(settings.mipmapFilter);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("Mipmap保持覆盖率", "MipMap Preserve Coverage"), GUILayout.Width(160f));
                settings.mipMapsPreserveCoverage = GUILayout.Toggle(settings.mipMapsPreserveCoverage, "");
                GUILayout.EndHorizontal();

                if (settings.mipMapsPreserveCoverage)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("Alpha Clip值", "Alpha Cutoff Value"), GUILayout.Width(160f));
                    settings.alphaTestReferenceValue = EditorGUILayout.FloatField(settings.alphaTestReferenceValue);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("边缘Mipmap", "Border Mipmap"), GUILayout.Width(160f));
                settings.borderMipmap = GUILayout.Toggle(settings.borderMipmap, "");
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("Mipmap消融", "Fadeout To Gray"), GUILayout.Width(160f));
                settings.fadeout = GUILayout.Toggle(settings.fadeout, "");
                GUILayout.EndHorizontal();

                if (settings.fadeout)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("Mipmap消融距离", "Mipmap Fade Distance"), GUILayout.Width(160f));
                    settings.mipmapFadeDistanceStart = EditorGUILayout.IntField(settings.mipmapFadeDistanceStart);
                    settings.mipmapFadeDistanceEnd = EditorGUILayout.IntField(settings.mipmapFadeDistanceEnd);
                    GUILayout.EndHorizontal();
                }
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("排布模式", "Warp Mode"), GUILayout.Width(160f));
            settings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(settings.wrapMode);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("过滤模式", "Filter Mode"), GUILayout.Width(160f));
            settings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(settings.filterMode);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("各项异性等级", "Aniso Level"), GUILayout.Width(160f));
            settings.anisoLevel = EditorGUILayout.IntSlider(settings.anisoLevel, 0, 16);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < settings.platformSettings.Length; i++)
            {
                GUILayout.Space(75f);
                GUILayout.BeginVertical(GUILayout.Width(250f));
                
                settings.platformSettings[i] = GUILayout.Toggle(settings.platformSettings[i], settings.platform[i], "BoldToggle");
                if (settings.platformSettings[i])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("最大尺寸", "Max Size"), GUILayout.Width(100f));
                    settings.maxTextureSize[i] = EditorGUILayout.IntPopup(settings.maxTextureSize[i], new[] {"32", "64", "128", "256", "512", "1024", "2048", "4096"}, _maxSize);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("重设置算法", "Resize Algorithm"), GUILayout.Width(100f));
                    settings.resizeAlgorithm[i] = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup(settings.resizeAlgorithm[i]);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("纹理格式", "Texture Format"), GUILayout.Width(100f));
                    settings.textureFormat[i] = (TextureImporterFormat)EditorGUILayout.EnumPopup(settings.textureFormat[i]);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        static GUIContent GetGUIContent(string chinese, string english, string tooltip = null)
        {
            return new GUIContent(TextureImporter.Inst.useChinese ? chinese : english, tooltip);
        }
    }
}