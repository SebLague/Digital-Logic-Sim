using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput.Internal;

namespace SebInput
{
	// Convience class for listening to multiple MouseInteractions
	public class MouseInteractionGroup<T>
	{

		public event System.Action<T> MouseEntered;
		public event System.Action<T> MouseExitted;

		public event System.Action<T> LeftMouseDown;
		public event System.Action<T> RightMouseDown;
		public event System.Action<T> LeftMouseReleased;
		public event System.Action<T> RightMouseReleased;

		public event System.Action<T> LeftClickCompleted;
		public event System.Action<T> RightClickCompleted;

		public void AddInteractionToGroup(MouseInteraction<T> interaction)
		{
			interaction.MouseEntered += (e) => MouseEntered?.Invoke(e);
			interaction.MouseExitted += (e) => MouseExitted?.Invoke(e);
			interaction.LeftMouseDown += (e) => LeftMouseDown?.Invoke(e);
			interaction.RightMouseDown += (e) => RightMouseDown?.Invoke(e);
			interaction.LeftMouseReleased += (e) => LeftMouseReleased?.Invoke(e);
			interaction.RightMouseReleased += (e) => RightMouseReleased?.Invoke(e);
			interaction.LeftClickCompleted += (e) => LeftClickCompleted?.Invoke(e);
			interaction.RightClickCompleted += (e) => RightClickCompleted?.Invoke(e);
		}

	}

}