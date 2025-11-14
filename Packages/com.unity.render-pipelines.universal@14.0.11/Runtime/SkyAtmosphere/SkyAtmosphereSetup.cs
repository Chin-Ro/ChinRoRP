namespace UnityEngine.Rendering.Universal
{
    struct FAtmosphereSetup
    {
        //////////////////////////////////////////////// Runtime
        
        public Vector3 PlanetCenterKm;		// In sky unit (kilometers)
        public float BottomRadiusKm;			// idem
        public float TopRadiusKm;				// idem
        
        public float MultiScatteringFactor;

        public Color RayleighScattering;// Unit is 1/km
        public float RayleighDensityExpScale;
        
        public Color MieScattering;		// Unit is 1/km
        public Color MieExtinction;		// idem
        public Color MieAbsorption;		// idem
        public float MieDensityExpScale;
        public float MiePhaseG;
        
        public Color AbsorptionExtinction;
        public float AbsorptionDensity0LayerWidth;
        public float AbsorptionDensity0ConstantTerm;
        public float AbsorptionDensity0LinearTerm;
        public float AbsorptionDensity1ConstantTerm;
        public float AbsorptionDensity1LinearTerm;
        
        public Color GroundAlbedo;
        public float TransmittanceMinLightElevationAngle;
        
        internal void ComputeViewData(Vector3 WorldCameraOrigin, Vector3 PreViewTranslation, Vector3 ViewForward, Vector3 ViewRight,
            out Vector3 SkyCameraTranslatedWorldOriginTranslatedWorld, out Vector4 SkyPlanetTranslatedWorldCenterAndViewHeight, out Matrix4x4 SkyViewLutReferential)
        {
            // The constants below should match the one in SkyAtmosphereCommon.ush
            // Always force to be 5 meters above the ground/sea level (to always see the sky and not be under the virtual planet occluding ray tracing) and lower for small planet radius
            float PlanetRadiusOffset = 0.005f;		
            
            float Offset = PlanetRadiusOffset * SkyAtmosphereUtils.SkyUnitToM;
            float BottomRadiusWorld = BottomRadiusKm * SkyAtmosphereUtils.SkyUnitToM;
            Vector3 PlanetCenterWorld = PlanetCenterKm * SkyAtmosphereUtils.SkyUnitToM;
            Vector3 PlanetCenterTranslatedWorld = PlanetCenterWorld + PreViewTranslation;
            Vector3 WorldCameraOriginTranslatedWorld = WorldCameraOrigin + PreViewTranslation;
            Vector3 PlanetCenterToCameraTranslatedWorld = WorldCameraOriginTranslatedWorld - PlanetCenterTranslatedWorld;
            float DistanceToPlanetCenterTranslatedWorld = Mathf.Sqrt(PlanetCenterToCameraTranslatedWorld.x * PlanetCenterToCameraTranslatedWorld.x +
                                                                       PlanetCenterToCameraTranslatedWorld.y * PlanetCenterToCameraTranslatedWorld.y +
                                                                       PlanetCenterToCameraTranslatedWorld.z * PlanetCenterToCameraTranslatedWorld.z);
            
            // If the camera is below the planet surface, we snap it back onto the surface.
            // This is to make sure the sky is always visible even if the camera is inside the virtual planet.
            SkyCameraTranslatedWorldOriginTranslatedWorld = DistanceToPlanetCenterTranslatedWorld < (BottomRadiusWorld + Offset) ?
                    PlanetCenterTranslatedWorld + (BottomRadiusWorld + Offset) * (PlanetCenterToCameraTranslatedWorld / DistanceToPlanetCenterTranslatedWorld) :
                    WorldCameraOriginTranslatedWorld;
            
            Vector3 Temp = (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld);
            float normalizedUp = Mathf.Sqrt(Temp.x * Temp.x + Temp.y * Temp.y + Temp.z * Temp.z);
            
            SkyPlanetTranslatedWorldCenterAndViewHeight = new Vector4(PlanetCenterTranslatedWorld.x, PlanetCenterTranslatedWorld.y, PlanetCenterTranslatedWorld.z, normalizedUp);
            
            // Now compute the referential for the SkyView LUT
            Vector3 PlanetCenterToWorldCameraPos = (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld) * SkyAtmosphereUtils.MToSkyUnit;
            Vector3 Up = PlanetCenterToWorldCameraPos.normalized;
            Vector3 Forward = ViewForward;          // This can make texel visible when the camera is rotating. Use constant world direction instead?
            //FVector3f	Left = normalize(cross(Forward, Up)); 
            Vector3	Left = Vector3.Normalize(Vector3.Cross(Forward, Up));
            float DotMainDir = Mathf.Abs(Vector3.Dot(Up, Forward));
            SkyViewLutReferential = Matrix4x4.identity;
            if (DotMainDir > 0.999f)
            {
                // When it becomes hard to generate a referential, generate it procedurally.
                // [ Duff et al. 2017, "Building an Orthonormal Basis, Revisited" ]
                float Sign = Up.y >= 0.0f ? 1.0f : -1.0f;
                float a = -1.0f / (Sign + Up.y);
                float b = Up.x * Up.z * a;
                Forward = new Vector3(Sign * b, -Sign * Up.z, 1 + Sign * a * Mathf.Pow(Up.z, 2.0f));
                Left = new Vector3(Sign + a * Mathf.Pow(Up.x, 2.0f), -Up.x, b);

                SkyViewLutReferential.SetColumn(0, -Forward);
                SkyViewLutReferential.SetColumn(1, Left);
                SkyViewLutReferential.SetColumn(2, Up);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
            else
            {
                // This is better as it should be more stable with respect to camera forward.
                Forward = Vector3.Cross(Up, Left);
                Forward.Normalize();
                SkyViewLutReferential.SetColumn(0, -Forward);
                SkyViewLutReferential.SetColumn(1, Left);
                SkyViewLutReferential.SetColumn(2, Up);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
        }

        private Vector2 GetAzimuthAndElevation(Vector3 Direction, Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ)
        {
            Vector3 NormalDir =  Direction.normalized;
            // Find projected point (on AxisX and AxisY, remove AxisZ component)
            Vector3 NoZProjDir = (NormalDir - Vector3.Dot(NormalDir, AxisZ) * AxisZ).normalized;
            // Figure out if projection is on right or left.
            float AzimuthSign = (Vector3.Dot(NoZProjDir, AxisY) < 0.0f) ? -1.0f : 1.0f;
            float ElevationSin = Vector3.Dot(NormalDir, AxisZ);
            float AzimuthCos = Vector3.Dot(NoZProjDir, AxisX);

            // Convert to Angles in Radian.
            return new Vector2(Mathf.Acos(AzimuthCos) * AzimuthSign, Mathf.Asin(ElevationSin));
        }
        
        // The following code is from SkyAtmosphere.usf and has been converted to lambda functions. 
        // It compute transmittance from the origin towards a sun direction. 

        Vector2 RayIntersectSphere(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector3 LocalPosition = RayOrigin - SphereOrigin;
            float LocalPositionSqr = Vector3.Dot(LocalPosition, LocalPosition);

            Vector3 QuadraticCoef;
            QuadraticCoef.x = Vector3.Dot(RayDirection, RayDirection);
            QuadraticCoef.y = 2.0f * Vector3.Dot(RayDirection, LocalPosition);
            QuadraticCoef.z = LocalPositionSqr - SphereRadius * SphereRadius;

            float Discriminant = QuadraticCoef.y * QuadraticCoef.y - 4.0f * QuadraticCoef.x * QuadraticCoef.z;

            // Only continue if the ray intersects the sphere
            Vector2 Intersections = new Vector2(-1.0f, -1.0f );
            if (Discriminant >= 0)
            {
                float SqrtDiscriminant = Mathf.Sqrt(Discriminant);
                Intersections.x = (-QuadraticCoef.y - 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
                Intersections.y = (-QuadraticCoef.y + 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
            }
            return Intersections;
        }
        
        // Nearest intersection of ray r,mu with sphere boundary
        float raySphereIntersectNearest(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector2 sol = RayIntersectSphere(RayOrigin, RayDirection, SphereOrigin, SphereRadius);
            float sol0 = sol.x;
            float sol1 = sol.y;
            if (sol0 < 0.0f && sol1 < 0.0f)
            {
                return -1.0f;
            }
            if (sol0 < 0.0f)
            {
                return Mathf.Max(0.0f, sol1);
            }
            else if (sol1 < 0.0f)
            {
                return Mathf.Max(0.0f, sol0);
            }
            return Mathf.Max(0.0f, Mathf.Min(sol0, sol1));
        }
        
        Color OpticalDepth(Vector3 RayOrigin, Vector3 RayDirection)
        {
            float TMax = raySphereIntersectNearest(RayOrigin, RayDirection, Vector3.zero, TopRadiusKm);

            Color OpticalDepthRGB = Color.clear;
            Vector3 VectorZero = Vector3.zero;
            if (TMax > 0.0f)
            {
                float SampleCount = 15.0f;
                float SampleStep = 1.0f / SampleCount;
                float SampleLength = SampleStep * TMax;
                for (float SampleT = 0.0f; SampleT < 1.0f; SampleT += SampleStep)
                {
                    Vector3 Pos = RayOrigin + RayDirection * (TMax * SampleT);
                    float viewHeight = (Vector3.Distance(Pos, VectorZero) - BottomRadiusKm);

                    float densityMie = Mathf.Max(0.0f, Mathf.Exp(MieDensityExpScale * viewHeight));
                    float densityRay = Mathf.Max(0.0f, Mathf.Exp(RayleighDensityExpScale * viewHeight));
                    float densityOzo = Mathf.Clamp(viewHeight < AbsorptionDensity0LayerWidth ?
                            AbsorptionDensity0LinearTerm * viewHeight + AbsorptionDensity0ConstantTerm :
                            AbsorptionDensity1LinearTerm * viewHeight + AbsorptionDensity1ConstantTerm,
                        0.0f, 1.0f);

                    Color SampleExtinction = densityMie * MieExtinction + densityRay * RayleighScattering + densityOzo * AbsorptionExtinction;
                    OpticalDepthRGB += SampleLength * SampleExtinction;
                }
            }

            return OpticalDepthRGB;
        }

        internal Color GetTransmittanceAtGroundLevel(Vector3 SunDirection)
        {
            // Assuming camera is along Z on (0,0,earthRadius + 500m)
            Vector3 WorldPos = new Vector3(0.0f, BottomRadiusKm + 0.5f, 0.0f);
            Vector2 AzimuthElevation = GetAzimuthAndElevation(SunDirection, Vector3.forward, Vector3.left, Vector3.up);
            AzimuthElevation.y = Mathf.Max(Mathf.Deg2Rad * TransmittanceMinLightElevationAngle, AzimuthElevation.y);
            Vector3 WorldDir = new Vector3(0.0f, Mathf.Sin(AzimuthElevation.y), Mathf.Cos(AzimuthElevation.y));
            Color OpticalDepthRGB = OpticalDepth(WorldPos, WorldDir);
            return new Color(Mathf.Exp(-OpticalDepthRGB.r), Mathf.Exp(-OpticalDepthRGB.g), Mathf.Exp(-OpticalDepthRGB.b), 1.0f);
        }
    }
}