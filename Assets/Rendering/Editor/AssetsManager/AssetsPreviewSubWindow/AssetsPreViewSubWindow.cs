using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rendering.Editor.AssetsManager
{
    public class AssetsPreViewSubWindow : EditorWindow
    {
        private int _selectedObj = 0;
        private Object _selectedObject;
        
        private Vector2 _preViewScroll;
        private Vector2 _inspectorScroll;
        
        private readonly List<Texture> _textures = new List<Texture>();
        private readonly List<Object> _objects = new List<Object>();
        //private Texture _background;

        private void OnEnable()
        {
            //_background = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Rendering/Editor/Icons/Backgrounds/1.jpg");
        }

        public void Init(List<string> assetsPath)
        {
            _textures.Clear();
            _objects.Clear();
            foreach (var path in assetsPath)
            {
                Texture preview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(path));
                _textures.Add(preview);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                _objects.Add(asset);
            }
        }
        
        void OnGUI()
        {
            // GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _background, ScaleMode.ScaleAndCrop, true, 0f,
            //     new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            _preViewScroll = GUILayout.BeginScrollView(_preViewScroll);
            GUILayout.BeginHorizontal();
            GUILayout.Label("预览缩略图");
            
            if (GUILayout.Button("刷新", GUILayout.Width(80f)))
            {
                _textures.Clear();
                foreach (var obj in _objects)
                {
                    _textures.Add(AssetPreview.GetAssetPreview(obj));
                }
            }
            GUILayout.EndHorizontal();
            
            if (_textures != null)
            {
                _selectedObj = GUILayout.SelectionGrid(_selectedObj, _textures.ToArray(), 5);
                _selectedObject = _objects[_selectedObj];
                Selection.activeObject = _selectedObject;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll, GUILayout.ExpandWidth(false));
            
            if (_selectedObject == null) return;
            var editor = UnityEditor.Editor.CreateEditor(_selectedObject);
            editor.DrawHeader();
            EditorGUILayout.ObjectField(_selectedObject, _selectedObject.GetType(), false);
            editor.DrawDefaultInspector();
            GUILayout.EndScrollView();
            Rect previewRect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            editor.DrawPreview(previewRect);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Name: "+ _selectedObject.name);
            GUILayout.Label("Type: " + _selectedObject.GetType());
            GUILayout.Label("Path: " + AssetDatabase.GetAssetPath(_selectedObject));
        }

        public static void DrawWindow(List<string> assetsPath, string name)
        {
            if (assetsPath.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有资源", "确定");
                return;
            }
            var window = GetWindow<AssetsPreViewSubWindow>(false, name, false);
            window.Init(assetsPath);
            window.minSize = new Vector2(1100, 800);
            window.ShowAuxWindow();
        }
    }
}