using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.MainMenu
{
	public class Menu : MonoBehaviour
	{
		public event System.Action MenuOpened;
		public event System.Action MenuClosed;
		bool isOpen;

		public void SetIsOpen(bool isOpen)
		{
			gameObject.SetActive(isOpen);
			if (this.isOpen != isOpen)
			{
				this.isOpen = isOpen;
				(isOpen ? MenuOpened : MenuClosed)?.Invoke();
			}
		}
	}
}