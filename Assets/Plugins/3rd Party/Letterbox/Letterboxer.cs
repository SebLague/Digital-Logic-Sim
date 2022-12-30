using UnityEngine;

// Code from https://github.com/RyanNielson/Letterboxer

namespace Letterboxer {
	[ExecuteInEditMode, RequireComponent (typeof (Camera))]
	public class Letterboxer : MonoBehaviour {
		[SerializeField]
		private int targetWidth = 1280;

		[SerializeField]
		private int targetHeight = 720;

		[SerializeField]
		private CameraType type = CameraType.MaintainAspectRatio;

		private Camera _camera;
		
		private Camera Camera {
			get { return _camera ?? (_camera = GetComponent<Camera> ()); }
		}

		private int currentScreenWidth = -1;

		private int currentScreenHeight = -1;

		private void Update () {
			if (ShouldUpdateLetterbox ()) {
				currentScreenWidth = Screen.width;
				currentScreenHeight = Screen.height;

				UpdateLetterbox ();
			}
		}

		private void UpdateLetterbox () {
			if (type == CameraType.MaintainAspectRatio) {
				HandleMaintainAspectRatio ();
			} else {
				HandleBestFit ();
			}
		}

		private void HandleMaintainAspectRatio () {
			float targetAspect = targetWidth / (float) targetHeight;
			float windowAspect = currentScreenWidth / (float) currentScreenHeight;
			float scaleHeight = windowAspect / targetAspect;

			Camera.rect = scaleHeight < 1.0f ? GetLetterboxRect (scaleHeight) : GetPillarboxRect (scaleHeight);
		}

		private void HandleBestFit () {
			int nearestWidth = currentScreenWidth / targetWidth * targetWidth;
			int nearestHeight = currentScreenHeight / targetHeight * targetHeight;

			int scaleFactor = GetScaleFactor (nearestWidth, nearestHeight);
			float xWidthFactor = targetWidth * scaleFactor / (float) currentScreenWidth;
			float yHeightFactor = targetHeight * scaleFactor / (float) currentScreenHeight;

			Camera.rect = new Rect (GetRectPosition (xWidthFactor, currentScreenWidth), GetRectPosition (yHeightFactor, currentScreenHeight), xWidthFactor, yHeightFactor);
		}

		private int GetScaleFactor (int nearestWidth, int nearestHeight) {
			int xScaleFactor = nearestWidth / targetWidth;
			int yScaleFactor = nearestHeight / targetHeight;

			return yScaleFactor < xScaleFactor ? yScaleFactor : xScaleFactor;
		}

		private float GetRectPosition (float factor, int screenSize) {
			return (1 - factor) / 2f + GetOffset (screenSize);
		}

		private float GetOffset (int size) {
			return size % 2 == 0 ? 0 : 1f / size;
		}

		private Rect GetLetterboxRect (float scaleHeight) {
			return new Rect (0, (1f - scaleHeight) / 2f, 1f, scaleHeight);
		}

		private Rect GetPillarboxRect (float scaleHeight) {
			float scalewidth = 1.0f / scaleHeight;

			return new Rect ((1f - scalewidth) / 2f, 0, scalewidth, 1f);
		}

		private bool ShouldUpdateLetterbox () {
			return Screen.width != currentScreenWidth || Screen.height != currentScreenHeight;
		}

		private void OnValidate () {
			targetWidth = Mathf.Max (1, targetWidth);
			targetHeight = Mathf.Max (1, targetHeight);

			UpdateLetterbox ();
		}
	}
}