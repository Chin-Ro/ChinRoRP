using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    private ScriptableRenderContext _context;
    private Camera _camera;

    private const string BufferName = "Render Camera";

    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = BufferName
    };

    private CullingResults _cullingResults;

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("ChinRoLit");

    private Lighting _lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings)
    {
        this._context = context;
        this._camera = camera;
        
        PrepareBuffer();
        //  Draw UI, before culling oprater
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        
        _buffer.BeginSample(SampleName);
        ExecuteBuffer();
        //  Lighting
        _lighting.Setup(context, _cullingResults, shadowSettings, useLightsPerObject);
        _buffer.EndSample(SampleName);
        //  Setup cameras properties, contribute view-projector matrix.
        Setup();
        //  Draw Mesh
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject);
        //  Draw Legacy Shaders
        DrawUnsupportedShaders();
        //  Draw Wireframe
        DrawGizmos();
        //  Clean temporary RT
        _lighting.CleanUp();
        //  Submit into loop
        Submit();
    }
    
    private void Setup()
    {
        _context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;
        _buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        _buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }
    
    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags =
            useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //  Draw opaque, opaque object has depth
        var sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.ReflectionProbes | 
                            PerObjectData.Lightmaps | 
                            PerObjectData.ShadowMask | 
                            PerObjectData.LightProbe | 
                            PerObjectData.OcclusionProbe | 
                            PerObjectData.LightProbeProxyVolume | 
                            PerObjectData.OcclusionProbeProxyVolume |
                            lightsPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        
        //  Draw Skybox
        _context.DrawSkybox(_camera);

        //  Draw transparent part
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        _buffer.EndSample(SampleName);
        ExecuteBuffer();
        _context.Submit();
    }

    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private bool Cull(float maxShadowDistance)
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _context.Cull(ref p);
            return true;
        }

        return false;
    }
}
