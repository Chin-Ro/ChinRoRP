//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  URP渲染数学库
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class UniversalUtils
    {
        /// <summary>Get the aspect ratio of a projection matrix.</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        internal static float ProjectionMatrixAspect(in Matrix4x4 matrix)
            => - matrix.m11 / matrix.m00;
        
        internal static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap, float aspectRatio = -1, bool isOrthographic = false)
        {
            Matrix4x4 viewSpaceRasterTransform;

            if (isOrthographic)
            {
                // For ortho cameras, project the skybox with no perspective
                // the same way as builtin does (case 1264647)
                viewSpaceRasterTransform = new Matrix4x4(
                    new Vector4(-2.0f * screenSize.z, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, -2.0f * screenSize.w, 0.0f, 0.0f),
                    new Vector4(1.0f, 1.0f, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                // Compose the view space version first.
                // V = -(X, Y, Z), s.t. Z = 1,
                // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
                // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

                aspectRatio = aspectRatio < 0 ? screenSize.x * screenSize.w : aspectRatio;
                float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);

                // Compose the matrix.
                float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
                float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

                float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
                float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

                if (renderToCubemap)
                {
                    // Flip Y.
                    m11 = -m11;
                    m21 = -m21;
                }

                viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, m11, 0.0f, 0.0f),
                    new Vector4(m20, m21, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            }

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }
        
        internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;
        
        internal static float ComputeViewportScale(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Scale by (vp_dim / buf_dim).
            return viewportSize * rcpBufferSize;
        }

        internal static float ComputeViewportLimit(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Clamp to (vp_dim - 0.5) / buf_dim.
            return (viewportSize - 0.5f) * rcpBufferSize;
        }

        internal static Vector4 ComputeViewportScaleAndLimit(Vector2Int viewportSize, Vector2Int bufferSize)
        {
            return new Vector4(ComputeViewportScale(viewportSize.x, bufferSize.x),  // Scale(x)
                ComputeViewportScale(viewportSize.y, bufferSize.y),                 // Scale(y)
                ComputeViewportLimit(viewportSize.x, bufferSize.x),                 // Limit(x)
                ComputeViewportLimit(viewportSize.y, bufferSize.y));                // Limit(y)
        }
        
        internal static float ComputZPlaneTexelSpacing(float planeDepth, float verticalFoV, float resolutionY)
        {
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            return tanHalfVertFoV * (2.0f / resolutionY) * planeDepth;
        }
        
        public static bool IsProjectionMatrixOblique(Matrix4x4 projectionMatrix)
        {
            return projectionMatrix[2] != 0 || projectionMatrix[6] != 0;
        }
        
        public static Matrix4x4 CalculateProjectionMatrix(Camera camera)
        {
            if (camera.orthographic)
            {
                var h = camera.orthographicSize;
                var w = camera.orthographicSize * camera.aspect;
                return Matrix4x4.Ortho(-w, w, -h, h, camera.nearClipPlane, camera.farClipPlane);
            }
            else
                return Matrix4x4.Perspective(camera.GetGateFittedFieldOfView(), camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        }
    }
    
    struct ZonalHarmonicsL2
    {
        public float[] coeffs; // Must have the size of 3

        public static ZonalHarmonicsL2 GetHenyeyGreensteinPhaseFunction(float anisotropy)
        {
            float g = anisotropy;

            var zh = new ZonalHarmonicsL2();
            zh.coeffs = new float[3];

            zh.coeffs[0] = 0.5f * Mathf.Sqrt(1.0f / Mathf.PI);
            zh.coeffs[1] = 0.5f * Mathf.Sqrt(3.0f / Mathf.PI) * g;
            zh.coeffs[2] = 0.5f * Mathf.Sqrt(5.0f / Mathf.PI) * g * g;

            return zh;
        }

        public static void GetCornetteShanksPhaseFunction(ZonalHarmonicsL2 zh, float anisotropy)
        {
            float g = anisotropy;

            zh.coeffs[0] = 0.282095f;
            zh.coeffs[1] = 0.293162f * g * (4.0f + (g * g)) / (2.0f + (g * g));
            zh.coeffs[2] = (0.126157f + 1.44179f * (g * g) + 0.324403f * (g * g) * (g * g)) / (2.0f + (g * g));
        }
    }
}