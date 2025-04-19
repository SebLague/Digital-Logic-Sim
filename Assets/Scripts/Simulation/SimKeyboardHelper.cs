using System.Collections.Generic;
using System.Threading;
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

		static long KeyStates = 0;

		// Call from Main Thread
		public static void RefreshInputState()
		{
			if (!InputHelper.AnyKeyOrMouseHeldThisFrame || // early exit if no key held
				InputHelper.CtrlIsHeld || InputHelper.ShiftIsHeld || InputHelper.AltIsHeld) // don't trigger key chips if modifier is held
			{
				Interlocked.Exchange(ref KeyStates, 0);
				return;
			}

			long keyStates = 0;
			for (int i = 0; i < ValidInputKeys.Length; ++i)
			{
				KeyCode keyCode = ValidInputKeys[i];
				if (InputHelper.IsKeyHeld(keyCode))
					keyStates |= (1L << i);
			}

			Interlocked.Exchange(ref KeyStates, keyStates);
		}

		// Call from Sim Thread
		public static bool KeyIsHeld(byte key)
		{
			long keyStates = Interlocked.Read(ref KeyStates);
			return ((keyStates >> key) & 1) != 0;
		}
	}
}