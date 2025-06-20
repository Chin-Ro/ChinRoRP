using UnityEngine;
using UnityEditor;

namespace Rendering.Editor.LWGUI
{
	public static class CustomHeader
	{
		public static void DoCustomHeader(LWGUI lwgui)
		{
			// Draw your custom gui...
			
			// Debug.Log(lwgui.shader);
		}
		
		[InitializeOnLoadMethod]
		private static void RegisterEvent()
		{
			LWGUI.onDrawCustomHeader += DoCustomHeader;
		}
	}
}