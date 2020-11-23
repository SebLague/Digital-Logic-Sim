using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CustomPostProcessing : MonoBehaviour {

	public PostProcessingEffect[] effects;
	Shader defaultShader;
	Material defaultMat;
	List<RenderTexture> temporaryTextures = new List<RenderTexture> ();

	public event System.Action<RenderTexture> onPostProcessingComplete;
	public event System.Action<RenderTexture> onPostProcessingBegin;

	void Init () {
		if (defaultShader == null) {
			defaultShader = Shader.Find ("Unlit/Texture");
		}
		defaultMat = new Material (defaultShader);
	}

	[ImageEffectOpaque]
	void OnRenderImage (RenderTexture intialSource, RenderTexture finalDestination) {
		if (onPostProcessingBegin != null) {
			onPostProcessingBegin (finalDestination);
		}
		Init ();

		temporaryTextures.Clear ();

		RenderTexture currentSource = intialSource;
		RenderTexture currentDestination = null;

		if (effects != null) {
			for (int i = 0; i < effects.Length; i++) {
				PostProcessingEffect effect = effects[i];
				if (effect != null) {
					if (i == effects.Length - 1) {
						// Final effect, so render into final destination texture
						currentDestination = finalDestination;
					} else {
						// Get temporary texture to render this effect into
						currentDestination = TemporaryRenderTexture (finalDestination);
						temporaryTextures.Add (currentDestination); //
					}

					effect.Render (currentSource, currentDestination); // render the effect
					currentSource = currentDestination; // output texture of this effect becomes input for next effect
				}
			}
		}

		// In case dest texture was not rendered into (due to being provided a null effect), copy current src to dest
		if (currentDestination != finalDestination) {
			Graphics.Blit (currentSource, finalDestination, defaultMat);
		}

		// Release temporary textures
		for (int i = 0; i < temporaryTextures.Count; i++) {
			RenderTexture.ReleaseTemporary (temporaryTextures[i]);
		}

		// Trigger post processing complete event
		if (onPostProcessingComplete != null) {
			onPostProcessingComplete (finalDestination);
		}

	}

	// Helper function for blitting a list of materials
	public static void RenderMaterials (RenderTexture source, RenderTexture destination, List<Material> materials) {
		List<RenderTexture> temporaryTextures = new List<RenderTexture> ();

		RenderTexture currentSource = source;
		RenderTexture currentDestination = null;

		if (materials != null) {
			for (int i = 0; i < materials.Count; i++) {
				Material material = materials[i];
				if (material != null) {

					if (i == materials.Count - 1) { // last material
						currentDestination = destination;
					} else {
						// get temporary texture to render this effect into
						currentDestination = TemporaryRenderTexture (destination);
						temporaryTextures.Add (currentDestination);
					}
					Graphics.Blit (currentSource, currentDestination, material);
					currentSource = currentDestination;
				}
			}
		}

		// In case dest texture was not rendered into (due to being provided a null material), copy current src to dest
		if (currentDestination != destination) {
			Graphics.Blit (currentSource, destination, new Material (Shader.Find ("Unlit/Texture")));
		}
		// Release temporary textures
		for (int i = 0; i < temporaryTextures.Count; i++) {
			RenderTexture.ReleaseTemporary (temporaryTextures[i]);
		}
	}

	public static RenderTexture TemporaryRenderTexture (RenderTexture template) {
		return RenderTexture.GetTemporary (template.descriptor);
	}

}