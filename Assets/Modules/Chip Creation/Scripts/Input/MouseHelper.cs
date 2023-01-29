using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	public static class MouseHelper
	{
		static Camera _cam;


		public static bool LeftMousePressedThisFrame()
		{
			return Mouse.current.leftButton.wasPressedThisFrame;
		}

		public static bool LeftMouseIsPressed()
		{
			return Mouse.current.leftButton.isPressed;
		}

		public static bool LeftMouseReleasedThisFrame()
		{
			return Mouse.current.leftButton.wasReleasedThisFrame;
		}

		public static bool RightMousePressedThisFrame()
		{
			return Mouse.current.rightButton.wasPressedThisFrame;
		}

		public static Vector3 GetMouseWorldPosition(float z)
		{
			Vector2 pos = GetMouseWorldPosition();
			return new Vector3(pos.x, pos.y, z);
		}

		public static Vector2 GetMouseWorldPosition()
		{
			return Cam.ScreenToWorldPoint(GetMouseScreenPosition());
		}

		public static Vector2 GetMouseScreenPosition()
		{
			if (Application.isEditor)
			{
				return SebInput.Internal.MouseEventSystem.GetMousePos();
			}
			return Mouse.current.position.ReadValue();
		}

		public static Vector2 CalculateAxisSnappedMousePosition(Vector2 origin, bool snap = true)
		{
			Vector2 snappedMousePos = GetMouseWorldPosition();
			if (snap)
			{
				Vector2 delta = snappedMousePos - origin;
				bool snapHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
				snappedMousePos = new Vector2(snapHorizontal ? snappedMousePos.x : origin.x, snapHorizontal ? origin.y : snappedMousePos.y);
			}
			return snappedMousePos;

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
			// Ensure static variables are properly initialized when domain reloading is disabled.
			_cam = null;
		}
	}
}