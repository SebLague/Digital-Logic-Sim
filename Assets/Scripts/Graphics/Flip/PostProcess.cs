using UnityEngine;

namespace DLS.Graphics
{
	[ExecuteAlways]
	public class PostProcess : MonoBehaviour
	{
		public Shader flipShader;
		Material flipMat;
		
		public void OnRenderImage(RenderTexture src, RenderTexture target)
		{
			if (flipMat == null) flipMat = new Material(flipShader);
			
			UnityEngine.Graphics.Blit(src, target, flipMat);
		}
	}
}