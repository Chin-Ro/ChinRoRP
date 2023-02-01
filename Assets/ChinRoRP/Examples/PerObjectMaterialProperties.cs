using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int _baseColorId  = Shader.PropertyToID("_BaseColor"),
     _cutoffId = Shader.PropertyToID("_Cutoff"),
     _metallicId = Shader.PropertyToID("_Metallic"),
     _smoothnessId = Shader.PropertyToID("_Smoothness"),
     _emissionColorId = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock _block;
    
    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0.0f, 1.0f)] 
    float cutoff = 0.5f,
		  metallic = 0f,
	      smoothness = 0.5f;

    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;
    private void Awake()
    {
	    OnValidate();
    }

    void OnValidate () {
		if (_block == null) {
			_block = new MaterialPropertyBlock();
		}
		_block.SetColor(_baseColorId, baseColor);
		_block.SetFloat(_cutoffId, cutoff);
		_block.SetFloat(_metallicId, metallic);
		_block.SetFloat(_smoothnessId, smoothness);
		_block.SetColor(_emissionColorId, emissionColor);
		
		GetComponent<Renderer>().SetPropertyBlock(_block);
	}
}
