using Seb.Helpers;
using UnityEngine;

namespace DLS.Game
{
	public static class KeyboardShortcuts
	{
		// ---- Main Menu shortcuts
		public static bool MainMenu_NewProjectShortcutTriggered => CtrlShortcutTriggered(KeyCode.N);
		public static bool MainMenu_OpenProjectShortcutTriggered => CtrlShortcutTriggered(KeyCode.O);
		public static bool MainMenu_SettingsShortcutTriggered => CtrlShortcutTriggered(KeyCode.S);
		public static bool MainMenu_QuitShortcutTriggered => CtrlShortcutTriggered(KeyCode.Q);

		// ---- Bottom Bar Menu shorcuts ----
		public static bool NewNoteShortcutTriggered => CtrlShortcutTriggered(KeyCode.M);
		public static bool SaveShortcutTriggered => CtrlShortcutTriggered(KeyCode.S);
		public static bool LibraryShortcutTriggered => CtrlShortcutTriggered(KeyCode.L);
		public static bool PreferencesShortcutTriggered => CtrlShortcutTriggered(KeyCode.P);
		public static bool CreateNewChipShortcutTriggered => CtrlShortcutTriggered(KeyCode.N);
		public static bool QuitToMainMenuShortcutTriggered => CtrlShortcutTriggered(KeyCode.Q);
		public static bool SearchShortcutTriggered => CtrlShortcutTriggered(KeyCode.Space) || CtrlShortcutTriggered(KeyCode.F);


		// ---- Misc shortcuts ----
		public static bool DuplicateShortcutTriggered => MultiModeHeld && InputHelper.IsKeyDownThisFrame(KeyCode.D);
		public static bool ToggleGridShortcutTriggered => CtrlShortcutTriggered(KeyCode.G);
		public static bool ResetCameraShortcutTriggered => CtrlShortcutTriggered(KeyCode.R);

		// ---- Single key shortcuts ----
		public static bool CancelShortcutTriggered => InputHelper.IsKeyDownThisFrame(KeyCode.Escape);
		public static bool ConfirmShortcutTriggered => InputHelper.IsKeyDownThisFrame(KeyCode.Return);
		public static bool DeleteShortcutTriggered => InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete);
		public static bool SimNextStepShortcutTriggered => InputHelper.IsKeyDownThisFrame(KeyCode.Tab);

		// ---- Dev shortcuts ----
		public static bool OpenSaveDataFolderShortcutTriggered => InputHelper.IsKeyDownThisFrame(KeyCode.O) && InputHelper.CtrlIsHeld && InputHelper.ShiftIsHeld && InputHelper.AltIsHeld;

		// ---- Modifiers ----
		public static bool SnapModeHeld => InputHelper.CtrlIsHeld;

		// In "Multi-mode", placed chips will be duplicated once placed to allow placing again; selecting a chip will add it to the current selection; etc.
		public static bool MultiModeHeld => InputHelper.AltIsHeld || InputHelper.ShiftIsHeld;
		public static bool StraightLineModeHeld => InputHelper.ShiftIsHeld;
		public static bool StraightLineModeTriggered=> InputHelper.IsKeyDownThisFrame(KeyCode.LeftShift);
		public static bool CameraActionKeyHeld => InputHelper.AltIsHeld;
		public static bool TakeFirstFromCollectionModifierHeld => InputHelper.CtrlIsHeld || InputHelper.AltIsHeld || InputHelper.ShiftIsHeld;

		// ---- Helpers ----
		static bool CtrlShortcutTriggered(KeyCode key) => InputHelper.IsKeyDownThisFrame(key) && InputHelper.CtrlIsHeld && !(InputHelper.AltIsHeld || InputHelper.ShiftIsHeld);
		static bool ShiftShortcutTriggered(KeyCode key) => InputHelper.IsKeyDownThisFrame(key) && InputHelper.ShiftIsHeld && !(InputHelper.AltIsHeld || InputHelper.CtrlIsHeld);
	}
}