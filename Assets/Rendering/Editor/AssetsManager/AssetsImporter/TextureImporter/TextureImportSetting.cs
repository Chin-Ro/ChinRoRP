using System;
using UnityEditor;
using UnityEngine;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class TextureImportSetting : BaseSetting
    {
        public TextureImporterType textureType = TextureImporterType.Default;
        public TextureImporterShape textureShape = TextureImporterShape.Texture2D;
        public bool sRGBTexture = true;
        public TextureImporterAlphaSource alphaSource = TextureImporterAlphaSource.None;
        public bool alphaIsTransparency;
        public TextureImporterGenerateCubemap generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
        
        public bool convertToNormalmap;
        public bool flipGreenChannel;
        
        public SpriteImportMode spriteImportMode = SpriteImportMode.Single;
        public float spritePixelsPerUnit = 100.0f;

        public TextureImporterNPOTScale npotScale = TextureImporterNPOTScale.None;
        public bool isReadable;
        public bool vtOnly;
        public bool mipmapEnabled;
        public bool ignoreMipmapLimit;
        public bool streamingMipmaps;
        public int streamingMipmapsPriority = 0;
        public TextureImporterMipFilter mipmapFilter = TextureImporterMipFilter.BoxFilter;
        public bool mipMapsPreserveCoverage;
        public float alphaTestReferenceValue = 0.5f;
        public bool borderMipmap = true;
        public bool fadeout;
        public int mipmapFadeDistanceStart = 2;
        public int mipmapFadeDistanceEnd = 4;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public FilterMode filterMode = FilterMode.Bilinear;
        public int anisoLevel = 0;
        
        public bool[] platformSettings = {false, false, false, true};
        public string[] platform = {"Standalone", "iOS", "Android", "WebGL"};
        public int[] maxTextureSize = {2048, 512, 512, 256};

        public TextureResizeAlgorithm[] resizeAlgorithm =
        {
            TextureResizeAlgorithm.Mitchell, TextureResizeAlgorithm.Mitchell, TextureResizeAlgorithm.Mitchell,
            TextureResizeAlgorithm.Mitchell
        };

        public TextureImporterFormat[] textureFormat =
            { TextureImporterFormat.DXT1, TextureImporterFormat.ASTC_5x5, TextureImporterFormat.ASTC_5x5, TextureImporterFormat.ASTC_5x5 };
        
        public enum TexturePreset
        {
            Mask,
            Normal,
            Ramp,
            AnimMap
        }
        public override void ImportAsset(AssetImporter importer, bool reimport = false)
        {
            var textureImporter = (UnityEditor.TextureImporter)importer;
            if(textureImporter == null) return;

            textureImporter.textureType = textureType;
            if (textureImporter.textureType == TextureImporterType.Default ||
                textureImporter.textureType == TextureImporterType.NormalMap ||
                textureImporter.textureType == TextureImporterType.SingleChannel)
            {
                textureImporter.textureShape = textureShape;
                if (textureImporter.textureShape == TextureImporterShape.TextureCube)
                {
                    textureImporter.generateCubemap = generateCubemap;
                }
            }
            else
            {
                textureImporter.textureShape = TextureImporterShape.Texture2D;
            }

            if (textureImporter.textureType == TextureImporterType.Default)
            {
                textureImporter.sRGBTexture = sRGBTexture;
            }

            if (textureImporter.textureType == TextureImporterType.Default ||
                textureImporter.textureType == TextureImporterType.SingleChannel)
            {
                textureImporter.alphaSource = alphaSource;
                textureImporter.alphaIsTransparency = alphaIsTransparency;
            }

            if (textureImporter.textureType == TextureImporterType.NormalMap)
            {
                textureImporter.convertToNormalmap = convertToNormalmap;
                textureImporter.flipGreenChannel = flipGreenChannel;
            }

            if (textureImporter.textureType == TextureImporterType.GUI)
            {
                textureImporter.textureShape = TextureImporterShape.Texture2D;
            }

            if (textureImporter.textureType == TextureImporterType.Sprite)
            {
                textureImporter.textureShape = TextureImporterShape.Texture2D;
                textureImporter.spriteImportMode = spriteImportMode;
                textureImporter.spritePixelsPerUnit = spritePixelsPerUnit;
                textureImporter.sRGBTexture = sRGBTexture;
                textureImporter.alphaIsTransparency = alphaIsTransparency;
                textureImporter.alphaSource = alphaSource;
            }

            if (textureImporter.textureType == TextureImporterType.Cursor)
            {
                textureImporter.textureShape = TextureImporterShape.Texture2D;
            }

            if (textureImporter.textureType == TextureImporterType.Cookie)
            {
                textureImporter.alphaSource = alphaSource;
                textureImporter.alphaIsTransparency = alphaIsTransparency;
            }

            if (textureImporter.textureType == TextureImporterType.Lightmap ||
                textureImporter.textureType == TextureImporterType.DirectionalLightmap ||
                textureImporter.textureType == TextureImporterType.Shadowmask)
            {
                textureImporter.textureShape = TextureImporterShape.Texture2D;
            }

            textureImporter.npotScale = npotScale;
            textureImporter.isReadable = isReadable;
            textureImporter.vtOnly = vtOnly;
            textureImporter.mipmapEnabled = mipmapEnabled;
            if (textureImporter.mipmapEnabled)
            {
                textureImporter.ignoreMipmapLimit = ignoreMipmapLimit;
                textureImporter.streamingMipmaps = streamingMipmaps;
                if (textureImporter.streamingMipmaps)
                {
                    textureImporter.streamingMipmapsPriority = streamingMipmapsPriority;
                }
                textureImporter.mipmapFilter = mipmapFilter;
                textureImporter.mipMapsPreserveCoverage = mipMapsPreserveCoverage;
                if (textureImporter.mipMapsPreserveCoverage)
                {
                    textureImporter.alphaTestReferenceValue = alphaTestReferenceValue;
                }
                textureImporter.borderMipmap = borderMipmap;
                textureImporter.fadeout = fadeout;
                if (textureImporter.fadeout)
                {
                    textureImporter.mipmapFadeDistanceStart = mipmapFadeDistanceStart;
                    textureImporter.mipmapFadeDistanceEnd = mipmapFadeDistanceEnd;
                }
            }

            textureImporter.wrapMode = wrapMode;
            textureImporter.filterMode = filterMode;
            textureImporter.anisoLevel = anisoLevel;

            for (int i = 0; i < platformSettings.Length; i++)
            {
                if (platformSettings[i])
                {
                    TexturePlatformSetting(textureImporter, platform[i], i);
                }
            }
            
            if (reimport)
            {
                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private void TexturePlatformSetting(UnityEditor.TextureImporter textureImporter, string platformName, int i)
        {
            TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(platformName);
            settings.overridden = true;
            settings.maxTextureSize = maxTextureSize[i];
            settings.resizeAlgorithm = resizeAlgorithm[i];
            settings.format = textureFormat[i];
            textureImporter.SetPlatformTextureSettings(settings);
        }

        public void ApplyTexturePreset(TexturePreset preset)
        {
            switch (preset)
            {
                case TexturePreset.Mask:
                    textureType = TextureImporterType.Default;
                    textureShape = TextureImporterShape.Texture2D;
                    sRGBTexture = false;
                    wrapMode = TextureWrapMode.Clamp;
                    break;
                
                case TexturePreset.Normal:
                    textureType = TextureImporterType.NormalMap;
                    wrapMode = TextureWrapMode.Clamp;
                    break;
                
                case TexturePreset.AnimMap:
                    textureType = TextureImporterType.Default;
                    textureShape = TextureImporterShape.Texture2D;
                    sRGBTexture = false;
                    isReadable = false;
                    wrapMode = TextureWrapMode.Clamp;
                    mipmapEnabled = false;
                    break;
                
                case TexturePreset.Ramp:
                    textureType = TextureImporterType.Default;
                    textureShape = TextureImporterShape.Texture2D;
                    sRGBTexture = true;
                    isReadable = true;
                    wrapMode = TextureWrapMode.Clamp;
                    mipmapEnabled = false;
                    break;
            }
        }
    }
}