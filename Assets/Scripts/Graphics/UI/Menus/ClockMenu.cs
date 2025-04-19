using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using System;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ClockMenu
	{
		static SubChipInstance clockChip;
		static readonly UIHandle ID_Clockspeed = new("ClockMenu_Clockspeed");

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;

			using (UI.BeginBoundsScope(true))
			{
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize("10000000000", inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 2.25f);
				Vector2 pos = UI.Centre + Vector2.up * 5;

				UI.DrawText("Clock speed", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				InputFieldState state = UI.InputField(ID_Clockspeed, inputTheme, UI.PrevBounds.CentreBottom + Vector2.down * 3.5f, inputFieldSize, clockChip.Clockspeed.ToString(), Anchor.TextCentreRight, padX / 2, ValidateIntegerInput, true);
				Bounds2D inputFieldBounds = UI.PrevBounds;
				UInt64 newValue = clockChip.Clockspeed;
				if (!state.focused && !UInt64.TryParse(state.text, out newValue))
					state.SetText(newValue.ToString());

				// Draw cancel/confirm buttons
				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(UI.GetCurrentBoundsScope().BottomLeft, UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					if (!UInt64.TryParse(state.text, out newValue))
						state.SetText(newValue.ToString());
					Project.ActiveProject.NotifyClockChipClockspeedChanged(clockChip, newValue);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			clockChip = (SubChipInstance)ContextMenu.interactionContext;
			InputFieldState state = UI.GetInputFieldState(ID_Clockspeed);
			state.SetText(clockChip.Clockspeed.ToString());
		}

		static bool ValidateIntegerInput(string str)
		{
			if (str.Length >= 12) return false;
			foreach (char c in str)
			{
				if (c < '0' || c > '9')
					return false;
			}
			return true;
		}
	}
}