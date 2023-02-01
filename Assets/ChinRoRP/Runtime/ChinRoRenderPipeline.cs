using UnityEngine;
using UnityEngine.Rendering;

public partial class ChinRoRenderPipeline : RenderPipeline
{
    private CameraRender _cameraRender = new CameraRender();
    
    bool _useDynamicBatching, _useGPUInstancing, _useLightsPerObject;

    private ShadowSettings _shadowSettings;
    public ChinRoRenderPipeline (bool useDynamicBatching, bool useGPUInstancing, bool useSrpBatcher, bool useLightsPerObject, ShadowSettings shadowSettings)
    {
        this._shadowSettings = shadowSettings;
        this._useDynamicBatching = useDynamicBatching;
        this._useGPUInstancing = useGPUInstancing;
        this._useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSrpBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            _cameraRender.Render(context, camera, _useDynamicBatching, _useGPUInstancing, _useLightsPerObject, _shadowSettings);
        }
    }
}