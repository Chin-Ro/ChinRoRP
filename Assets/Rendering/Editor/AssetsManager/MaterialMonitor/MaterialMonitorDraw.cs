using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class MaterialMonitorDraw
    {
        private static Vector2 _scrollView;
        private static MaterialBaseInfo _currentInfo = new MaterialBaseInfo();
        private static MaterialBaseInfo _lastInfo = new MaterialBaseInfo();
        private static bool _init = true;
        public static void Draw(MaterialMonitor monitor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4f);
            
            if (GUILayout.Button(" + ", "ToolbarButton"))
            {
                monitor.matchFolders.Add(new AssetsMatchFolder());
            }

            if (GUILayout.Button(" - ", EditorStyles.toolbarButton))
            {
                var count = monitor.matchFolders.Count;
                if(count > 0) monitor.matchFolders.RemoveAt(count - 1);
            }

            if (GUILayout.Button(monitor.useChinese ? "Eng" : " 中 ", EditorStyles.toolbarButton))
            {
                monitor.useChinese = !monitor.useChinese;
            }
            
            if (GUILayout.Button(GetGUIContent("刷新", "Refresh"), EditorStyles.toolbarButton))
            {
                MaterialMonitor.Inst.RefreshAllAssetsByExtension();
            }

            if (GUILayout.Button(GetGUIContent("保存", "Save"), EditorStyles.toolbarButton))
            {
                AssetsManagerSettings.Settings.SaveSettings();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            _scrollView = GUILayout.BeginScrollView(_scrollView);
            DrawFolderList(monitor);
            GUILayout.EndScrollView();
        }

        static void DrawFolderList(MaterialMonitor monitor)
        {
            GUILayout.BeginVertical();
            for (int i = 0; i < monitor.matchFolders.Count; i++)
            {
                var folder = monitor.matchFolders[i];
                
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
                    folder.matchRules.Add(new AssetsMatchRule(new MaterialMonitorSetting()));
                }

                if (GUILayout.Button(GetGUIContent("删除目录", "Remove Folder"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    monitor.matchFolders.Remove(folder);
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
                    rule.extension = ".mat";
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
                DrawSettings((MaterialMonitorSetting)rule.setting, rule.matchedAssets);
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        static void DrawSettings(MaterialMonitorSetting setting, List<string> matchedAssets)
        {
            if (_init)
            {
                _currentInfo = setting.materialBaseInfos[0];
                _init = false;
            }
            
            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(5f);

            foreach (var baseInfo in setting.materialBaseInfos)
            {
                if(GUILayout.Toggle(_currentInfo == baseInfo, baseInfo.name, "ToolbarButton"))
                {
                    _currentInfo = baseInfo;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_lastInfo != _currentInfo)
            {
                var method = _currentInfo.GetType().GetMethod("OnEnable",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                {
                    if(method != null && method.GetCustomAttribute<OnTypeEnableAttribute>() != null)
                        method.Invoke(null, new object[]{matchedAssets});
                }
                _lastInfo = _currentInfo;
            }
            
            var drawMethod = _currentInfo.GetType().GetMethod("DrawCustom",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            
            if (drawMethod != null && drawMethod.GetCustomAttribute<CustomDrawAttribute>() != null)
            {
                drawMethod.Invoke(null, null);
            }
        }
        
        public static GUIContent GetGUIContent(string chinese, string english, string tooltip = null)
        {
            return new GUIContent(MaterialMonitor.Inst.useChinese ? chinese : english, tooltip);
        }
    }
}