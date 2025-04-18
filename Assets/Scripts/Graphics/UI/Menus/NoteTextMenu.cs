using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class NoteTextMenu
	{
		const string MaxLabelLength = "MY REAL LONG LABEL TEXT";
		static NoteInstance note;
		static readonly UIHandle ID_NameField = new("NoteTextMenu_NameField");

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		public static void OnMenuOpened()
		{
			note = (NoteInstance)ContextMenu.interactionContext;

			InputFieldState inputFieldState = UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(note.Text);
			inputFieldState.SelectAll();
		}

		public static void DrawMenu()
		{
			UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			float spacing = 0.8f;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			Draw.ID panelID = UI.ReservePanel();

			using (UI.BeginBoundsScope(true))
			{
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize(MaxLabelLength, inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 26f);
				Vector2 pos = UI.Centre + Vector2.up * 5;

				// Draw input field
				InputFieldState inputFieldState = UI.TextArea(ID_NameField, inputTheme, pos, inputFieldSize, note.Text, Anchor.Centre, padX / 2, MaxLabelLength, 7, null, true);
				Bounds2D inputFieldBounds = UI.PrevBounds;
				string newName = inputFieldState.text;

				// Draw cancel/confirm buttons
				Vector2 buttonsTopLeft = UI.PrevBounds.BottomLeft + Vector2.down * spacing;
				int buttonIndex = UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				// Keyboard shortcuts and UI input
				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (buttonIndex == 1) Confirm(newName);
			}
		}

		static void Confirm(string newName)
		{
			note.Text = newName;
			note.Resize();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}
	}
}