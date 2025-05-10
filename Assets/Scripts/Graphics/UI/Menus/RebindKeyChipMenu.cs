using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class RebindKeyChipMenu
	{
		public const string allowedChars = "1234567890QWERTYUIOPASDFGHJKLZXCVBNM";
		static SubChipInstance keyChip;
		static string chosenKey;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = UI.Centre + Vector2.up * (UI.HalfHeight * 0.25f);

			using (UI.BeginBoundsScope(true))
			{
				if (InputHelper.AnyKeyOrMouseDownThisFrame && !string.IsNullOrEmpty(InputHelper.InputStringThisFrame))
				{
					char activeChar = char.ToUpper(InputHelper.InputStringThisFrame[0]);
					if (allowedChars.Contains(activeChar))
					{
						chosenKey = activeChar.ToString();
					}
				}

				UI.DrawText("Press a key to rebind\n (alphanumeric only)", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				UI.DrawPanel(UI.PrevBounds.CentreBottom + Vector2.down, Vector2.one * 3.5f, new Color(0.1f, 0.1f, 0.1f), Anchor.CentreTop);
				UI.DrawText(chosenKey, theme.FontBold, theme.FontSizeRegular * 1.5f, UI.PrevBounds.Centre, Anchor.TextCentre, Color.white);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(UI.GetCurrentBoundsScope().BottomLeft, UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					Project.ActiveProject.NotifyKeyChipBindingChanged(keyChip, chosenKey[0]);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			keyChip = (SubChipInstance)ContextMenu.interactionContext;
			chosenKey = keyChip.activationKeyString;
		}
	}
}