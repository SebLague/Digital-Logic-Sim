using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public static class UIHelper
	{
		static Camera _cam;

		public static Vector2 CalcCanvasLocalPos(Vector2 screenPos, float refWidth = 1920, float refHeight = 1080)
		{
			float scaleX = refWidth / (float)Screen.width;
			float scaleY = refHeight / (float)Screen.height;
			return new Vector2((screenPos.x - Screen.width / 2) * scaleX, (screenPos.y - Screen.height / 2) * scaleY);
		}

		// Note: Canvas is assumed to be Screen Space - Camera (not Overlay)
		public static bool MouseOverRect(RectTransform rect)
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rect, MouseHelper.GetMouseScreenPosition(), Cam);
		}

		static Camera Cam
		{
			get
			{
				if (_cam == null)
				{
					_cam = Camera.main;
				}
				return _cam;
			}
		}



		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			_cam = null;
		}
	}
}