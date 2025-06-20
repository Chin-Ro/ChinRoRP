using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    public class OnTypeEnableAttribute : Attribute
    {
        
    }
    public class CustomDrawAttribute : Attribute
    {
        
    }
    
    public class AssetsManagerEditorWindow : EditorWindow
    {
        static EditorWindow _window;
        static AssetsManagerSettings _settings;
        
        private BaseManager _currentSetting;
        private BaseManager _lastSetting;
        //private Texture _background;
        private bool _isInit;
        
        [MenuItem("Tools/Assets Manager", false, 0)]
        static void OpenWindow()
        {
            _window = GetWindow<AssetsManagerEditorWindow>();
            _window.titleContent = new GUIContent("资源管理工具");
            _window.minSize = new Vector2(1340, 800);
            _window.Show();
        }

        private void OnEnable()
        {
            _lastSetting = null;
            //_background = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Rendering/Editor/Icons/Backgrounds/2.jpg");
        }

        private void OnGUI()
        {
            // GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _background, ScaleMode.ScaleAndCrop, true, 0f,
            //     new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
            InitSettings();
            DrawType();
            DrawPlane();
        }

        void InitSettings()
        {
            if(_isInit && _settings != null) return;
            _isInit = true;
            _settings = AssetsManagerSettings.Settings;
            _currentSetting = _settings.managers[0];
        }
        
        void DrawType()
        {
            GUILayout.BeginHorizontal();
            foreach (var manager in _settings.managers)
            {
                if(GUILayout.Toggle(_currentSetting == manager, manager.name, "Button", GUILayout.Height(_currentSetting == manager ? 30 : 25)))
                {
                    _currentSetting = manager;
                }
            }
            GUILayout.EndHorizontal();
            
            if (_lastSetting != _currentSetting)
            {
                var method = _currentSetting.GetType().GetMethod("OnEnable",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                {
                    if (method != null && method.GetCustomAttribute<OnTypeEnableAttribute>() != null)
                        method.Invoke(null, null);
                }
                
                _lastSetting = _currentSetting;
            }
        }

        void DrawPlane()
        {
            var method = _currentSetting.GetType().GetMethod("DrawCustom",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);

            if (method != null && method.GetCustomAttribute<CustomDrawAttribute>() != null)
            {
                method.Invoke(null, new object[]{_currentSetting});
            }
        }

        private void OnDisable()
        {
            _settings?.SaveSettings();
        }

        private void OnLostFocus()
        {
            _settings?.SaveSettings();
        }
    }

    internal class Style
    {
        public static GUIStyle Box;

        static Style()
        {
            Box = new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(),
                padding = new RectOffset()
            };
        }
    } 
}
