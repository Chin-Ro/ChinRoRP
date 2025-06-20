using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Rendering.Editor.AssetsManager
{
    [Serializable]
    public class FbxImportSetting : BaseSetting
    {
        public bool useAssetCheck;
        
        public float scale = 1f;
        public bool convertUnits = true;
        public bool bakeAxisConversion;
        public bool importBlendShapes;
        public bool importDeformPercent;
        public bool importVisibility = true;
        public bool importCameras;
        public bool importLights;
        public bool preserveHierarchy;
        public bool sortHierarchyByName;
        
        public ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Medium;
        public bool isReadable;
        public MeshOptimizationFlags meshOptimizationFlags = MeshOptimizationFlags.Everything;
        public bool addCollider;

        public bool keepQuads;
        public bool weldVertices = true;
        public ModelImporterIndexFormat indexFormat = ModelImporterIndexFormat.Auto;
        
        public bool useLegacyBlendShapeNormals;
        public ModelImporterNormals normals = ModelImporterNormals.Import;
        public ModelImporterNormals blendShapeNormals = ModelImporterNormals.Calculate;
        public ModelImporterNormalCalculationMode normalsCalculateMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
        public ModelImporterNormalSmoothingSource normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
        public float normalSmoothingAngle = 60f;
        public ModelImporterTangents tangents = ModelImporterTangents.CalculateMikk;
        
        public bool swapUVChannels;
        public bool generateSecondaryUV;
        public bool generateSecondaryUVAdvanced;
        public float secondaryUVHardAngle = 88f;
        public float secondaryUVAngleDistortion = 8f;
        public float secondaryUVAreaDistortion = 15f;
        public ModelImporterSecondaryUVMarginMethod secondaryUVMarginMethod = ModelImporterSecondaryUVMarginMethod.Calculate;
        public float secondaryUVMinLightmapResolution = 40f;
        public float secondaryUVMinObjectScale = 1f;
        public float secondaryUVMargin = 4f;
        
        public bool strictVertexDataChecks;
        
        public ModelImporterAnimationType animationType = ModelImporterAnimationType.None;
        public ModelImporterGenerateAnimations  generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
        
        public ModelImporterAvatarSetup avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        public string motionNodeName;
        public ModelImporterSkinWeights skinWeights = ModelImporterSkinWeights.Standard;
        public bool optimizeBone = true;
        public bool optimizeGameObjects;
        public List<string> extraExposedTransformPaths = new List<string>();
        public bool extraExposedTransformPathsFoldout;
        
        public bool importConstraints;
        public bool importAnimation;
        public WrapMode animationWrapMode = WrapMode.Default;
        public ModelImporterAnimationCompression animationCompression = ModelImporterAnimationCompression.Optimal;
        public float animationRotationError = 0.5f;
        public float animationPositionError = 0.5f;
        public float animationScaleError = 0.5f;
        public bool importAnimatedCustomProperties;
        public bool removeConstantScaleCurves;
        
        public ModelImporterMaterialImportMode materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        public bool useSRGBMaterialColor;
        public ModelImporterMaterialLocation materialLocation = ModelImporterMaterialLocation.External;
        public ModelImporterMaterialName materialName = ModelImporterMaterialName.BasedOnTextureName;
        public ModelImporterMaterialSearch materialSearch = ModelImporterMaterialSearch.RecursiveUp;

        [FormerlySerializedAs("maxVertexCount")] public int maxTrianglesCount = 3000;
        
        public override void ImportAsset(AssetImporter importer, bool reimport = false)
        {
            var fbxImporter = importer as ModelImporter;
            if (fbxImporter == null) return;

            if (useAssetCheck)
            {
                
            }
            
            fbxImporter.globalScale = scale;
            fbxImporter.useFileUnits = convertUnits;
            fbxImporter.bakeAxisConversion = bakeAxisConversion;
            fbxImporter.importBlendShapes = importBlendShapes;
            
            if (importBlendShapes)
            {
                fbxImporter.importBlendShapeDeformPercent = importDeformPercent;
            }
            
            fbxImporter.importVisibility = importVisibility;
            fbxImporter.importCameras = importCameras;
            fbxImporter.importLights = importLights;
            fbxImporter.preserveHierarchy = preserveHierarchy;
            fbxImporter.sortHierarchyByName = sortHierarchyByName;

            fbxImporter.meshCompression = meshCompression;
            fbxImporter.isReadable = isReadable;
            fbxImporter.meshOptimizationFlags = meshOptimizationFlags;
            fbxImporter.addCollider = addCollider;

            fbxImporter.keepQuads = keepQuads;
            fbxImporter.weldVertices = weldVertices;
            fbxImporter.indexFormat = indexFormat;
            
            fbxImporter.importNormals = normals;
            fbxImporter.normalCalculationMode = normalsCalculateMode;
            
            if (!useLegacyBlendShapeNormals)
            {
                fbxImporter.importBlendShapeNormals = blendShapeNormals;
                fbxImporter.normalSmoothingSource = normalSmoothingSource;
            }

            fbxImporter.normalSmoothingAngle = normalSmoothingAngle;

            fbxImporter.importTangents = fbxImporter.importNormals == ModelImporterNormals.None ? ModelImporterTangents.None : tangents;
            
            fbxImporter.swapUVChannels = swapUVChannels;
            fbxImporter.generateSecondaryUV = generateSecondaryUV;

            if (generateSecondaryUV)
            {
                fbxImporter.secondaryUVHardAngle = secondaryUVHardAngle;
                fbxImporter.secondaryUVAngleDistortion = secondaryUVAngleDistortion;
                fbxImporter.secondaryUVAreaDistortion = secondaryUVAreaDistortion;
                fbxImporter.secondaryUVMarginMethod = secondaryUVMarginMethod;
                switch (fbxImporter.secondaryUVMarginMethod)
                {
                    case ModelImporterSecondaryUVMarginMethod.Calculate:
                        fbxImporter.secondaryUVMinLightmapResolution = secondaryUVMinLightmapResolution;
                        fbxImporter.secondaryUVMinObjectScale = secondaryUVMinObjectScale;
                        break;
                    case ModelImporterSecondaryUVMarginMethod.Manual:
                        fbxImporter.secondaryUVPackMargin = secondaryUVMargin;
                        break;
                }
            }

            fbxImporter.strictVertexDataChecks = strictVertexDataChecks;

            fbxImporter.animationType = animationType;

            if (animationType != ModelImporterAnimationType.None)
            {
                switch (animationType)
                {
                    case ModelImporterAnimationType.Legacy:
                        fbxImporter.generateAnimations = generateAnimations;
                        break;
                    case ModelImporterAnimationType.Generic:
                    case ModelImporterAnimationType.Human:
                    {
                        fbxImporter.avatarSetup = avatarSetup;

                        if(animationType == ModelImporterAnimationType.Generic && avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                        {
                            fbxImporter.motionNodeName = motionNodeName;
                        }

                        if (avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                        {
                            fbxImporter.optimizeGameObjects = optimizeGameObjects;
                            if (optimizeGameObjects)
                            {
                                fbxImporter.extraExposedTransformPaths = extraExposedTransformPaths.ToArray();
                            }
                        }

                        break;
                    }
                }

                fbxImporter.skinWeights = skinWeights;
                fbxImporter.optimizeBones = optimizeBone;
            }

            fbxImporter.importConstraints = importConstraints;
            fbxImporter.importAnimation = importAnimation;
            
            if (importAnimation)
            {
                fbxImporter.animationWrapMode = animationWrapMode;
                fbxImporter.animationCompression = animationCompression;
                fbxImporter.animationRotationError = animationRotationError;
                fbxImporter.animationPositionError = animationPositionError;
                fbxImporter.animationScaleError = animationScaleError;
                fbxImporter.importAnimatedCustomProperties = importAnimatedCustomProperties;
                fbxImporter.removeConstantScaleCurves = removeConstantScaleCurves;
            }

            fbxImporter.materialImportMode = materialImportMode;

            if (materialImportMode != ModelImporterMaterialImportMode.None)
            {
                if (materialImportMode == ModelImporterMaterialImportMode.ImportStandard)
                {
                    fbxImporter.useSRGBMaterialColor = useSRGBMaterialColor;
                }

                fbxImporter.materialLocation = materialLocation;
                fbxImporter.materialName = materialName;
                fbxImporter.materialSearch = materialSearch;
            }

            if (reimport)
            {
                EditorUtility.SetDirty(fbxImporter);
                fbxImporter.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        public override void PostAsset(GameObject g, string assetPath)
        {
            if(!useAssetCheck) return;

            List<MeshFilter> meshFilters = new List<MeshFilter>();

            MeshFilter mainMeshFilter = g.GetComponent<MeshFilter>();
            if(mainMeshFilter != null)
                meshFilters.Add(mainMeshFilter);

            for (int i = 0; i < g.transform.childCount; i++)
            {
                MeshFilter subMeshFilter = g.transform.GetChild(i).GetComponent<MeshFilter>();
                if(subMeshFilter == null)
                    continue;
                meshFilters.Add(subMeshFilter);
            }

            for (int i = 0; i < meshFilters.Count; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if(meshFilter.sharedMesh.triangles.Length / 3 <= maxTrianglesCount) continue;

                EditorUtility.DisplayDialog("提示",
                    $"模型：{g.name}\n网格：{meshFilter.sharedMesh.name}\n路径: {assetPath}\n三角面数大于 {maxTrianglesCount}，已被移除",
                    "OK");
                AssetDatabase.DeleteAsset(assetPath);
                return;
            }
        }
    }
}