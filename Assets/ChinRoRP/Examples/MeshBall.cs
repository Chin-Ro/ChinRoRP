using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour {

    static int _baseColorId = Shader.PropertyToID("_BaseColor"),
        _metallicId = Shader.PropertyToID("_Metallic"),
        _smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    [SerializeField] private LightProbeProxyVolume lightProbeProxyVolume = null;

    private Matrix4x4[] _matrices = new Matrix4x4[1023];
    private Vector4[] _baseColors = new Vector4[1023];

    private float[]
        _metallic = new float[1023],
        _smoothness = new float[1023];

    private MaterialPropertyBlock _block;

    private void Awake()
    {
        for (int i = 0; i < _matrices.Length; i++)
        {
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 15f + this.transform.position, Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), Vector3.one * Random.Range(0.5f, 1.5f));
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1.0f));
            _metallic[i] = Random.value < 0.25f ? 1f : 0f;
            _smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
            _block.SetVectorArray(_baseColorId, _baseColors);
            _block.SetFloatArray(_metallicId, _metallic);
            _block.SetFloatArray(_smoothnessId, _smoothness);

            var positions = new Vector3[1023];
            for (int i = 0; i < _matrices.Length; i++) {
                positions[i] = _matrices[i].GetColumn(3);
            }

            var lightProbes = new SphericalHarmonicsL2[1023];
            var occlusionProbes = new Vector4[1023];
            LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, occlusionProbes);
            _block.CopySHCoefficientArraysFrom(lightProbes);
            _block.CopyProbeOcclusionArrayFrom(occlusionProbes);
        }

        Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, 1023, _block,
            ShadowCastingMode.On, true, 0, null, lightProbeProxyVolume ?
                LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided,
            lightProbeProxyVolume);
    }
}