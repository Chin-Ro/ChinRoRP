using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Rendering;

public class ChinRoShaderGUI : ShaderGUI
{
    private MaterialEditor _materialEditor;
    private Object[] _materials;
    private MaterialProperty[] _materialProperty;
    
    bool Clipping {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    bool PremultiplyAlpha {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    BlendMode SrcBlend {
        set => SetProperty("_SrcBlend", (float)value);
    }

    BlendMode DstBlend {
        set => SetProperty("_DstBlend", (float)value);
    }

    bool ZWrite {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    bool HasProperty (string name) =>
        FindProperty(name, _materialProperty, false) != null;

    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");
    RenderQueue RenderQueue {
        set {
            foreach (Material m in _materials) {
                m.renderQueue = (int)value;
            }
        }
    }

    private bool _showPresets;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();
        base.OnGUI(materialEditor, properties);
        _materialEditor = materialEditor;
        _materials = materialEditor.targets;
        this._materialProperty = properties;
        BakedEmission();
        
        EditorGUILayout.Space();
        _showPresets = EditorGUILayout.Foldout(_showPresets, "Preset", true);
        if (_showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
            CopyLightMappingProperties();
        }
    }

    void CopyLightMappingProperties()
    {
        MaterialProperty mainTex = FindProperty("_MainTex", _materialProperty, false);
        MaterialProperty baseMap = FindProperty("_BaseMap", _materialProperty, false);
        if (mainTex != null && baseMap != null)
        {
            mainTex.textureValue = baseMap.textureValue;
            mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
        }

        MaterialProperty color = FindProperty("_Color", _materialProperty, false);
        MaterialProperty baseColor = FindProperty("_BaseColor", _materialProperty, false);
        if (color != null && baseColor != null)
        {
            color.colorValue = baseColor.colorValue;
        }
    }
    void BakedEmission()
    {
        EditorGUI.BeginChangeCheck();
        _materialEditor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in _materialEditor.targets)
            {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
    bool  SetProperty (string name, float value)
    {
        MaterialProperty property = FindProperty(name, _materialProperty, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    void SetProperty (string name, string keyword, bool value) {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }
    
    void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material material in _materials)
            {
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material material in _materials)
            {
                material.DisableKeyword(keyword);
            }
        }
    }
    
    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            _materialEditor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }

    //  Opaque
    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    //  Alpha Clip
    void ClipPreset () {
        if (PresetButton("Alpha Clip")) {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    
    //  Fade
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    //  Tranparent
    void TransparentPreset()
    {
        if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    enum ShadowMode
    {
        On, Clip, Dither, Off
    }

    ShadowMode Shadows
    {
        set
        {
            if (SetProperty("_Shadows", (float)value))
            {
                SetKeyword("_SHADOW_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOW_DITHER", value == ShadowMode.Dither);
            }
        }
    }

    void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadows", _materialProperty, false);
        if (shadows == null || shadows.hasMixedValue)
        {
            return;
        }

        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach (Material m in _materials)
        {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }
}