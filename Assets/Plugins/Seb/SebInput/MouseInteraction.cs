using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput.Internal;

namespace SebInput
{
	public class MouseInteraction<T>
	{

		// Terminology: composite collider => the set of 2D colliders attached to this object and its child objects

		// Called when mouse enters composite collider
		public event System.Action<T> MouseEntered;
		// Called when mouse exits composite collider
		public event System.Action<T> MouseExitted;

		// Called when left mouse is pressed down over composite collider
		public event System.Action<T> LeftMouseDown;
		// Called when right mouse is pressed down over composite collider
		public event System.Action<T> RightMouseDown;
		// Called when left mouse is released over composite collider
		public event System.Action<T> LeftMouseReleased;
		// Called when right mouse is released over composite collider
		public event System.Action<T> RightMouseReleased;

		// Called when left mouse has been both pressed and then released over composite collider
		public event System.Action<T> LeftClickCompleted;
		// Called when right mouse has been both pressed and then released over composite collider
		public event System.Action<T> RightClickCompleted;

		// Is the mouse currently over the composite collider?
		public bool MouseIsOver { get; private set; }

		// Adds a listener component to the given gameObject. The given eventContext will be passed in to all events.
		public MouseInteraction(GameObject listenerTarget, T eventContext)
		{
			var listener = listenerTarget.AddComponent<MouseInteractionListener>();

			listener.MouseEntered += () => { MouseIsOver = true; MouseEntered?.Invoke(eventContext); };
			listener.MouseExitted += () => { MouseIsOver = false; MouseExitted?.Invoke(eventContext); };
			listener.LeftMouseDown += () => LeftMouseDown?.Invoke(eventContext);
			listener.RightMouseDown += () => RightMouseDown?.Invoke(eventContext);
			listener.LeftMouseReleased += () => LeftMouseReleased?.Invoke(eventContext);
			listener.RightMouseReleased += () => RightMouseReleased?.Invoke(eventContext);
			listener.LeftClickCompleted += () => LeftClickCompleted?.Invoke(eventContext);
			listener.RightClickCompleted += () => RightClickCompleted?.Invoke(eventContext);
		}

	}

}