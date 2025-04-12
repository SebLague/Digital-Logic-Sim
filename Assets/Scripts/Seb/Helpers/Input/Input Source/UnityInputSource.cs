using UnityEngine;

namespace Seb.Helpers.InputHandling
{
	public class UnityInputSource : IInputSource
	{
		public Vector2 MousePosition => Input.mousePosition;

		public bool IsKeyDownThisFrame(KeyCode key) => Input.GetKeyDown(key);
		public bool IsKeyUpThisFrame(KeyCode key) => Input.GetKeyUp(key);
		public bool IsKeyHeld(KeyCode key) => Input.GetKey(key);
		public bool AnyKeyOrMouseDownThisFrame => Input.anyKeyDown;
		public bool AnyKeyOrMouseHeldThisFrame => Input.anyKey;
		public string InputString => Input.inputString;
		public Vector2 MouseScrollDelta => Input.mouseScrollDelta;
		public bool IsMouseDownThisFrame(MouseButton button) => Input.GetMouseButtonDown((int)button);
	}
}