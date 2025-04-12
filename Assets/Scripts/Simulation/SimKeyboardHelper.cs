using System.Collections.Generic;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Simulation
{
	public static class SimKeyboardHelper
	{
		public static readonly KeyCode[] ValidInputKeys =
		{
			KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
			KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N,
			KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
			KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,

			KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
			KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
		};

		static readonly HashSet<char> KeyLookup = new();
		static bool HasAnyInput;

		// Call from Main Thread
		public static void RefreshInputState()
		{
			lock (KeyLookup)
			{
				KeyLookup.Clear();
				HasAnyInput = false;

				if (!InputHelper.AnyKeyOrMouseHeldThisFrame) return; // early exit if no key held
				if (InputHelper.CtrlIsHeld || InputHelper.ShiftIsHeld || InputHelper.AltIsHeld) return; // don't trigger key chips if modifier is held

				foreach (KeyCode key in ValidInputKeys)
				{
					if (InputHelper.IsKeyHeld(key))
					{
						char keyChar = char.ToUpper((char)key);
						KeyLookup.Add(keyChar);
						HasAnyInput = true;
					}
				}
			}
		}

		// Call from Sim Thread
		public static bool KeyIsHeld(char key)
		{
			bool isHeld;

			lock (KeyLookup)
			{
				isHeld = HasAnyInput && KeyLookup.Contains(key);
			}

			return isHeld;
		}
	}
}