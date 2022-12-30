using UnityEngine;

namespace SebInput.Internal
{
	public class MouseInteractionListener : MonoBehaviour
	{
		// Terminology: composite collider => the set of 2D colliders attached to this object and its child objects

		// Called when mouse enters composite collider
		public event System.Action MouseEntered;
		// Called when mouse exits composite collider
		public event System.Action MouseExitted;

		// Called when left mouse is pressed down over composite collider
		public event System.Action LeftMouseDown;
		// Called when right mouse is pressed down over composite collider
		public event System.Action RightMouseDown;
		// Called when left mouse has been both pressed and then released over composite collider
		public event System.Action LeftClickCompleted;
		// Called when right mouse has been both pressed and then released over composite collider
		public event System.Action RightClickCompleted;

		// Called when left mouse is released over composite collider
		public event System.Action LeftMouseReleased;
		// Called when right mouse is released over composite collider
		public event System.Action RightMouseReleased;

		void Awake()
		{
			MouseEventSystem.AddInteractionListener(this);
		}

		public void OnMouseEnter()
		{
			MouseEntered?.Invoke();
		}

		public void OnMouseExit()
		{
			MouseExitted?.Invoke();
		}

		public void OnMousePressDown(MouseEventSystem.MouseButton mouseButton)
		{
			if (mouseButton == MouseEventSystem.MouseButton.Left)
			{
				LeftMouseDown?.Invoke();
			}
			else if (mouseButton == MouseEventSystem.MouseButton.Right)
			{
				RightMouseDown?.Invoke();
			}
		}

		public void OnClickCompleted(MouseEventSystem.MouseButton mouseButton)
		{
			if (mouseButton == MouseEventSystem.MouseButton.Left)
			{
				LeftClickCompleted?.Invoke();
			}
			else if (mouseButton == MouseEventSystem.MouseButton.Right)
			{
				RightClickCompleted?.Invoke();
			}
		}

		public void OnMouseRelease(MouseEventSystem.MouseButton mouseButton)
		{
			if (mouseButton == MouseEventSystem.MouseButton.Left)
			{
				LeftMouseReleased?.Invoke();
			}
			else if (mouseButton == MouseEventSystem.MouseButton.Right)
			{
				RightMouseReleased?.Invoke();
			}
		}

		void OnDestroy()
		{
			MouseEventSystem.RemoveInteractionListener(this);
		}
	}

}