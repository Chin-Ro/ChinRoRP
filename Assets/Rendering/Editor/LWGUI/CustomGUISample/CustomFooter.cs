using UnityEngine;
using UnityEditor;

namespace Rendering.Editor.LWGUI
{
	public static class CustomFooter
	{
		public static void DoCustomFooter(LWGUI lwgui)
		{
			// Draw your custom gui...
			
			// Debug.Log(lwgui.shader);
		}
		
		[InitializeOnLoadMethod]
		private static void RegisterEvent()
		{
			LWGUI.onDrawCustomFooter += DoCustomFooter;
		}
	}
}