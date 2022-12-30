using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public class ColourWindow : MonoBehaviour
	{
		public event System.Action<Color> ColourUpdated;

		[Header("Colour References")]
		public RectTransform satValGrid;
		public RectTransform satValSelector;
		public RectTransform hueRamp;
		public RectTransform hueSelector;

		Material satValMaterial;

		bool isMovingSatValSelector;
		bool isMovingHueSelector;

		float hue;
		float saturation;
		float value;
		Color colOld;
		Camera cam;

		void Start()
		{
			cam = Camera.main;
			var satValImage = satValGrid.GetComponent<UnityEngine.UI.Image>();
			satValImage.material = new Material(satValImage.material);
			satValMaterial = satValImage.material;
		}

		public void SetInitialColour(Color colour, bool notify = false)
		{
			Color col = new Color(colour.r, colour.g, colour.b);
			Color.RGBToHSV(col, out hue, out saturation, out value);
			colOld = col;
			UpdateSelectorPositions();
			if (notify)
			{
				ColourUpdated?.Invoke(colour);
			}
		}

		void Update()
		{
			satValMaterial.SetFloat("Hue", hue);
			Color col = Color.HSVToRGB(hue, saturation, value);
			if (col != colOld)
			{
				ColourUpdated?.Invoke(new Color(col.r, col.g, col.b));
				colOld = col;
			}

			HandleInput();
		}

		void HandleInput()
		{
			Vector2 mousePos = MouseHelper.GetMouseScreenPosition();
			HandleSatValInput(mousePos);
			HandleHueInput(mousePos);

			UpdateSelectorPositions();

			if (MouseHelper.LeftMouseReleasedThisFrame())
			{
				OnMouseUp();
			}
		}

		void HandleHueInput(Vector2 mousePos)
		{
			if (MouseHelper.LeftMouseIsPressed())
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(hueRamp, mousePos, cam) && !isMovingSatValSelector)
				{
					isMovingHueSelector = true;
				}
			}

			if (isMovingHueSelector)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(hueRamp, mousePos, cam, out Vector2 localPoint);
				float height = hueRamp.sizeDelta.y;
				hue = Mathf.Clamp01((localPoint.y + height / 2) / height);
			}
		}

		void HandleSatValInput(Vector2 mousePos)
		{


			if (MouseHelper.LeftMouseIsPressed())
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(satValGrid, mousePos, cam) && !isMovingHueSelector)
				{
					isMovingSatValSelector = true;
				}
			}

			if (isMovingSatValSelector)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(satValGrid, mousePos, cam, out Vector2 localPoint);
				//Debug.Log(localPoint);
				float width = satValGrid.sizeDelta.x;
				float height = satValGrid.sizeDelta.y;
				saturation = Mathf.Clamp01((localPoint.x + width / 2) / width);
				value = Mathf.Clamp01((localPoint.y + height / 2) / height);
			}
		}

		void UpdateSelectorPositions()
		{
			UpdateSatValSelectorPosition();
			UpdateHueSelectorPosition();

			void UpdateSatValSelectorPosition()
			{
				float width = satValGrid.sizeDelta.x;
				float height = satValGrid.sizeDelta.y;
				float clampedX = saturation * width - width / 2;
				float clampedY = value * height - height / 2;
				satValSelector.localPosition = new Vector2(clampedX, clampedY);
			}

			void UpdateHueSelectorPosition()
			{
				float height = hueRamp.sizeDelta.y;
				float clampedY = hue * height - height / 2;
				hueSelector.localPosition = new Vector2(0, clampedY);
			}
		}

		void OnMouseUp()
		{
			isMovingSatValSelector = false;
			isMovingHueSelector = false;
		}

	}
}