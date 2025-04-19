using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using System.Linq;
using UnityEngine;

namespace DLS.Graphics
{
	public static class RebindKeyChipMenu
	{
		public static readonly string[] KeyStrings = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Select(c => new string(c, 1)).ToArray();
		public const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		static SubChipInstance keyChip;
		static byte key;

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
					for (int i = 0; i < allowedChars.Length; ++i)
					{
						if (activeChar == allowedChars[i])
						{
							key = (byte)i;
							break;
						}
					}
				}

				UI.DrawText("Press a key to rebind\n (alphanumeric only)", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				UI.DrawPanel(UI.PrevBounds.CentreBottom + Vector2.down, Vector2.one * 3.5f, new Color(0.1f, 0.1f, 0.1f), Anchor.CentreTop);
				UI.DrawText(KeyStrings[key], theme.FontBold, theme.FontSizeRegular * 1.5f, UI.PrevBounds.Centre, Anchor.TextCentre, Color.white);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(UI.GetCurrentBoundsScope().BottomLeft, UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					Project.ActiveProject.NotifyKeyChipBindingChanged(keyChip, key);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			keyChip = (SubChipInstance)ContextMenu.interactionContext;
			key = keyChip.Key;
		}
	}
}