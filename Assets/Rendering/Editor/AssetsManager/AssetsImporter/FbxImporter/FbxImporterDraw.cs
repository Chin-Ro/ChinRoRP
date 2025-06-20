using System;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class FbxImporterDraw
    {
        private static Vector2 _scrollView;
        private static bool _bExtension;
        public static void Draw(FbxImporter importer)
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
                FbxImporter.Inst.RefreshAllAssetsByExtension();
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

        static void DrawFolderList(FbxImporter importer)
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
                    folder.matchRules.Add(new AssetsMatchRule(new FbxImportSetting()));
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
                    rule.extension = ".fbx|.FBX|.max";
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
                DrawSettings((FbxImportSetting)rule.setting);
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        static void DrawSettings(FbxImportSetting settings)
        {
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("资源检查：", "Asset Check:"), GUILayout.ExpandWidth(false));
            settings.useAssetCheck = GUILayout.Toggle(settings.useAssetCheck, "", GUILayout.Width(25f));
            
            GUILayout.Space(75f);
            if (settings.useAssetCheck)
            {
                GUILayout.Label(GetGUIContent("最大三角面数：", "Max Triangles Count:"), GUILayout.ExpandWidth(false));
                settings.maxTrianglesCount = EditorGUILayout.IntField(settings.maxTrianglesCount, GUILayout.Width(100f));
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(5f);
            DrawModel(settings);
            GUILayout.Space(180);
            DrawAnimationRig(settings);
            GUILayout.Space(180);
            DrawMaterials(settings);
            
            GUILayout.EndHorizontal();
        }

        private static void DrawModel(FbxImportSetting settings)
        {
            GUILayout.BeginVertical(GUILayout.Width(400));
            GUILayout.Label("Model", "BoldLabel");
            
            GUILayout.BeginVertical();
            GUILayout.Label("Scene");
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("全局缩放", "Scale Factor"), GUILayout.Width(185));
            settings.scale = EditorGUILayout.FloatField(settings.scale, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("转换单位", "Convert Units"), GUILayout.Width(185));
            settings.convertUnits = GUILayout.Toggle(settings.convertUnits, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("转换烘焙轴", "Bake Axis Conversion"), GUILayout.Width(185));
            settings.bakeAxisConversion = GUILayout.Toggle(settings.bakeAxisConversion, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入变形", "Import BlendShapes"), GUILayout.Width(185));
            settings.importBlendShapes = GUILayout.Toggle(settings.importBlendShapes, "");
            GUILayout.EndHorizontal();

            if (settings.importBlendShapes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("导入变形百分比", "Import Deform Percent"), GUILayout.Width(185));
                settings.importDeformPercent = GUILayout.Toggle(settings.importDeformPercent, "");
                GUILayout.EndHorizontal();
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入可见性", "Import Visibility"), GUILayout.Width(185));
            settings.importVisibility = GUILayout.Toggle(settings.importVisibility, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入相机", "Import Cameras"), GUILayout.Width(185));
            settings.importCameras = GUILayout.Toggle(settings.importCameras, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入灯光", "Import Lights"), GUILayout.Width(185));
            settings.importLights = GUILayout.Toggle(settings.importLights, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("保留层级", "Preserve Hierarchy"), GUILayout.Width(185));
            settings.preserveHierarchy = GUILayout.Toggle(settings.preserveHierarchy, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("按名称排序层级", "Sort Hierarchy By Name"), GUILayout.Width(185));
            settings.sortHierarchyByName = GUILayout.Toggle(settings.sortHierarchyByName, "");
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Meshes", "BoldLabel");
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("网格压缩", "Mesh Compression"), GUILayout.Width(185));
            settings.meshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup(settings.meshCompression);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("可读可写", "Read/Write"), GUILayout.Width(185));
            settings.isReadable = GUILayout.Toggle(settings.isReadable, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("网格优化设置", "Mesh Optimization Flags"), GUILayout.Width(185));
            settings.meshOptimizationFlags = (MeshOptimizationFlags)EditorGUILayout.EnumFlagsField(settings.meshOptimizationFlags);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("添加碰撞体", "Add Collider"), GUILayout.Width(185));
            settings.addCollider = GUILayout.Toggle(settings.addCollider, "");
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Geometry", "BoldLabel");
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("保留四边形", "Keep Quads"), GUILayout.Width(185));
            settings.keepQuads = GUILayout.Toggle(settings.keepQuads, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("合并顶点", "Weld Vertices"), GUILayout.Width(185));
            settings.weldVertices = GUILayout.Toggle(settings.weldVertices, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("索引格式", "Index Format"), GUILayout.Width(185));
            settings.indexFormat = (ModelImporterIndexFormat)EditorGUILayout.EnumPopup(settings.indexFormat);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("旧式变形器法线", "Legacy Blend Shape Normals"), GUILayout.Width(185));
            settings.useLegacyBlendShapeNormals = GUILayout.Toggle(settings.useLegacyBlendShapeNormals, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("法线", "Normals"), GUILayout.Width(185));
            settings.normals = (ModelImporterNormals)EditorGUILayout.EnumPopup(settings.normals);
            GUILayout.EndHorizontal();

            if (settings.normals != ModelImporterNormals.None)
            {
                if (settings.importBlendShapes && !settings.useLegacyBlendShapeNormals)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("变形器法线", "Blend Shape Normals"), GUILayout.Width(185));
                    settings.blendShapeNormals = (ModelImporterNormals)EditorGUILayout.EnumPopup(settings.blendShapeNormals);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("法线模式", "Normals Mode"), GUILayout.Width(185));
                settings.normalsCalculateMode = (ModelImporterNormalCalculationMode)EditorGUILayout.EnumPopup(settings.normalsCalculateMode);
                GUILayout.EndHorizontal();

                if (!settings.useLegacyBlendShapeNormals)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("光滑来源", "Smoothing Source"), GUILayout.Width(185));
                    settings.normalSmoothingSource = (ModelImporterNormalSmoothingSource)EditorGUILayout.EnumPopup(settings.normalSmoothingSource);
                    GUILayout.EndHorizontal();
                }
            
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("平滑角度", "Smoothing Angle"), GUILayout.Width(185));
                settings.normalSmoothingAngle = EditorGUILayout.Slider(settings.normalSmoothingAngle, 0f, 180f);
                GUILayout.EndHorizontal();
            
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("切线", "Tangents"), GUILayout.Width(185));
                settings.tangents =  (ModelImporterTangents)EditorGUILayout.EnumPopup(settings.tangents);
                GUILayout.EndHorizontal();
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("交换UV通道", "Swap UVs"), GUILayout.Width(185));
            settings.swapUVChannels = GUILayout.Toggle(settings.swapUVChannels, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("生成光照UV", "Generate Lightmap UV"), GUILayout.Width(185));
            settings.generateSecondaryUV = GUILayout.Toggle(settings.generateSecondaryUV, "");
            GUILayout.EndHorizontal();

            if (settings.generateSecondaryUV)
            {
                settings.generateSecondaryUVAdvanced = EditorGUILayout.Foldout(settings.generateSecondaryUVAdvanced, GetGUIContent("光照贴图UV设置", "Lightmap UVs settings"));
                if (settings.generateSecondaryUVAdvanced)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("硬角度", "Hard Angle"), GUILayout.Width(185));
                    settings.secondaryUVHardAngle = EditorGUILayout.Slider(settings.secondaryUVHardAngle, 0f, 180f);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("角度失真", "Angle Error"), GUILayout.Width(185));
                    settings.secondaryUVAngleDistortion = EditorGUILayout.Slider(settings.secondaryUVAngleDistortion, 1f, 75);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("区域失真", "Area Error"), GUILayout.Width(185));
                    settings.secondaryUVAreaDistortion = EditorGUILayout.Slider(settings.secondaryUVAreaDistortion, 1f, 75);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("边界模式", "Margin Method"), GUILayout.Width(185));
                    settings.secondaryUVMarginMethod = (ModelImporterSecondaryUVMarginMethod)EditorGUILayout.EnumPopup(settings.secondaryUVMarginMethod);
                    GUILayout.EndHorizontal();

                    if (settings.secondaryUVMarginMethod == ModelImporterSecondaryUVMarginMethod.Calculate)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("最小光照贴图分辨率", "Min Lightmap Resolution"), GUILayout.Width(185));
                        settings.secondaryUVMinLightmapResolution = EditorGUILayout.FloatField(settings.secondaryUVMinLightmapResolution);
                        GUILayout.EndHorizontal();
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("最小对象缩放", "Min Object Scale"), GUILayout.Width(185));
                        settings.secondaryUVMinObjectScale = EditorGUILayout.FloatField(settings.secondaryUVMinObjectScale);
                        GUILayout.EndHorizontal();
                    }
                    else if(settings.secondaryUVMarginMethod == ModelImporterSecondaryUVMarginMethod.Manual)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("包裹边界", "Pack Margin"), GUILayout.Width(185));
                        settings.secondaryUVMargin = EditorGUILayout.Slider(settings.secondaryUVMargin, 1f, 64f);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("严格顶点数据检查", "Strict Vertex Data Checks"), GUILayout.Width(185));
            settings.strictVertexDataChecks = GUILayout.Toggle(settings.strictVertexDataChecks, "");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        private static void DrawAnimationRig(FbxImportSetting settings)
        {
            GUILayout.BeginVertical(GUILayout.Width(400));
            GUILayout.Label("Rig", "BoldLabel");
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("动画类型", "Animation Type"), GUILayout.Width(185));
            settings.animationType = (ModelImporterAnimationType)EditorGUILayout.EnumPopup(settings.animationType);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10f);

            if (settings.animationType != ModelImporterAnimationType.None)
            {
                if (settings.animationType == ModelImporterAnimationType.Legacy)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("生成模式", "Generation"), GUILayout.Width(185));
                    settings.generateAnimations =
                        (ModelImporterGenerateAnimations)EditorGUILayout.EnumPopup(settings.generateAnimations);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("Avatar设置", "Avatar Setup"), GUILayout.Width(185));
                    settings.avatarSetup = (ModelImporterAvatarSetup)EditorGUILayout.EnumPopup(settings.avatarSetup);
                    if (settings.animationType == ModelImporterAnimationType.Human)
                    {
                        if (settings.avatarSetup == ModelImporterAvatarSetup.NoAvatar)
                        {
                            settings.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (settings.avatarSetup != ModelImporterAvatarSetup.NoAvatar)
                    {
                        if (settings.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel &&
                            settings.animationType == ModelImporterAnimationType.Generic)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(GetGUIContent("动作节点名称", "Node Name"), GUILayout.Width(185));
                            settings.motionNodeName = GUILayout.TextField(settings.motionNodeName);
                            GUILayout.EndHorizontal();
                        }
                    }
                    
                    if (settings.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("优化游戏对象", "Optimize Game Objects"), GUILayout.Width(185));
                        settings.optimizeGameObjects = GUILayout.Toggle(settings.optimizeGameObjects, "");
                        GUILayout.EndHorizontal();
                        
                        if (settings.optimizeGameObjects)
                        {
                            settings.extraExposedTransformPathsFoldout = EditorGUILayout.Foldout(
                                settings.extraExposedTransformPathsFoldout,
                                GetGUIContent("显示节点", "Extra Exposed Transform Paths"));
                            if (settings.extraExposedTransformPathsFoldout)
                            {
                                GUILayout.BeginHorizontal();
                                if(GUILayout.Button(" + ", "ToolbarButton"))
                                {
                                    settings.extraExposedTransformPaths.Add("");
                                }
                                if(GUILayout.Button(" - ", "ToolbarButton"))
                                {
                                    if (settings.extraExposedTransformPaths.Count > 0)
                                    {
                                        settings.extraExposedTransformPaths.RemoveAt(settings.extraExposedTransformPaths.Count - 1);
                                    }
                                }
                                GUILayout.EndHorizontal();

                                for (int i = 0; i < settings.extraExposedTransformPaths.Count; i++)
                                {
                                    settings.extraExposedTransformPaths[i] = GUILayout.TextField(settings.extraExposedTransformPaths[i]);
                                }
                            }
                        }
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("皮肤权重", "Skin Weights"), GUILayout.Width(185));
                settings.skinWeights = (ModelImporterSkinWeights)EditorGUILayout.EnumPopup(settings.skinWeights);
                GUILayout.EndHorizontal();
                    
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("优化骨骼", "Strip Bone"), GUILayout.Width(185));
                settings.optimizeBone = GUILayout.Toggle(settings.optimizeBone, "");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            
            
            GUILayout.Space(50f);
            GUILayout.Label("Animation","BoldLabel");
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入约束", "Import Constraints"), GUILayout.Width(185));
            settings.importConstraints = GUILayout.Toggle(settings.importConstraints, "");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("导入动画", "Import Animation"), GUILayout.Width(185));
            settings.importAnimation = GUILayout.Toggle(settings.importAnimation, "");
            GUILayout.EndHorizontal();

            if (settings.importAnimation)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("动画播放模式", "Wrap Mode"), GUILayout.Width(185));
                settings.animationWrapMode = (WrapMode)EditorGUILayout.EnumPopup(settings.animationWrapMode);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("动画压缩", "Animation Compression"), GUILayout.Width(185));
                settings.animationCompression = (ModelImporterAnimationCompression)EditorGUILayout.EnumPopup(settings.animationCompression);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("旋转误差", "Rotation Error"), GUILayout.Width(185));
                settings.animationRotationError = EditorGUILayout.FloatField(settings.animationRotationError);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("位置误差", "Position Error"), GUILayout.Width(185));
                settings.animationPositionError = EditorGUILayout.FloatField(settings.animationPositionError);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("缩放误差", "Scale Error"), GUILayout.Width(185));
                settings.animationScaleError = EditorGUILayout.FloatField(settings.animationScaleError);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("导入动画自定义属性", "Animated Custom Properties"), GUILayout.Width(185));
                settings.importAnimatedCustomProperties = GUILayout.Toggle(settings.importAnimatedCustomProperties, "");
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("移除常量缩放曲线", "Remove Constant Scale Curves"), GUILayout.Width(185));
                settings.removeConstantScaleCurves = GUILayout.Toggle(settings.removeConstantScaleCurves, "");
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        private static void DrawMaterials(FbxImportSetting settings)
        {
            GUILayout.BeginVertical(GUILayout.Width(400));
            GUILayout.Label("Materials", "BoldLabel");
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetGUIContent("材质导入模式", "Material Import Mode"), GUILayout.Width(185));
            settings.materialImportMode = (ModelImporterMaterialImportMode)EditorGUILayout.EnumPopup(settings.materialImportMode);
            GUILayout.EndHorizontal();

            if (settings.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                if (settings.materialImportMode == ModelImporterMaterialImportMode.ImportStandard)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("使用sRGB材质颜色", "Use SRGB Material Color"), GUILayout.Width(185));
                    settings.useSRGBMaterialColor = GUILayout.Toggle(settings.useSRGBMaterialColor, "");
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(GetGUIContent("材质存放路径", "Location"), GUILayout.Width(185));
                settings.materialLocation =
                    (ModelImporterMaterialLocation)EditorGUILayout.EnumPopup(settings.materialLocation);
                GUILayout.EndHorizontal();

                if (settings.materialLocation == ModelImporterMaterialLocation.External)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetGUIContent("材质命名", "Naming"), GUILayout.Width(185));
                    settings.materialName = (ModelImporterMaterialName)EditorGUILayout.EnumPopup(settings.materialName);
                    GUILayout.EndHorizontal();
                    
                    if (settings.materialName == ModelImporterMaterialName.BasedOnTextureName)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(GetGUIContent("搜索路径", "Search"), GUILayout.Width(185));
                        settings.materialSearch = (ModelImporterMaterialSearch)EditorGUILayout.EnumPopup(settings.materialSearch);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        static GUIContent GetGUIContent(string chinese, string english, string tooltip = null)
        {
            return new GUIContent(FbxImporter.Inst.useChinese ? chinese : english, tooltip);
        }
    }
}