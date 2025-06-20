// Copyright (c) Jason Ma
using System;
using UnityEngine;

namespace Rendering.Editor.LWGUI
{
	[Serializable]
	public class GradientObject : ScriptableObject
	{
		[SerializeField] public Gradient gradient = new Gradient();
	}
}