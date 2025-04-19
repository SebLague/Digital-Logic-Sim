using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PinEditMenu
	{
		const string MaxLengthPinName = "MY LONG PIN NAME";
		static DevPinInstance devPin;
		static readonly UIHandle ID_NameField = new("PinEditMenu_NameField");
		static readonly UIHandle ID_ValueDisplayMode = new("PinEditMenu_ValueDisplayMode");

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		static readonly string[] PinDecimalDisplayOptions =
		{
			"Off",
			"Unsigned",
			"Signed",
			"HEX"
		};

		public static void OnMenuOpened()
		{
			InputFieldState inputFieldState = UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(devPin.Pin.Name);
			inputFieldState.SelectAll();

			UI.GetWheelSelectorState(ID_ValueDisplayMode).index = (int)devPin.pinValueDisplayMode;
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
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize(MaxLengthPinName, inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 2.25f);
				Vector2 pos = UI.Centre + Vector2.up * 5;

				// Draw input field
				InputFieldState inputFieldState = UI.InputField(ID_NameField, inputTheme, pos, inputFieldSize, devPin.Pin.Name, Anchor.Centre, padX / 2, ValidatePinNameInput, true);
				Bounds2D inputFieldBounds = UI.PrevBounds;
				string newName = inputFieldState.text;

				// Draw value display options
				if (devPin.BitCount != PinBitCount.Bit1)
				{
					const float wheelWidth = 15.2f;

					Vector2 topLeftCurr = UI.PrevBounds.BottomLeft + Vector2.down * spacing;
					MenuHelper.LabeledOptionsWheel("Decimal Display", Color.white, topLeftCurr, new Vector2(inputFieldBounds.Width, DrawSettings.SelectorWheelHeight), ID_ValueDisplayMode, PinDecimalDisplayOptions, wheelWidth, true);
				}

				// Draw cancel/confirm buttons
				Vector2 buttonsTopLeft = UI.PrevBounds.BottomLeft + Vector2.down * spacing;
				int buttonIndex = UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				// Keyboard shortcuts and UI input
				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1) Confirm(newName);
			}
		}

		static void Confirm(string newName)
		{
			devPin.Pin.Name = newName;

			if (devPin.BitCount != PinBitCount.Bit1)
			{
				devPin.pinValueDisplayMode = (PinValueDisplayMode)UI.GetWheelSelectorState(ID_ValueDisplayMode).index;
			}

			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		public static void SetTargetPin(DevPinInstance devPin)
		{
			PinEditMenu.devPin = devPin;
		}

		static bool ValidatePinNameInput(string name) => name.Length <= MaxLengthPinName.Length;
	}
}