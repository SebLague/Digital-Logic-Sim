using UnityEngine;

namespace Seb.Helpers.InputHandling
{
	public interface IInputSource
	{
		public Vector2 MousePosition { get; }

		public bool AnyKeyOrMouseDownThisFrame { get; }
		public bool AnyKeyOrMouseHeldThisFrame { get; }

		public string InputString { get; }
		public Vector2 MouseScrollDelta { get; }
		public bool IsKeyDownThisFrame(KeyCode key);
		public bool IsKeyUpThisFrame(KeyCode key);
		public bool IsKeyHeld(KeyCode key);

		public bool IsMouseDownThisFrame(MouseButton button) => IsKeyDownThisFrame(GetMouseKeyCode(button));
		public bool IsMouseUpThisFrame(MouseButton button) => IsKeyUpThisFrame(GetMouseKeyCode(button));
		public bool IsMouseHeld(MouseButton button) => IsKeyHeld(GetMouseKeyCode(button));


		static KeyCode GetMouseKeyCode(MouseButton mouseButton) => KeyCode.Mouse0 + (int)mouseButton;
	}
}