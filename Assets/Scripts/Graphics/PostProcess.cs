using UnityEngine;

namespace DLS.Graphics
{
	[ExecuteAlways]
	public class PostProcess : MonoBehaviour
	{
		public void OnRenderImage(RenderTexture src, RenderTexture target)
		{
			// Seems to fix vertical flip issues (?)
			UnityEngine.Graphics.Blit(src, target);
		}
	}
}