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
			return Mouse.current.position.ReadValue();
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