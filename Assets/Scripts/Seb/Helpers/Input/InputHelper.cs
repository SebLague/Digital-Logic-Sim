using Seb.Helpers.InputHandling;
using Seb.Types;
using UnityEngine;

namespace Seb.Helpers
{
	public enum MouseButton
	{
		Left = 0,
		Right = 1,
		Middle = 2
	}

	public static class InputHelper
	{
		public static IInputSource InputSource = new UnityInputSource();
		static Camera _worldCam;
		static Vector2 prevWorldMousePos;
		static int prevWorldMouseFrame = -1;
		static int leftMouseDownConsumeFrame = -1;
		static int rightMouseDownConsumeFrame = -1;
		static int middleMouseDownConsumeFrame = -1;
		public static Vector2 MousePos => InputSource.MousePosition; // Screen-space mouse position
		public static string InputStringThisFrame => InputSource.InputString;
		public static bool AnyKeyOrMouseDownThisFrame => InputSource.AnyKeyOrMouseDownThisFrame;
		public static bool AnyKeyOrMouseHeldThisFrame => InputSource.AnyKeyOrMouseHeldThisFrame;
		public static Vector2 MouseScrollDelta => InputSource.MouseScrollDelta;

		public static Camera WorldCam
		{
			get
			{
				if (_worldCam == null) _worldCam = Camera.main;
				return _worldCam;
			}
		}

		public static Vector2 MousePosWorld
		{
			get
			{
				if (Time.frameCount != prevWorldMouseFrame)
				{
					prevWorldMousePos = WorldCam.ScreenToWorldPoint(MousePos);
					prevWorldMouseFrame = Time.frameCount;
				}

				return prevWorldMousePos;
			}
		}

		public static bool ShiftIsHeld => IsKeyHeld(KeyCode.LeftShift) || IsKeyHeld(KeyCode.RightShift);
		public static bool CtrlIsHeld => IsKeyHeld(KeyCode.LeftControl) || IsKeyHeld(KeyCode.RightControl);
		public static bool AltIsHeld => IsKeyHeld(KeyCode.LeftAlt) || IsKeyHeld(KeyCode.RightAlt);

		public static bool IsKeyDownThisFrame(KeyCode key) => InputSource.IsKeyDownThisFrame(key);
		public static bool IsKeyUpThisFrame(KeyCode key) => InputSource.IsKeyUpThisFrame(key);
		public static bool IsKeyHeld(KeyCode key) => InputSource.IsKeyHeld(key);

		public static bool IsMouseInGameWindow()
		{
			Vector2 mousePos = MousePos;
			return mousePos.x >= 0 && mousePos.y >= 0 && mousePos.x < Screen.width && mousePos.y < Screen.height;
		}

		public static bool MouseInBounds_ScreenSpace(Vector2 centre, Vector2 size)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePos - centre;
			return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
		}

		public static bool MouseInBounds_ScreenSpace(Bounds2D bounds)
		{
			if (!Application.isPlaying) return false;
			return bounds.PointInBounds(MousePos);
		}

		public static bool MouseInPoint_ScreenSpace(Vector2 centre, float radius)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePos - centre;
			return offset.sqrMagnitude < radius * radius;
		}

		public static bool MouseInsidePoint_World(Vector2 centre, float radius)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePosWorld - centre;
			return offset.sqrMagnitude < radius * radius;
		}

		public static bool MouseInsideBounds_World(Vector2 centre, Vector2 size)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePosWorld - centre;
			return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
		}

		public static bool MouseInsideBounds_World(Bounds2D bounds)
		{
			if (!Application.isPlaying) return false;
			return bounds.PointInBounds(MousePosWorld);
		}

		public static bool IsMouseHeld(MouseButton button)
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseHeld(button);
		}

		// Check if mouse button was pressed this frame. Optionally consume the event, so it will return false for other callers this frame.
		public static bool IsMouseDownThisFrame(MouseButton button, bool consumeEvent = false)
		{
			if (!Application.isPlaying) return false;
			if (MouseDownEventIsConsumed(button)) return false;

			if (consumeEvent)
			{
				ConsumeMouseButtonDownEvent(button);
			}

			return InputSource.IsMouseDownThisFrame(button);
		}


		// Check if any mouse button was pressed this frame, even if the event was consumed.
		public static bool IsAnyMouseButtonDownThisFrame_IgnoreConsumed()
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseDownThisFrame(MouseButton.Left) || InputSource.IsMouseDownThisFrame(MouseButton.Right) || InputSource.IsMouseDownThisFrame(MouseButton.Middle);
		}

		// Consume mouse down event (the mouse event will report false on all subsequent calls this frame)
		public static void ConsumeMouseButtonDownEvent(MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				leftMouseDownConsumeFrame = Time.frameCount;
			}
			else if (button == MouseButton.Right)
			{
				rightMouseDownConsumeFrame = Time.frameCount;
			}
			else if (button == MouseButton.Middle)
			{
				middleMouseDownConsumeFrame = Time.frameCount;
			}
		}

		static bool MouseDownEventIsConsumed(MouseButton button)
		{
			int lastConsumedFrame = button switch
			{
				MouseButton.Left => leftMouseDownConsumeFrame,
				MouseButton.Right => rightMouseDownConsumeFrame,
				MouseButton.Middle => middleMouseDownConsumeFrame,
				_ => -1
			};
			return Time.frameCount == lastConsumedFrame;
		}

		public static bool IsMouseUpThisFrame(MouseButton button)
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseUpThisFrame(button);
		}

		public static void CopyToClipboard(string s) => GUIUtility.systemCopyBuffer = s;
		public static string GetClipboardContents() => GUIUtility.systemCopyBuffer;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Reset()
		{
			_worldCam = null;
			prevWorldMouseFrame = -1;
			leftMouseDownConsumeFrame = -1;
			rightMouseDownConsumeFrame = -1;
			middleMouseDownConsumeFrame = -1;
			InputSource = new UnityInputSource();
		}
	}
}