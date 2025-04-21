using System;
using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PulseEditMenu
	{
		static SubChipInstance pulseChip;
		static uint pulseWidth;

		static readonly UIHandle ID_PulseWidthInput = new("PulseChipEdit_PulseWidth");
		static readonly Func<string, bool> integerInputValidator = ValidatePulseWidthInput;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = UI.Centre + Vector2.up * (UI.HalfHeight * 0.25f);

			using (UI.BeginBoundsScope(true))
			{
				UI.DrawText("Pulse Width (ticks)", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				InputFieldTheme inputFieldTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
				inputFieldTheme.fontSize = DrawSettings.ActiveUITheme.FontSizeRegular;

				Vector2 size = new(5.6f, DrawSettings.SelectorWheelHeight);
				Vector2 inputPos = UI.PrevBounds.CentreBottom + Vector2.down * DrawSettings.VerticalButtonSpacing;
				InputFieldState state = UI.InputField(ID_PulseWidthInput, inputFieldTheme, inputPos, size, string.Empty, Anchor.CentreTop, 1, integerInputValidator, forceFocus: true);
				uint.TryParse(state.text, out pulseWidth);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(UI.GetCurrentBoundsScope().BottomLeft, UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					Project.ActiveProject.NotifyPulseWidthChanged(pulseChip, pulseWidth);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			pulseChip = (SubChipInstance)ContextMenu.interactionContext;
			pulseWidth = pulseChip.InternalData[0];
			UI.GetInputFieldState(ID_PulseWidthInput).SetText(pulseWidth.ToString());
		}

		public static bool ValidatePulseWidthInput(string s)
		{
			if (s.Length > 4) return false;
			if (string.IsNullOrEmpty(s)) return true;
			if (s.Contains(" ")) return false;
			return int.TryParse(s, out _);
		}
	}
}